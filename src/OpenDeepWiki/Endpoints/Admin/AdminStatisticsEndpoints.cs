using Microsoft.AspNetCore.Mvc;
using OpenDeepWiki.Services.Admin;

namespace OpenDeepWiki.Endpoints.Admin;

/// <summary>
/// Admin statistics endpoints
/// </summary>
public static class AdminStatisticsEndpoints
{
    public static RouteGroupBuilder MapAdminStatisticsEndpoints(this RouteGroupBuilder group)
    {
        var statisticsGroup = group.MapGroup("/statistics")
            .WithTags("Admin - Statistics");

        // Get dashboard statistics
        statisticsGroup.MapGet("/dashboard", async (
            [FromQuery] int days,
            [FromServices] IAdminStatisticsService statisticsService) =>
        {
            if (days <= 0) days = 7;
            var result = await statisticsService.GetDashboardStatisticsAsync(days);
            return Results.Ok(new { success = true, data = result });
        })
        .WithName("GetDashboardStatistics")
        .WithSummary("Get dashboard statistics");

        // Get token consumption statistics
        statisticsGroup.MapGet("/token-usage", async (
            [FromQuery] int days,
            [FromServices] IAdminStatisticsService statisticsService) =>
        {
            if (days <= 0) days = 7;
            var result = await statisticsService.GetTokenUsageStatisticsAsync(days);
            return Results.Ok(new { success = true, data = result });
        })
        .WithName("GetTokenUsageStatistics")
        .WithSummary("Get token consumption statistics");

        // Get MCP usage statistics
        statisticsGroup.MapGet("/mcp-usage", async (
            [FromQuery] int days,
            [FromServices] IAdminMcpProviderService mcpService) =>
        {
            if (days <= 0) days = 7;
            var result = await mcpService.GetMcpUsageStatisticsAsync(days);
            return Results.Ok(new { success = true, data = result });
        })
        .WithName("GetMcpUsageStatistics")
        .WithSummary("Get MCP usage statistics");

        return group;
    }
}
