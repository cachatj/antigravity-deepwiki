using OpenDeepWiki.Models.Admin;

namespace OpenDeepWiki.Services.Admin;

/// <summary>
/// Admin MCP provider service interface
/// </summary>
public interface IAdminMcpProviderService
{
    Task<List<McpProviderDto>> GetProvidersAsync();
    Task<McpProviderDto> CreateProviderAsync(McpProviderRequest request);
    Task<bool> UpdateProviderAsync(string id, McpProviderRequest request);
    Task<bool> DeleteProviderAsync(string id);
    Task<Models.Admin.PagedResult<McpUsageLogDto>> GetUsageLogsAsync(McpUsageLogFilter filter);
    Task<McpUsageStatisticsResponse> GetMcpUsageStatisticsAsync(int days);
}
