using System.ComponentModel.DataAnnotations;

namespace OpenDeepWiki.Entities;

/// <summary>
/// 系统设置实体
/// </summary>
public class SystemSetting : AggregateRoot<string>
{
    /// <summary>
    /// 设置键
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// 设置值
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// 设置描述
    /// </summary>
    [StringLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// 设置分类（general, ai, security 等）
    /// </summary>
    [StringLength(50)]
    public string Category { get; set; } = "general";
}
