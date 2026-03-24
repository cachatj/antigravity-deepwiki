using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Google.GenAI;
using Microsoft.Extensions.AI;

namespace OpenDeepWiki.Agents;

/// <summary>
/// A native IChatClient implementation for Google.GenAI,
/// bypassing AsIChatClient() to avoid Microsoft.Extensions.AI.Abstractions versioning conflicts.
/// </summary>
public sealed class GeminiChatClient : IChatClient
{
    private readonly Google.GenAI.Client _client;
    private readonly string _model;
    private readonly ChatClientMetadata _metadata;

    public GeminiChatClient(Google.GenAI.Client client, string model)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _metadata = new ChatClientMetadata("Google.GenAI", new Uri("https://generativelanguage.googleapis.com"));
    }

    public void Dispose()
    {
        // GenAIClient does not need explicit disposing here
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return serviceType == typeof(ChatClientMetadata) ? _metadata : null;
    }

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages, 
        ChatOptions? options = null, 
        CancellationToken cancellationToken = default)
    {
        var request = BuildRequest(chatMessages, options);
        var response = await _client.Models.GenerateContentAsync(_model, request.Contents, request.Config, cancellationToken);
        
        return ParseResponse(response, _model);
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages, 
        ChatOptions? options = null, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = BuildRequest(chatMessages, options);
        var stream = _client.Models.GenerateContentStreamAsync(_model, request.Contents, request.Config, cancellationToken);

        var first = true;
        
        await foreach (var chunk in stream)
        {
            if (cancellationToken.IsCancellationRequested) yield break;

            if (first && chunk.UsageMetadata != null)
            {
                // Just in case we want to yield metadata
            }
            first = false;

            if (chunk.Text != null)
            {
                yield return new ChatResponseUpdate
                {
                    Role = ChatRole.Assistant,
                    Contents = new[] { new TextContent(chunk.Text) }
                };
            }
            
            if (chunk.FunctionCalls != null)
            {
                foreach (var fc in chunk.FunctionCalls)
                {
                    var argsDict = new Dictionary<string, object?>();
                    if (fc.Args != null)
                    {
                        var json = JsonSerializer.Serialize(fc.Args);
                        argsDict = JsonSerializer.Deserialize<Dictionary<string, object?>>(json);
                    }

                    yield return new ChatResponseUpdate
                    {
                        Role = ChatRole.Assistant,
                        Contents = new[] { new FunctionCallContent(fc.Id ?? Guid.NewGuid().ToString(), fc.Name ?? string.Empty, argsDict) }
                    };
                }
            }
        }
    }

    private class GeminiRequest
    {
        public List<Google.GenAI.Types.Content> Contents { get; set; } = new();
        public Google.GenAI.Types.GenerateContentConfig Config { get; set; } = new();
    }

    private GeminiRequest BuildRequest(IEnumerable<ChatMessage> chatMessages, ChatOptions? options)
    {
        var req = new GeminiRequest();
        
        foreach (var message in chatMessages)
        {
            if (message.Role == ChatRole.System)
            {
                if (req.Config.SystemInstruction == null)
                {
                    req.Config.SystemInstruction = new Google.GenAI.Types.Content { Parts = new List<Google.GenAI.Types.Part>() };
                }
                req.Config.SystemInstruction.Parts.Add(new Google.GenAI.Types.Part { Text = message.Text });
                continue;
            }

            var content = new Google.GenAI.Types.Content
            {
                Role = message.Role == ChatRole.User ? "user" : "model",
                Parts = new List<Google.GenAI.Types.Part>()
            };

            foreach (var item in message.Contents)
            {
                if (item is TextContent textPart)
                {
                    content.Parts.Add(new Google.GenAI.Types.Part { Text = textPart.Text });
                }
                else if (item is FunctionCallContent fc)
                {
                    // Map to Gemini FunctionCall
                    var dict = fc.Arguments as IDictionary<string, object?>;
                    var dictObj = dict != null ? dict.ToDictionary(k => k.Key, v => v.Value ?? new object()) : new Dictionary<string, object>();

                    content.Parts.Add(new Google.GenAI.Types.Part 
                    { 
                        FunctionCall = new Google.GenAI.Types.FunctionCall 
                        { 
                            Name = fc.Name, 
                            Args = dictObj,
                            Id = fc.CallId
                        } 
                    });
                }
                else if (item is FunctionResultContent fr)
                {
                    // Map to Gemini FunctionResponse
                    var resultDict = new Dictionary<string, object> { { "result", fr.Result ?? string.Empty } };

                    content.Parts.Add(new Google.GenAI.Types.Part 
                    { 
                        FunctionResponse = new Google.GenAI.Types.FunctionResponse 
                        { 
                            Name = fr.CallId, // FunctionResultContent doesn't contain Name, we use CallId or empty
                            Response = resultDict,
                            Id = fr.CallId
                        } 
                    });
                }
            }

            req.Contents.Add(content);
        }

        if (options != null)
        {
            if (options.Temperature.HasValue)
                req.Config.Temperature = options.Temperature.Value;
            
            if (options.MaxOutputTokens.HasValue)
                req.Config.MaxOutputTokens = options.MaxOutputTokens.Value;

            if (options.Tools != null && options.Tools.Any())
            {
                var functionDeclarations = new List<Google.GenAI.Types.FunctionDeclaration>();
                foreach (var tool in options.Tools.OfType<AIFunction>())
                {
                    var declaration = new Google.GenAI.Types.FunctionDeclaration
                    {
                        Name = tool.Name,
                        Description = tool.Description
                    };
                    
                    try
                    {
                        var schemaJson = tool.JsonSchema.ToString();
                        var schemaOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var parameters = JsonSerializer.Deserialize<Google.GenAI.Types.Schema>(schemaJson, schemaOptions);
                        declaration.Parameters = parameters;
                    }
                    catch (Exception)
                    {
                        // Fallback if schema doesn't match perfectly, though typically GenAI SDK handles generic schema fields.
                    }
                    
                    functionDeclarations.Add(declaration);
                }

                req.Config.Tools = new List<Google.GenAI.Types.Tool>
                {
                    new Google.GenAI.Types.Tool
                    {
                        FunctionDeclarations = functionDeclarations
                    }
                };
            }
        }

        return req;
    }

    private ChatResponse ParseResponse(Google.GenAI.Types.GenerateContentResponse response, string model)
    {
        var message = new ChatMessage { Role = ChatRole.Assistant };
        
        if (response.Text != null)
        {
            message.Contents.Add(new TextContent(response.Text));
        }

        if (response.FunctionCalls != null)
        {
            foreach (var fc in response.FunctionCalls)
            {
                var argsDict = new Dictionary<string, object?>();
                if (fc.Args != null)
                {
                    var json = JsonSerializer.Serialize(fc.Args);
                    argsDict = JsonSerializer.Deserialize<Dictionary<string, object?>>(json);
                }

                message.Contents.Add(new FunctionCallContent(fc.Id ?? Guid.NewGuid().ToString(), fc.Name ?? string.Empty, argsDict));
            }
        }

        return new ChatResponse(message)
        {
            ModelId = model
        };
    }
}
