namespace OpenDeepWiki.Models.Bookmark;

/// <summary>
/// Bookmark operation response
/// </summary>
public class BookmarkResponse
{
    /// <summary>
    /// Whether the operation succeeded
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message (only set on failure)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Bookmark record ID (only set on success)
    /// </summary>
    public string? BookmarkId { get; set; }
}
