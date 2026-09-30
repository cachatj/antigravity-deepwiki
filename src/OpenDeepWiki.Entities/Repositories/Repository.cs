using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenDeepWiki.Entities;

/// <summary>
/// Repository processing status
/// </summary>
public enum RepositoryStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3
}

/// <summary>
/// Repository entity
/// </summary>
public class Repository : AggregateRoot<string>
{
    /// <summary>
    /// Owner user ID
    /// </summary>
    [Required]
    [StringLength(36)]
    public string OwnerUserId { get; set; } = string.Empty;

    /// <summary>
    /// Git URL
    /// </summary>
    [Required]
    [StringLength(500)]
    public string GitUrl { get; set; } = string.Empty;

    /// <summary>
    /// Repository name
    /// </summary>
    [Required]
    [StringLength(100)]
    public string RepoName { get; set; } = string.Empty;

    /// <summary>
    /// Repository organization
    /// </summary>
    [Required]
    [StringLength(100)]
    public string OrgName { get; set; } = string.Empty;

    /// <summary>
    /// Repository account
    /// </summary>
    [StringLength(200)]
    public string? AuthAccount { get; set; }

    /// <summary>
    /// Repository password (stored in plain text)
    /// </summary>
    [StringLength(500)]
    public string? AuthPassword { get; set; }

    /// <summary>
    /// Whether it is public
    /// </summary>
    public bool IsPublic { get; set; } = true;

    /// <summary>
    /// Repository processing status
    /// </summary>
    public RepositoryStatus Status { get; set; } = RepositoryStatus.Pending;

    /// <summary>
    /// Star count
    /// </summary>
    public int StarCount { get; set; }

    /// <summary>
    /// Fork count
    /// </summary>
    public int ForkCount { get; set; }

    /// <summary>
    /// Bookmark count
    /// </summary>
    public int BookmarkCount { get; set; } = 0;

    /// <summary>
    /// Subscription count
    /// </summary>
    public int SubscriptionCount { get; set; } = 0;

    /// <summary>
    /// View count
    /// </summary>
    public int ViewCount { get; set; } = 0;

    /// <summary>
    /// Primary programming language of the repository
    /// </summary>
    [StringLength(50)]
    public string? PrimaryLanguage { get; set; }

    /// <summary>
    /// Update check interval (minutes)
    /// null means the global default is used
    /// </summary>
    public int? UpdateIntervalMinutes { get; set; }

    /// <summary>
    /// Last update check time
    /// </summary>
    public DateTime? LastUpdateCheckAt { get; set; }

    /// <summary>
    /// Owner user navigation property
    /// </summary>
    [ForeignKey("OwnerUserId")]
    public virtual User? Owner { get; set; }
}
