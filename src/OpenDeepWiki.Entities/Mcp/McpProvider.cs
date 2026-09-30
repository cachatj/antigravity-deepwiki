using System.ComponentModel.DataAnnotations;

namespace OpenDeepWiki.Entities;

/// <summary>
/// MCP provider configuration entity (managed by administrators)
/// </summary>
public class McpProvider : AggregateRoot<string>
{
    /// <summary>
    /// Provider name
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Provider description
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// MCP service endpoint address
    /// </summary>
    [Required]
    [StringLength(500)]
    public string ServerUrl { get; set; } = string.Empty;

    /// <summary>
    /// Transport type: sse | streamable_http
    /// </summary>
    [Required]
    [StringLength(50)]
    public string TransportType { get; set; } = "streamable_http";

    /// <summary>
    /// Whether the user must provide an API key
    /// </summary>
    public bool RequiresApiKey { get; set; } = true;

    /// <summary>
    /// URL where users can obtain an API key (filled in by administrators)
    /// </summary>
    [StringLength(500)]
    public string? ApiKeyObtainUrl { get; set; }

    /// <summary>
    /// System-level API key (attached automatically when RequiresApiKey=false)
    /// </summary>
    [StringLength(500)]
    public string? SystemApiKey { get; set; }

    /// <summary>
    /// Associated AI model configuration ID (FK -> ModelConfig)
    /// </summary>
    [StringLength(100)]
    public string? ModelConfigId { get; set; }

    /// <summary>
    /// JSON array of administrator-configured request types
    /// </summary>
    public string? RequestTypes { get; set; }

    /// <summary>
    /// JSON array of tools allowed to be exposed
    /// </summary>
    public string? AllowedTools { get; set; }

    /// <summary>
    /// Whether it is enabled
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Sort order
    /// </summary>
    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// Provider icon URL
    /// </summary>
    [StringLength(500)]
    public string? IconUrl { get; set; }

    /// <summary>
    /// Daily request limit (0 = unlimited)
    /// </summary>
    public int MaxRequestsPerDay { get; set; } = 0;
}
