namespace OpenDeepWiki.Models;

/// <summary>
/// Repository document response
/// </summary>
public class RepositoryDocResponse
{
    /// <summary>
    /// Whether the document exists
    /// </summary>
    public bool Exists { get; set; } = true;

    /// <summary>
    /// Document path (slug)
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// Markdown content
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Source file list
    /// Records the source file paths read when generating this document
    /// </summary>
    public List<string> SourceFiles { get; set; } = [];
}
