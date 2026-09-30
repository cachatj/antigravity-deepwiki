using Microsoft.EntityFrameworkCore;
using OpenDeepWiki.EFCore;

namespace OpenDeepWiki.Endpoints;

/// <summary>
/// Public MCP provider endpoints (no authentication required)
/// </summary>
public static class McpProviderEndpoints
{
    private const string RepositoryScopedMcpPathTemplate = "/api/mcp/{owner}/{repo}";

    public static void MapMcpProviderEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/mcp-providers")
            .WithTags("MCP Providers");

        // Get all enabled MCP providers (public, no login required)
        group.MapGet("/", async (IContext context) =>
        {
            var providers = await context.McpProviders
                .Where(p => p.IsActive && !p.IsDeleted)
                .OrderBy(p => p.SortOrder)
                .ThenBy(p => p.Name)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Description,
                    ServerUrl = RepositoryScopedMcpPathTemplate,
                    p.TransportType,
                    p.RequiresApiKey,
                    p.ApiKeyObtainUrl,
                    p.IconUrl,
                    p.MaxRequestsPerDay,
                    p.AllowedTools,
                })
                .ToListAsync();

            return Results.Ok(new { success = true, data = providers });
        }).WithName("GetPublicMcpProviders")
          .WithSummary("Get public MCP provider list");
    }
}
