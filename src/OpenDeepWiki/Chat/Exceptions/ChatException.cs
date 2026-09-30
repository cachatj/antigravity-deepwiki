namespace OpenDeepWiki.Chat.Exceptions;

/// <summary>
/// Base exception for the Chat system
/// </summary>
public class ChatException : Exception
{
    /// <summary>
    /// Error code
    /// </summary>
    public string ErrorCode { get; }
    
    /// <summary>
    /// Whether the operation should be retried
    /// </summary>
    public bool ShouldRetry { get; }
    
    public ChatException(string message, string errorCode, bool shouldRetry = false)
        : base(message)
    {
        ErrorCode = errorCode;
        ShouldRetry = shouldRetry;
    }
    
    public ChatException(string message, string errorCode, Exception innerException, bool shouldRetry = false)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        ShouldRetry = shouldRetry;
    }
}
