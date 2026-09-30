using Microsoft.EntityFrameworkCore;
using OpenDeepWiki.EFCore;
using OpenDeepWiki.Entities;

namespace OpenDeepWiki.Services.Mcp;

/// <summary>
/// MCP usage log service implementation
/// </summary>
public class McpUsageLogService : IMcpUsageLogService
{
    private readonly IContextFactory _contextFactory;
    private readonly ILogger<McpUsageLogService> _logger;

    public McpUsageLogService(IContextFactory contextFactory, ILogger<McpUsageLogService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public async Task LogUsageAsync(McpUsageLog log)
    {
        try
        {
            using var context = _contextFactory.CreateContext();
            if (string.IsNullOrWhiteSpace(log.UserId))
            {
                log.UserId = "anonymous";
            }

            if (string.IsNullOrWhiteSpace(log.McpProviderId))
            {
                log.McpProviderId = await context.McpProviders
                    .Where(p => p.IsActive && !p.IsDeleted)
                    .OrderBy(p => p.SortOrder)
                    .Select(p => p.Id)
                    .FirstOrDefaultAsync()
                    ?? "unknown";
            }

            log.Id = Guid.NewGuid().ToString();
            log.CreatedAt = DateTime.UtcNow;
            context.McpUsageLogs.Add(log);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write MCP usage log: {ToolName}", log.ToolName);
        }
    }

    public async Task AggregateDailyStatisticsAsync(DateTime date)
    {
        try
        {
            using var context = _contextFactory.CreateContext();
            var dateStart = date.Date;
            var dateEnd = dateStart.AddDays(1);

            var logs = await context.McpUsageLogs
                .Where(l => !l.IsDeleted && l.CreatedAt >= dateStart && l.CreatedAt < dateEnd)
                .GroupBy(l => l.McpProviderId)
                .Select(g => new
                {
                    McpProviderId = g.Key,
                    RequestCount = g.LongCount(),
                    SuccessCount = g.LongCount(l => l.ResponseStatus >= 200 && l.ResponseStatus < 300),
                    ErrorCount = g.LongCount(l => l.ResponseStatus >= 400),
                    TotalDurationMs = g.Sum(l => l.DurationMs),
                    InputTokens = g.Sum(l => (long)l.InputTokens),
                    OutputTokens = g.Sum(l => (long)l.OutputTokens)
                })
                .ToListAsync();

            foreach (var log in logs)
            {
                var existing = await context.McpDailyStatistics
                    .FirstOrDefaultAsync(s => s.McpProviderId == log.McpProviderId && s.Date == dateStart && !s.IsDeleted);

                if (existing != null)
                {
                    existing.RequestCount = log.RequestCount;
                    existing.SuccessCount = log.SuccessCount;
                    existing.ErrorCount = log.ErrorCount;
                    existing.TotalDurationMs = log.TotalDurationMs;
                    existing.InputTokens = log.InputTokens;
                    existing.OutputTokens = log.OutputTokens;
                    existing.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    context.McpDailyStatistics.Add(new McpDailyStatistics
                    {
                        Id = Guid.NewGuid().ToString(),
                        McpProviderId = log.McpProviderId,
                        Date = dateStart,
                        RequestCount = log.RequestCount,
                        SuccessCount = log.SuccessCount,
                        ErrorCount = log.ErrorCount,
                        TotalDurationMs = log.TotalDurationMs,
                        InputTokens = log.InputTokens,
                        OutputTokens = log.OutputTokens,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await context.SaveChangesAsync();
            _logger.LogInformation("MCP daily statistics aggregation completed: {Date}, {Count} provider records", dateStart.ToString("yyyy-MM-dd"), logs.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MCP daily statistics aggregation failed: {Date}", date.ToString("yyyy-MM-dd"));
        }
    }
}
