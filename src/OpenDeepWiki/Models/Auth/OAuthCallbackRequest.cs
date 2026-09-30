namespace OpenDeepWiki.Models.Auth;

/// <summary>
/// OAuth callback request
/// </summary>
public class OAuthCallbackRequest
{
    /// <summary>
    /// Authorization code
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// State value (used to prevent CSRF attacks)
    /// </summary>
    public string? State { get; set; }
}
