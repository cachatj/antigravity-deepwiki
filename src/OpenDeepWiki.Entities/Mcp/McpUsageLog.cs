using System.ComponentModel.DataAnnotations;

namespace OpenDeepWiki.Entities;

/// <summary>
/// MCP usage log entity
/// </summary>
public class McpUsageLog : AggregateRoot<string>
{
    /// <summary>
    /// User ID (parsed from the Bearer token)
    /// </summary>
    [StringLength(100)]
    public string? UserId { get; set; }

    /// <summary>
    /// Provider ID
    /// </summary>
    [StringLength(100)]
    public string? McpProviderId { get; set; }

    /// <summary>
    /// Name of the tool called
    /// </summary>
    [Required]
    [StringLength(200)]
    public string ToolName { get; set; } = string.Empty;

    /// <summary>
    /// Request summary
    /// </summary>
    [StringLength(1000)]
    public string? RequestSummary { get; set; }

    /// <summary>
    /// HTTP status code
    /// </summary>
    public int ResponseStatus { get; set; }

    /// <summary>
    /// Response time (milliseconds)
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Input token count
    /// </summary>
    public int InputTokens { get; set; }

    /// <summary>
    /// Output token count
    /// </summary>
    public int OutputTokens { get; set; }

    /// <summary>
    /// Request IP
    /// </summary>
    [StringLength(50)]
    public string? IpAddress { get; set; }

    /// <summary>
    /// Client User-Agent
    /// </summary>
    [StringLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Error message
    /// </summary>
    [StringLength(2000)]
    public string? ErrorMessage { get; set; }
}
