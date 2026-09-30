using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OpenDeepWiki.Entities;

/// <summary>
/// Local storage entity (used to store files)
/// </summary>
public class LocalStorage : AggregateRoot<string>
{
    /// <summary>
    /// File name
    /// </summary>
    [Required]
    [StringLength(255)]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// File extension
    /// </summary>
    [Required]
    [StringLength(50)]
    public string FileExtension { get; set; } = string.Empty;

    /// <summary>
    /// File size (bytes)
    /// </summary>
    public long FileSize { get; set; } = 0;

    /// <summary>
    /// File type (MIME type)
    /// </summary>
    [Required]
    [StringLength(100)]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// File storage path (relative path)
    /// </summary>
    [Required]
    [StringLength(500)]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// File hash (MD5/SHA256)
    /// </summary>
    [StringLength(128)]
    public string? FileHash { get; set; }

    /// <summary>
    /// File category (e.g. avatar, document, image, video, audio)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Uploader ID
    /// </summary>
    [Required]
    [StringLength(36)]
    public string UploaderId { get; set; } = string.Empty;

    /// <summary>
    /// Related business ID (e.g. user ID, repository ID)
    /// </summary>
    [StringLength(36)]
    public string? BusinessId { get; set; }

    /// <summary>
    /// Related business type (e.g. User, Warehouse, Document)
    /// </summary>
    [StringLength(50)]
    public string? BusinessType { get; set; }

    /// <summary>
    /// File status: 0 = temporary, 1 = permanent, 2 = deleted
    /// </summary>
    public int Status { get; set; } = 1;

    /// <summary>
    /// Whether it is publicly accessible
    /// </summary>
    public bool IsPublic { get; set; } = false;

    /// <summary>
    /// Access count
    /// </summary>
    public int AccessCount { get; set; } = 0;

    /// <summary>
    /// Last access time
    /// </summary>
    public DateTime? LastAccessAt { get; set; }

    /// <summary>
    /// Expiration time (temporary files)
    /// </summary>
    public DateTime? ExpiredAt { get; set; }

    /// <summary>
    /// Metadata (JSON format, stores custom attributes)
    /// </summary>
    [StringLength(2000)]
    public string? Metadata { get; set; }

    /// <summary>
    /// User navigation property (uploader)
    /// </summary>
    [ForeignKey("UploaderId")]
    public virtual User? Uploader { get; set; }

    /// <summary>
    /// Mark as deleted
    /// </summary>
    public override void MarkAsDeleted()
    {
        base.MarkAsDeleted();
        Status = 2;
    }

    /// <summary>
    /// Increment the access count
    /// </summary>
    public void IncrementAccessCount()
    {
        AccessCount++;
        LastAccessAt = DateTime.UtcNow;
    }
}
