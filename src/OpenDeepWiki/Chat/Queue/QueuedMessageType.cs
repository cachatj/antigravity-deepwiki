namespace OpenDeepWiki.Chat.Queue;

/// <summary>
/// Queue message type
/// </summary>
public enum QueuedMessageType
{
    /// <summary>
    /// Inbound message (received from a platform)
    /// </summary>
    Incoming,
    
    /// <summary>
    /// Outbound message (sent to a platform)
    /// </summary>
    Outgoing,
    
    /// <summary>
    /// Retry message
    /// </summary>
    Retry
}
