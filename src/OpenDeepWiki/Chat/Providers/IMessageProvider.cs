using Microsoft.AspNetCore.Http;
using OpenDeepWiki.Chat.Abstractions;

namespace OpenDeepWiki.Chat.Providers;

/// <summary>
/// Message provider interface
/// </summary>
public interface IMessageProvider
{
    /// <summary>
    /// Platform identifier
    /// </summary>
    string PlatformId { get; }
    
    /// <summary>
    /// Platform display name
    /// </summary>
    string DisplayName { get; }
    
    /// <summary>
    /// Whether enabled
    /// </summary>
    bool IsEnabled { get; }
    
    /// <summary>
    /// Initialize the provider
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Parse a raw platform message into the unified format
    /// </summary>
    Task<IChatMessage?> ParseMessageAsync(string rawMessage, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Send a message to the platform
    /// </summary>
    Task<SendResult> SendMessageAsync(IChatMessage message, string targetUserId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Send multiple messages
    /// </summary>
    Task<IEnumerable<SendResult>> SendMessagesAsync(IEnumerable<IChatMessage> messages, string targetUserId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validate webhook request
    /// </summary>
    Task<WebhookValidationResult> ValidateWebhookAsync(HttpRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Shut down the provider
    /// </summary>
    Task ShutdownAsync(CancellationToken cancellationToken = default);
}
