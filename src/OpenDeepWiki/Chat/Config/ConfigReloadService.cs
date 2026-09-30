using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OpenDeepWiki.Chat.Config;

/// <summary>
/// Config hot reload background service
/// Periodically checks for config changes and notifies related components
/// </summary>
public class ConfigReloadService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfigChangeNotifier _changeNotifier;
    private readonly ILogger<ConfigReloadService> _logger;
    private readonly ChatConfigOptions _options;
    
    // Config snapshot used to detect changes
    private Dictionary<string, string> _configSnapshots = new();
    private readonly object _snapshotLock = new();
    
    public ConfigReloadService(
        IServiceScopeFactory scopeFactory,
        IConfigChangeNotifier changeNotifier,
        IOptions<ChatConfigOptions> options,
        ILogger<ConfigReloadService> logger)
    {
        _scopeFactory = scopeFactory;
        _changeNotifier = changeNotifier;
        _logger = logger;
        _options = options.Value;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.EnableHotReload)
        {
            _logger.LogInformation("Config hot reload is disabled");
            return;
        }
        
        _logger.LogInformation("Config reload service started");
        
        // Initialize snapshot
        await InitializeSnapshotsAsync(stoppingToken);
        
        // Periodically check for config changes
        var checkInterval = TimeSpan.FromSeconds(_options.CacheExpirationSeconds / 2);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(checkInterval, stoppingToken);
                await CheckForChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for config changes");
            }
        }
        
        _logger.LogInformation("Config reload service stopped");
    }
    
    /// <summary>
    /// Initialize config snapshot
    /// </summary>
    private async Task InitializeSnapshotsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var configService = scope.ServiceProvider.GetRequiredService<IChatConfigService>();
        
        var configs = await configService.GetAllConfigsAsync(cancellationToken);
        
        lock (_snapshotLock)
        {
            _configSnapshots = configs.ToDictionary(
                c => c.Platform,
                c => ComputeConfigHash(c)
            );
        }
        
        _logger.LogDebug("Initialized config snapshots for {Count} platforms", _configSnapshots.Count);
    }
    
    /// <summary>
    /// Check for config changes
    /// </summary>
    private async Task CheckForChangesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var configService = scope.ServiceProvider.GetRequiredService<IChatConfigService>();
        
        var currentConfigs = await configService.GetAllConfigsAsync(cancellationToken);
        var currentDict = currentConfigs.ToDictionary(c => c.Platform, c => c);
        
        Dictionary<string, string> previousSnapshots;
        lock (_snapshotLock)
        {
            previousSnapshots = new Dictionary<string, string>(_configSnapshots);
        }
        
        var changes = new List<(string Platform, ConfigChangeType Type)>();
        
        // Check for additions and updates
        foreach (var config in currentConfigs)
        {
            var hash = ComputeConfigHash(config);
            
            if (!previousSnapshots.TryGetValue(config.Platform, out var previousHash))
            {
                changes.Add((config.Platform, ConfigChangeType.Created));
            }
            else if (hash != previousHash)
            {
                changes.Add((config.Platform, ConfigChangeType.Updated));
            }
        }
        
        // Check for deletions
        foreach (var platform in previousSnapshots.Keys)
        {
            if (!currentDict.ContainsKey(platform))
            {
                changes.Add((platform, ConfigChangeType.Deleted));
            }
        }
        
        // Update snapshot
        lock (_snapshotLock)
        {
            _configSnapshots = currentConfigs.ToDictionary(
                c => c.Platform,
                c => ComputeConfigHash(c)
            );
        }
        
        // Notify changes
        foreach (var (platform, changeType) in changes)
        {
            _logger.LogInformation("Detected config change: {Platform} - {Type}", platform, changeType);
            _changeNotifier.NotifyChange(platform, changeType);
        }
    }
    
    /// <summary>
    /// Compute config hash
    /// </summary>
    private static string ComputeConfigHash(ProviderConfigDto config)
    {
        var content = $"{config.Platform}|{config.DisplayName}|{config.IsEnabled}|{config.ConfigData}|{config.WebhookUrl}|{config.MessageInterval}|{config.MaxRetryCount}";
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(content));
        return Convert.ToBase64String(hash);
    }
}
