using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
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
    /// <summary>
    /// Key under which a Gemini thought signature is stashed on an <see cref="AIContent"/>
    /// so it can be echoed back verbatim on subsequent requests. Gemini 3.x rejects
    /// function-call parts in history that are missing their signature.
    /// </summary>
    public const string ThoughtSignatureKey = "gemini.thoughtSignature";

    /// <summary>
    /// Documented placeholder accepted by the API for function calls that did not originate
    /// from the model (or whose signature was lost). Skips signature validation for that part.
    /// </summary>
    private static readonly byte[] SkipSignatureValidator =
        Encoding.UTF8.GetBytes("skip_thought_signature_validator");

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

        var message = new ChatMessage { Role = ChatRole.Assistant };
        foreach (var content in ConvertParts(response))
        {
            message.Contents.Add(content);
        }

        return new ChatResponse(message)
        {
            ModelId = _model
        };
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = BuildRequest(chatMessages, options);
        var stream = _client.Models.GenerateContentStreamAsync(_model, request.Contents, request.Config, cancellationToken);

        var textChars = 0;
        var functionCalls = new List<string>();
        string? finishReason = null;
        await foreach (var chunk in stream)
        {
            if (cancellationToken.IsCancellationRequested) yield break;

            foreach (var content in ConvertParts(chunk))
            {
                if (content is TextContent tc) textChars += tc.Text?.Length ?? 0;
                if (content is FunctionCallContent fcc) functionCalls.Add(fcc.Name);
                yield return new ChatResponseUpdate
                {
                    Role = ChatRole.Assistant,
                    ModelId = _model,
                    Contents = new[] { content }
                };
            }

            ReportAbnormalFinish(chunk);
            finishReason = chunk.Candidates?.FirstOrDefault()?.FinishReason?.ToString() ?? finishReason;

            var usage = ConvertUsage(chunk);
            if (usage != null)
            {
                yield return new ChatResponseUpdate
                {
                    Role = ChatRole.Assistant,
                    ModelId = _model,
                    Contents = new[] { usage }
                };
            }
        }

        // One line per model turn so silent/empty turns are visible in the container logs.
        Console.WriteLine($"[GeminiChatClient] Turn complete. Model: {_model}, TextChars: {textChars}, FunctionCalls: [{string.Join(", ", functionCalls)}], FinishReason: {finishReason ?? "n/a"}");
    }

    /// <summary>
    /// Surfaces non-STOP finish reasons and prompt blocks, which otherwise make the model look like it
    /// silently returned nothing (e.g. MALFORMED_FUNCTION_CALL, MAX_TOKENS, SAFETY).
    /// </summary>
    private void ReportAbnormalFinish(Google.GenAI.Types.GenerateContentResponse chunk)
    {
        var block = chunk.PromptFeedback?.BlockReason;
        if (block != null)
        {
            Console.WriteLine($"[GeminiChatClient] Prompt blocked. Model: {_model}, Reason: {block}, Message: {chunk.PromptFeedback?.BlockReasonMessage}");
        }

        var candidate = chunk.Candidates?.FirstOrDefault();
        var finish = candidate?.FinishReason?.ToString();
        if (!string.IsNullOrEmpty(finish) && !string.Equals(finish, "Stop", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"[GeminiChatClient] Abnormal finish. Model: {_model}, FinishReason: {finish}, FinishMessage: {candidate?.FinishMessage}");
        }
    }

    private static UsageContent? ConvertUsage(Google.GenAI.Types.GenerateContentResponse chunk)
    {
        var meta = chunk.UsageMetadata;
        if (meta == null || meta.TotalTokenCount is null or 0)
            return null;

        return new UsageContent(new UsageDetails
        {
            InputTokenCount = meta.PromptTokenCount,
            OutputTokenCount = (meta.CandidatesTokenCount ?? 0) + (meta.ThoughtsTokenCount ?? 0),
            TotalTokenCount = meta.TotalTokenCount
        });
    }

    /// <summary>
    /// Converts the parts of the first candidate into MEAI content, preserving thought signatures
    /// and dropping hidden "thought" parts so they are neither displayed nor echoed back.
    /// </summary>
    private static IEnumerable<AIContent> ConvertParts(Google.GenAI.Types.GenerateContentResponse response)
    {
        var parts = response.Candidates?.FirstOrDefault()?.Content?.Parts;
        if (parts == null) yield break;

        foreach (var part in parts)
        {
            if (part.FunctionCall is { } fc)
            {
                var argsDict = new Dictionary<string, object?>();
                if (fc.Args != null)
                {
                    var json = JsonSerializer.Serialize(fc.Args);
                    argsDict = JsonSerializer.Deserialize<Dictionary<string, object?>>(json) ?? new();
                }

                var call = new FunctionCallContent(fc.Id ?? Guid.NewGuid().ToString(), fc.Name ?? string.Empty, argsDict);
                AttachSignature(call, part.ThoughtSignature);
                yield return call;
            }
            else if (part.Text != null)
            {
                // Thought parts are the model's reasoning summary; do not surface them as output.
                if (part.Thought == true) continue;

                var text = new TextContent(part.Text);
                AttachSignature(text, part.ThoughtSignature);
                yield return text;
            }
        }
    }

    private static void AttachSignature(AIContent content, byte[]? signature)
    {
        if (signature == null || signature.Length == 0) return;
        content.AdditionalProperties ??= new AdditionalPropertiesDictionary();
        content.AdditionalProperties[ThoughtSignatureKey] = signature;
    }

    private static byte[]? GetSignature(AIContent content)
    {
        if (content.AdditionalProperties != null &&
            content.AdditionalProperties.TryGetValue(ThoughtSignatureKey, out var value) &&
            value is byte[] { Length: > 0 } bytes)
        {
            return bytes;
        }

        return null;
    }

    private class GeminiRequest
    {
        public List<Google.GenAI.Types.Content> Contents { get; set; } = new();
        public Google.GenAI.Types.GenerateContentConfig Config { get; set; } = new();
    }

    private GeminiRequest BuildRequest(IEnumerable<ChatMessage> chatMessages, ChatOptions? options)
    {
        var req = new GeminiRequest();

        // FunctionResultContent only carries the call id; Gemini requires the function name
        // on functionResponse parts, so resolve it from the preceding function calls.
        var callIdToName = new Dictionary<string, string>(StringComparer.Ordinal);

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
                // Function responses are sent back in the user turn, not the model turn.
                Role = message.Role == ChatRole.Assistant ? "model" : "user",
                Parts = new List<Google.GenAI.Types.Part>()
            };

            foreach (var item in message.Contents)
            {
                if (item is TextContent textPart)
                {
                    content.Parts.Add(new Google.GenAI.Types.Part
                    {
                        Text = textPart.Text,
                        ThoughtSignature = GetSignature(textPart)
                    });
                }
                else if (item is FunctionCallContent fc)
                {
                    if (!string.IsNullOrEmpty(fc.CallId))
                    {
                        callIdToName[fc.CallId] = fc.Name;
                    }

                    var dict = fc.Arguments as IDictionary<string, object?>;
                    var dictObj = dict != null ? dict.ToDictionary(k => k.Key, v => v.Value ?? new object()) : new Dictionary<string, object>();

                    content.Parts.Add(new Google.GenAI.Types.Part
                    {
                        // Echo the model's signature verbatim; fall back to the documented
                        // validator-skip placeholder for calls that never had one.
                        ThoughtSignature = GetSignature(fc) ?? SkipSignatureValidator,
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
                    var resultDict = new Dictionary<string, object> { { "result", fr.Result ?? string.Empty } };
                    callIdToName.TryGetValue(fr.CallId ?? string.Empty, out var functionName);

                    content.Parts.Add(new Google.GenAI.Types.Part
                    {
                        FunctionResponse = new Google.GenAI.Types.FunctionResponse
                        {
                            Name = functionName ?? fr.CallId,
                            Response = resultDict,
                            Id = fr.CallId
                        }
                    });
                }
            }

            if (content.Parts.Count == 0) continue;

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
                        var schemaJson = NormalizeSchemaForGemini(tool.JsonSchema);
                        var schemaOptions = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase) }
                        };
                        var parameters = JsonSerializer.Deserialize<Google.GenAI.Types.Schema>(schemaJson, schemaOptions);
                        declaration.Parameters = parameters;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[GeminiChatClient] Failed to deserialize schema for tool {tool.Name}: {ex.Message}");
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

    /// <summary>
    /// Gemini's Schema.Type is a single enum, but Microsoft.Extensions.AI emits JSON Schema
    /// type unions such as ["string","null"] for nullable parameters. Collapse those to the
    /// non-null type and mark the property nullable so the declaration deserializes.
    /// </summary>
    private static string NormalizeSchemaForGemini(JsonElement schema)
    {
        var node = System.Text.Json.Nodes.JsonNode.Parse(schema.GetRawText());
        if (node is null)
            return schema.GetRawText();

        NormalizeSchemaNode(node);
        return node.ToJsonString();
    }

    private static void NormalizeSchemaNode(System.Text.Json.Nodes.JsonNode node)
    {
        switch (node)
        {
            case System.Text.Json.Nodes.JsonObject obj:
                if (obj["type"] is System.Text.Json.Nodes.JsonArray typeArray)
                {
                    var types = typeArray.Select(t => t?.GetValue<string>()).Where(t => t != null).ToList();
                    var isNullable = types.Any(t => string.Equals(t, "null", StringComparison.OrdinalIgnoreCase));
                    var primary = types.FirstOrDefault(t => !string.Equals(t, "null", StringComparison.OrdinalIgnoreCase));
                    obj.Remove("type");
                    if (primary != null)
                        obj["type"] = primary;
                    if (isNullable)
                        obj["nullable"] = true;
                }

                // Gemini rejects unknown keywords; drop ones Microsoft.Extensions.AI commonly emits.
                obj.Remove("$schema");
                obj.Remove("additionalProperties");
                obj.Remove("default");

                foreach (var child in obj.Select(kv => kv.Value).Where(v => v != null).ToList())
                    NormalizeSchemaNode(child!);
                break;

            case System.Text.Json.Nodes.JsonArray arr:
                foreach (var item in arr.Where(i => i != null).ToList())
                    NormalizeSchemaNode(item!);
                break;
        }
    }
}
