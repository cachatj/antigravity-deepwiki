using OpenDeepWiki.Entities;

namespace OpenDeepWiki.Services.Mcp;

/// <summary>
/// MCP usage log service interface
/// </summary>
public interface IMcpUsageLogService
{
    /// <summary>
    /// Record MCP usage logs asynchronously (without blocking the request)
    /// </summary>
    Task LogUsageAsync(McpUsageLog log);

    /// <summary>
    /// Aggregate logs for the specified date into daily statistics
    /// </summary>
    Task AggregateDailyStatisticsAsync(DateTime date);
}
