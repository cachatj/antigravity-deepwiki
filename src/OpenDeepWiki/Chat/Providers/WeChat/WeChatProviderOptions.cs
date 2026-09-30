namespace OpenDeepWiki.Chat.Providers.WeChat;

/// <summary>
/// WeChat customer service provider configuration options
/// </summary>
public class WeChatProviderOptions : ProviderOptions
{
    /// <summary>
    /// WeChat Official Account / Mini Program AppID
    /// </summary>
    public string AppId { get; set; } = string.Empty;
    
    /// <summary>
    /// WeChat Official Account / Mini Program AppSecret
    /// </summary>
    public string AppSecret { get; set; } = string.Empty;
    
    /// <summary>
    /// Token configured on the WeChat server (used to verify message origin)
    /// </summary>
    public string Token { get; set; } = string.Empty;
    
    /// <summary>
    /// Message encryption/decryption key (EncodingAESKey)
    /// </summary>
    public string EncodingAesKey { get; set; } = string.Empty;
    
    /// <summary>
    /// WeChat API base URL
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://api.weixin.qq.com";
    
    /// <summary>
    /// Access token cache duration (seconds), default 7000 seconds (slightly less than WeChat's 7200-second validity)
    /// </summary>
    public int TokenCacheSeconds { get; set; } = 7000;
    
    /// <summary>
    /// Message encryption mode: plain, compatible, or safe
    /// </summary>
    public string EncryptMode { get; set; } = "safe";
}
