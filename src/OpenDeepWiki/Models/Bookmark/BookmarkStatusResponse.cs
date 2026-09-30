namespace OpenDeepWiki.Models.Bookmark;

/// <summary>
/// Bookmark status response
/// </summary>
public class BookmarkStatusResponse
{
    /// <summary>
    /// Whether bookmarked
    /// </summary>
    public bool IsBookmarked { get; set; }

    /// <summary>
    /// Bookmarked time (only set when bookmarked)
    /// </summary>
    public DateTime? BookmarkedAt { get; set; }
}
