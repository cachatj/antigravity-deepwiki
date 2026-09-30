using OpenDeepWiki.Models.Admin;

namespace OpenDeepWiki.Services.Admin;

/// <summary>
/// Admin chat assistant configuration service interface
/// </summary>
public interface IAdminChatAssistantService
{
    /// <summary>
    /// Get the chat assistant configuration (including option lists)
    /// </summary>
    Task<ChatAssistantConfigOptionsDto> GetConfigWithOptionsAsync();

    /// <summary>
    /// Get the chat assistant configuration
    /// </summary>
    Task<ChatAssistantConfigDto> GetConfigAsync();

    /// <summary>
    /// Update the chat assistant configuration
    /// </summary>
    Task<ChatAssistantConfigDto> UpdateConfigAsync(UpdateChatAssistantConfigRequest request);
}
