namespace OpenDeepWiki.Chat.Providers.Feishu;

/// <summary>
/// Feishu provider configuration options
/// </summary>
public class FeishuProviderOptions : ProviderOptions
{
    /// <summary>
    /// Feishu app App ID
    /// </summary>
    public string AppId { get; set; } = string.Empty;
    
    /// <summary>
    /// Feishu app App Secret
    /// </summary>
    public string AppSecret { get; set; } = string.Empty;
    
    /// <summary>
    /// Feishu Verification Token (used to validate webhook requests)
    /// </summary>
    public string VerificationToken { get; set; } = string.Empty;
    
    /// <summary>
    /// Feishu Encrypt Key (for message encryption/decryption, optional)
    /// </summary>
    public string? EncryptKey { get; set; }
    
    /// <summary>
    /// Feishu API base URL
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://open.feishu.cn/open-apis";
    
    /// <summary>
    /// Access token cache duration (seconds), default 7000 seconds (slightly less than Feishu's 7200-second validity)
    /// </summary>
    public int TokenCacheSeconds { get; set; } = 7000;
}
