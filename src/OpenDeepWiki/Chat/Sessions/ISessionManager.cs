namespace OpenDeepWiki.Chat.Sessions;

/// <summary>
/// Session manager interface
/// Handles session creation, lookup, update and cleanup
/// </summary>
public interface ISessionManager
{
    /// <summary>
    /// Get or create a session
    /// If a session already exists for the given user and platform, return it; otherwise create a new one
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="platform">Platform identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Session instance</returns>
    Task<IChatSession> GetOrCreateSessionAsync(
        string userId, 
        string platform, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get a session by session ID
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Session instance, or null if it does not exist</returns>
    Task<IChatSession?> GetSessionAsync(
        string sessionId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update the session in persistent storage
    /// </summary>
    /// <param name="session">Session instance</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateSessionAsync(
        IChatSession session, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Close a session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CloseSessionAsync(
        string sessionId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Clean up expired sessions
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default);
}
