namespace OpenDeepWiki.Models.Subscription;

/// <summary>
/// Subscription status response
/// </summary>
public class SubscriptionStatusResponse
{
    /// <summary>
    /// Whether subscribed
    /// </summary>
    public bool IsSubscribed { get; set; }

    /// <summary>
    /// Subscribed time (only set when subscribed)
    /// </summary>
    public DateTime? SubscribedAt { get; set; }
}
