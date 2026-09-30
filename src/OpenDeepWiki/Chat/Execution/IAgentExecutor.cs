using OpenDeepWiki.Chat.Abstractions;
using OpenDeepWiki.Chat.Sessions;

namespace OpenDeepWiki.Chat.Execution;

/// <summary>
/// Agent executor interface
/// Processes messages and generates responses
/// </summary>
public interface IAgentExecutor
{
    /// <summary>
    /// Execute the agent to process a message
    /// </summary>
    /// <param name="message">User message</param>
    /// <param name="session">Chat session</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Agent response</returns>
    Task<AgentResponse> ExecuteAsync(
        IChatMessage message, 
        IChatSession session, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Execute the agent with streaming
    /// </summary>
    /// <param name="message">User message</param>
    /// <param name="session">Chat session</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Stream of agent response chunks</returns>
    IAsyncEnumerable<AgentResponseChunk> ExecuteStreamAsync(
        IChatMessage message, 
        IChatSession session, 
        CancellationToken cancellationToken = default);
}
