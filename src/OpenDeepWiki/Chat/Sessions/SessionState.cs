namespace OpenDeepWiki.Chat.Sessions;

/// <summary>
/// Session state enumeration
/// </summary>
public enum SessionState
{
    /// <summary>
    /// Active; can receive and process messages
    /// </summary>
    Active,
    
    /// <summary>
    /// Processing; the agent is running
    /// </summary>
    Processing,
    
    /// <summary>
    /// Waiting; awaiting a user response
    /// </summary>
    Waiting,
    
    /// <summary>
    /// Expired; the configured expiration time has passed
    /// </summary>
    Expired,
    
    /// <summary>
    /// Closed; the session has ended
    /// </summary>
    Closed
}
