using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenDeepWiki.Chat.Providers;
using OpenDeepWiki.Chat.Routing;

namespace OpenDeepWiki.Chat;

/// <summary>
/// Provider initialization background service.
/// Responsible for initializing all providers on application startup and registering them with the router.
/// Requirements: 2.2, 2.4
/// </summary>
public class ProviderInitializationService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProviderInitializationService> _logger;

    public ProviderInitializationService(
        IServiceProvider serviceProvider,
        ILogger<ProviderInitializationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing Chat Providers...");

        using var scope = _serviceProvider.CreateScope();
        var providers = scope.ServiceProvider.GetRequiredService<IEnumerable<IMessageProvider>>();
        var router = _serviceProvider.GetRequiredService<IMessageRouter>();

        foreach (var provider in providers)
        {
            try
            {
                _logger.LogInformation("Initializing Provider: {PlatformId} ({DisplayName})", 
                    provider.PlatformId, provider.DisplayName);

                await provider.InitializeAsync(cancellationToken);
                
                // Register with router
                router.RegisterProvider(provider);
                
                _logger.LogInformation("Provider {PlatformId} initialized successfully, enabled: {IsEnabled}", 
                    provider.PlatformId, provider.IsEnabled);
            }
            catch (Exception ex)
            {
                // Requirements: 2.4 - Log error and continue loading other providers on initialization failure
                _logger.LogError(ex, "Provider {PlatformId} initialization failed, continuing with other providers", 
                    provider.PlatformId);
            }
        }

        _logger.LogInformation("Chat Provider initialization complete, registered {Count} providers", 
            router.GetAllProviders().Count());
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Shutting down Chat Providers...");

        var router = _serviceProvider.GetRequiredService<IMessageRouter>();
        
        foreach (var provider in router.GetAllProviders())
        {
            try
            {
                await provider.ShutdownAsync(cancellationToken);
                _logger.LogInformation("Provider {PlatformId} shut down", provider.PlatformId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error shutting down Provider {PlatformId}", provider.PlatformId);
            }
        }

        _logger.LogInformation("All Chat Providers shut down");
    }
}
