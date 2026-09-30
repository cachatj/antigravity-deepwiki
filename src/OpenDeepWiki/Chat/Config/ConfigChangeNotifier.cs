using Microsoft.Extensions.Logging;

namespace OpenDeepWiki.Chat.Config;

/// <summary>
/// Config change notifier
/// Notifies related components when configuration changes
/// </summary>
public class ConfigChangeNotifier : IConfigChangeNotifier
{
    private readonly ILogger<ConfigChangeNotifier> _logger;
    private readonly List<ConfigChangeSubscription> _subscriptions = new();
    private readonly object _lock = new();
    private long _subscriptionIdCounter;
    
    public ConfigChangeNotifier(ILogger<ConfigChangeNotifier> logger)
    {
        _logger = logger;
    }
    
    /// <inheritdoc />
    public IDisposable Subscribe(string? platform, Action<ConfigChangeEvent> handler)
    {
        var subscriptionId = Interlocked.Increment(ref _subscriptionIdCounter);
        var subscription = new ConfigChangeSubscription(subscriptionId, platform, handler);
        
        lock (_lock)
        {
            _subscriptions.Add(subscription);
        }
        
        _logger.LogDebug("Added config change subscription {Id} for platform: {Platform}", 
            subscriptionId, platform ?? "all");
        
        return new SubscriptionDisposable(() => Unsubscribe(subscriptionId));
    }
    
    /// <inheritdoc />
    public void NotifyChange(string platform, ConfigChangeType changeType)
    {
        var changeEvent = new ConfigChangeEvent(platform, changeType, DateTimeOffset.UtcNow);
        
        List<ConfigChangeSubscription> matchingSubscriptions;
        lock (_lock)
        {
            matchingSubscriptions = _subscriptions
                .Where(s => s.Platform == null || s.Platform == platform)
                .ToList();
        }
        
        _logger.LogInformation("Notifying {Count} subscribers of config change for platform: {Platform}, type: {Type}",
            matchingSubscriptions.Count, platform, changeType);
        
        foreach (var subscription in matchingSubscriptions)
        {
            try
            {
                subscription.Handler(changeEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in config change handler for subscription {Id}", subscription.Id);
            }
        }
    }
    
    /// <summary>
    /// Unsubscribe
    /// </summary>
    private void Unsubscribe(long subscriptionId)
    {
        lock (_lock)
        {
            var subscription = _subscriptions.FirstOrDefault(s => s.Id == subscriptionId);
            if (subscription != null)
            {
                _subscriptions.Remove(subscription);
                _logger.LogDebug("Removed config change subscription {Id}", subscriptionId);
            }
        }
    }
    
    /// <summary>
    /// Subscription info
    /// </summary>
    private record ConfigChangeSubscription(long Id, string? Platform, Action<ConfigChangeEvent> Handler);
    
    /// <summary>
    /// Helper class for cancelling a subscription
    /// </summary>
    private class SubscriptionDisposable : IDisposable
    {
        private readonly Action _disposeAction;
        private bool _disposed;
        
        public SubscriptionDisposable(Action disposeAction)
        {
            _disposeAction = disposeAction;
        }
        
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposeAction();
                _disposed = true;
            }
        }
    }
}

/// <summary>
/// Config change notifier interface
/// </summary>
public interface IConfigChangeNotifier
{
    /// <summary>
    /// Subscribe to config changes
    /// </summary>
    /// <param name="platform">Platform identifier; null subscribes to all platforms</param>
    /// <param name="handler">Change handler</param>
    /// <returns>An IDisposable that unsubscribes</returns>
    IDisposable Subscribe(string? platform, Action<ConfigChangeEvent> handler);
    
    /// <summary>
    /// Notify config change
    /// </summary>
    /// <param name="platform">Platform identifier</param>
    /// <param name="changeType">Change type</param>
    void NotifyChange(string platform, ConfigChangeType changeType);
}

/// <summary>
/// Config change event
/// </summary>
public record ConfigChangeEvent(
    string Platform,
    ConfigChangeType ChangeType,
    DateTimeOffset Timestamp
);

/// <summary>
/// Config change type
/// </summary>
public enum ConfigChangeType
{
    /// <summary>
    /// Added
    /// </summary>
    Created,
    
    /// <summary>
    /// Updated
    /// </summary>
    Updated,
    
    /// <summary>
    /// Deleted
    /// </summary>
    Deleted,
    
    /// <summary>
    /// Reloaded
    /// </summary>
    Reloaded
}
