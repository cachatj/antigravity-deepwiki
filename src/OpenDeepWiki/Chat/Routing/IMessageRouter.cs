using OpenDeepWiki.Chat.Abstractions;
using OpenDeepWiki.Chat.Providers;

namespace OpenDeepWiki.Chat.Routing;

/// <summary>
/// Message router interface
/// Routes messages to the correct provider
/// </summary>
public interface IMessageRouter
{
    /// <summary>
    /// Route an inbound message
    /// </summary>
    /// <param name="message">Inbound message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RouteIncomingAsync(IChatMessage message, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Route an outbound message
    /// </summary>
    /// <param name="message">Outbound message</param>
    /// <param name="targetUserId">Target user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RouteOutgoingAsync(IChatMessage message, string targetUserId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get the provider for the specified platform
    /// </summary>
    /// <param name="platform">Platform identifier</param>
    /// <returns>Provider instance, or null if it does not exist</returns>
    IMessageProvider? GetProvider(string platform);
    
    /// <summary>
    /// Get all registered providers
    /// </summary>
    /// <returns>Collection of all registered providers</returns>
    IEnumerable<IMessageProvider> GetAllProviders();
    
    /// <summary>
    /// Register providers
    /// </summary>
    /// <param name="provider">Provider to register</param>
    void RegisterProvider(IMessageProvider provider);
    
    /// <summary>
    /// Unregister a provider
    /// </summary>
    /// <param name="platform">Platform identifier</param>
    /// <returns>Whether unregistration succeeded</returns>
    bool UnregisterProvider(string platform);
    
    /// <summary>
    /// Check whether a provider is registered for the specified platform
    /// </summary>
    /// <param name="platform">Platform identifier</param>
    /// <returns>Whether it exists</returns>
    bool HasProvider(string platform);
}
