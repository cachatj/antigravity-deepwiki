using System.Security.Claims;

namespace OpenDeepWiki.Services.Auth;

/// <summary>
/// User context interface for accessing the currently logged-in user's information
/// </summary>
public interface IUserContext
{
    /// <summary>
    /// Current user ID, null when not logged in
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// Current user name
    /// </summary>
    string? UserName { get; }

    /// <summary>
    /// Current user email
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Whether authenticated
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Get all claims of the current user
    /// </summary>
    ClaimsPrincipal? User { get; }
}
