using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace OpenDeepWiki.Agents;

/// <summary>
/// Options for configuring an agent run.
/// Replaces ChatClientAgentOptions from Microsoft.Agents.AI.
/// </summary>
public class AgentRunOptions
{
    /// <summary>Chat options including tools, max tokens, and tool mode.</summary>
    public ChatOptions? ChatOptions { get; set; }

    /// <summary>Maximum number of tool-calling round-trips before stopping.</summary>
    public int MaxRoundTrips { get; set; } = 20;
}

/// <summary>
/// Lightweight agent runner that replaces ChatClientAgent from Microsoft.Agents.AI.
/// Implements the tool-calling loop directly over any IChatClient, using only
/// Microsoft.Extensions.AI types — no Microsoft.Agents dependency.
/// </summary>
public static class AgentRunner
{
    /// <summary>
    /// Runs a streaming agent loop: sends messages to the model, detects
    /// FunctionCallContent in the response, invokes the matching AIFunction,
    /// appends FunctionResultContent, and re-calls the model — repeating
    /// until the model produces a final text response or the round-trip limit is reached.
    /// </summary>
    public static async IAsyncEnumerable<ChatResponseUpdate> RunStreamingAsync(
        IChatClient chatClient,
        IList<ChatMessage> messages,
        AgentRunOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var chatOptions = options?.ChatOptions ?? new ChatOptions();
        var maxRoundTrips = options?.MaxRoundTrips ?? 20;

        // Build a name→function lookup from the tools in ChatOptions
        var toolMap = new Dictionary<string, AIFunction>(StringComparer.OrdinalIgnoreCase);
        if (chatOptions.Tools != null)
        {
            foreach (var tool in chatOptions.Tools.OfType<AIFunction>())
            {
                toolMap[tool.Name] = tool;
            }
        }

        for (var round = 0; round < maxRoundTrips; round++)
        {
            var pendingCalls = new List<FunctionCallContent>();
            var textParts = new List<string>();

            await foreach (var update in chatClient.GetStreamingResponseAsync(messages, chatOptions, cancellationToken))
            {
                // Yield text chunks immediately for streaming
                if (!string.IsNullOrEmpty(update.Text))
                {
                    textParts.Add(update.Text);
                }

                // Collect function call requests
                foreach (var content in update.Contents)
                {
                    if (content is FunctionCallContent fc)
                    {
                        pendingCalls.Add(fc);
                    }
                }

                // Always yield the update to the caller for real-time processing
                yield return update;
            }

            // If no function calls were requested, we're done
            if (pendingCalls.Count == 0)
            {
                yield break;
            }

            // Build the assistant message containing the function calls
            var assistantMessage = new ChatMessage
            {
                Role = ChatRole.Assistant
            };
            if (textParts.Count > 0)
            {
                assistantMessage.Contents.Add(new TextContent(string.Concat(textParts)));
            }
            foreach (var fc in pendingCalls)
            {
                assistantMessage.Contents.Add(fc);
            }
            messages.Add(assistantMessage);

            // Invoke each function and collect results
            var toolResultMessage = new ChatMessage { Role = ChatRole.Tool };
            foreach (var fc in pendingCalls)
            {
                object? result;
                try
                {
                    if (toolMap.TryGetValue(fc.Name, out var func))
                    {
                        result = await func.InvokeAsync(
                            fc.Arguments != null ? new AIFunctionArguments(fc.Arguments) : null, 
                            cancellationToken);
                    }
                    else
                    {
                        result = $"Error: Unknown function '{fc.Name}'";
                    }
                }
                catch (Exception ex)
                {
                    result = $"Error: {ex.Message}";
                }

                // Serialize complex results to JSON string for the model
                var resultStr = result switch
                {
                    null => "null",
                    string s => s,
                    _ => JsonSerializer.Serialize(result)
                };

                toolResultMessage.Contents.Add(new FunctionResultContent(fc.CallId, resultStr));
            }
            messages.Add(toolResultMessage);

            // Continue the loop — re-send to model with updated conversation
        }
    }
}
