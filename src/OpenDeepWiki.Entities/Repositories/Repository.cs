using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenDeepWiki.Entities;

/// <summary>
/// 仓库处理状态
/// </summary>
public enum RepositoryStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3
}

/// <summary>
/// 仓库实体
/// </summary>
public class Repository : AggregateRoot<string>
{
    /// <summary>
    /// 所属用户ID
    /// </summary>
    [Required]
    [StringLength(36)]
    public string OwnerUserId { get; set; } = string.Empty;

    /// <summary>
    /// Git地址
    /// </summary>
    [Required]
    [StringLength(500)]
    public string GitUrl { get; set; } = string.Empty;

    /// <summary>
    /// 仓库名称
    /// </summary>
    [Required]
    [StringLength(100)]
    public string RepoName { get; set; } = string.Empty;

    /// <summary>
    /// 仓库组织
    /// </summary>
    [Required]
    [StringLength(100)]
    public string OrgName { get; set; } = string.Empty;

    /// <summary>
    /// 仓库账户
    /// </summary>
    [StringLength(200)]
    public string? AuthAccount { get; set; }

    /// <summary>
    /// 仓库密码（明文存储）
    /// </summary>
    [StringLength(500)]
    public string? AuthPassword { get; set; }

    /// <summary>
    /// 是否公开
    /// </summary>
    public bool IsPublic { get; set; } = true;

    /// <summary>
    /// 仓库处理状态
    /// </summary>
    public RepositoryStatus Status { get; set; } = RepositoryStatus.Pending;

    /// <summary>
    /// Star数量
    /// </summary>
    public int StarCount { get; set; }

    /// <summary>
    /// Fork数量
    /// </summary>
    public int ForkCount { get; set; }

    /// <summary>
    /// 收藏数量
    /// </summary>
    public int BookmarkCount { get; set; } = 0;

    /// <summary>
    /// 订阅数量
    /// </summary>
    public int SubscriptionCount { get; set; } = 0;

    /// <summary>
    /// 浏览数量
    /// </summary>
    public int ViewCount { get; set; } = 0;

    /// <summary>
    /// 仓库主要编程语言
    /// </summary>
    [StringLength(50)]
    public string? PrimaryLanguage { get; set; }

    /// <summary>
    /// 更新检查间隔（分钟）
    /// null 表示使用全局默认值
    /// </summary>
    public int? UpdateIntervalMinutes { get; set; }

    /// <summary>
    /// 上次检查更新时间
    /// </summary>
    public DateTime? LastUpdateCheckAt { get; set; }

    /// <summary>
    /// 所属用户导航属性
    /// </summary>
    [ForeignKey("OwnerUserId")]
    public virtual User? Owner { get; set; }
}
