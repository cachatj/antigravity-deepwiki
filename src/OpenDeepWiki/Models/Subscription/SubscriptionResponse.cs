namespace OpenDeepWiki.Models.Subscription;

/// <summary>
/// Subscription operation response
/// </summary>
public class SubscriptionResponse
{
    /// <summary>
    /// Whether the operation succeeded
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message (only set on failure)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Subscription record ID (only set on success)
    /// </summary>
    public string? SubscriptionId { get; set; }
}
