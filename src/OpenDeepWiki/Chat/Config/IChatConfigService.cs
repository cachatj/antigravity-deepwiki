namespace OpenDeepWiki.Chat.Config;

/// <summary>
/// Chat config service interface
/// Provides management of provider configuration
/// </summary>
public interface IChatConfigService
{
    /// <summary>
    /// Get the config for the specified platform
    /// </summary>
    /// <param name="platform">Platform identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Config object, or null if it does not exist</returns>
    Task<ProviderConfigDto?> GetConfigAsync(string platform, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get all configs
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all configs</returns>
    Task<IEnumerable<ProviderConfigDto>> GetAllConfigsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Save config (add or update)
    /// </summary>
    /// <param name="config">Config object</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SaveConfigAsync(ProviderConfigDto config, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Delete config
    /// </summary>
    /// <param name="platform">Platform identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeleteConfigAsync(string platform, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validate config integrity
    /// </summary>
    /// <param name="config">Config object</param>
    /// <returns>Validation result</returns>
    ConfigValidationResult ValidateConfig(ProviderConfigDto config);
    
    /// <summary>
    /// Validate all configs
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of validation results</returns>
    Task<IEnumerable<ConfigValidationResult>> ValidateAllConfigsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Register a config change callback
    /// </summary>
    /// <param name="callback">Callback function</param>
    /// <returns>An IDisposable that unregisters the callback</returns>
    IDisposable OnConfigChanged(Action<string> callback);
    
    /// <summary>
    /// Trigger a config reload
    /// </summary>
    /// <param name="platform">Platform identifier; if null, reloads all configs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ReloadConfigAsync(string? platform = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Provider config DTO
/// </summary>
public class ProviderConfigDto
{
    /// <summary>
    /// Platform identifier
    /// </summary>
    public string Platform { get; set; } = string.Empty;
    
    /// <summary>
    /// Display name
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// Config data (plaintext JSON)
    /// </summary>
    public string ConfigData { get; set; } = string.Empty;
    
    /// <summary>
    /// Webhook URL
    /// </summary>
    public string? WebhookUrl { get; set; }
    
    /// <summary>
    /// Message send interval (milliseconds)
    /// </summary>
    public int MessageInterval { get; set; } = 500;
    
    /// <summary>
    /// Maximum retry count
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;
}

/// <summary>
/// Config validation result
/// </summary>
public class ConfigValidationResult
{
    /// <summary>
    /// Platform identifier
    /// </summary>
    public string Platform { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether validation passed
    /// </summary>
    public bool IsValid { get; set; }
    
    /// <summary>
    /// List of error messages
    /// </summary>
    public List<string> Errors { get; set; } = new();
    
    /// <summary>
    /// Missing config items
    /// </summary>
    public List<string> MissingFields { get; set; } = new();
    
    /// <summary>
    /// Create a successful validation result
    /// </summary>
    public static ConfigValidationResult Success(string platform) => new()
    {
        Platform = platform,
        IsValid = true
    };
    
    /// <summary>
    /// Create a failed validation result
    /// </summary>
    public static ConfigValidationResult Failure(string platform, params string[] errors) => new()
    {
        Platform = platform,
        IsValid = false,
        Errors = errors.ToList()
    };
}
