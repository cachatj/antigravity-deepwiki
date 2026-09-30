using System.ComponentModel.DataAnnotations;

namespace OpenDeepWiki.Entities;

/// <summary>
/// Chat assistant configuration entity
/// Stores the administrator-configured models, MCPs, skills, etc.
/// </summary>
public class ChatAssistantConfig : AggregateRoot<Guid>
{
    /// <summary>
    /// Whether the chat assistant feature is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = false;

    /// <summary>
    /// Enabled model ID list (JSON array)
    /// </summary>
    [StringLength(2000)]
    public string? EnabledModelIds { get; set; }

    /// <summary>
    /// Enabled MCP ID list (JSON array)
    /// </summary>
    [StringLength(2000)]
    public string? EnabledMcpIds { get; set; }

    /// <summary>
    /// Enabled skill ID list (JSON array)
    /// </summary>
    [StringLength(2000)]
    public string? EnabledSkillIds { get; set; }

    /// <summary>
    /// Default model ID
    /// </summary>
    [StringLength(100)]
    public string? DefaultModelId { get; set; }

    /// <summary>
    /// Whether image upload is enabled
    /// </summary>
    public bool EnableImageUpload { get; set; } = false;
}
