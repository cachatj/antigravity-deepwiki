namespace OpenDeepWiki.Entities;

/// <summary>
/// MCP daily statistics aggregate entity
/// </summary>
public class McpDailyStatistics : AggregateRoot<string>
{
    /// <summary>
    /// Provider ID
    /// </summary>
    public string? McpProviderId { get; set; }

    /// <summary>
    /// Statistics date
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Total request count
    /// </summary>
    public long RequestCount { get; set; }

    /// <summary>
    /// Success count
    /// </summary>
    public long SuccessCount { get; set; }

    /// <summary>
    /// Error count
    /// </summary>
    public long ErrorCount { get; set; }

    /// <summary>
    /// Total elapsed time (milliseconds)
    /// </summary>
    public long TotalDurationMs { get; set; }

    /// <summary>
    /// Total input tokens
    /// </summary>
    public long InputTokens { get; set; }

    /// <summary>
    /// Total output tokens
    /// </summary>
    public long OutputTokens { get; set; }
}
