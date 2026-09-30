using OpenDeepWiki.Entities;

namespace OpenDeepWiki.Models;

/// <summary>
/// Repository list response
/// </summary>
public class RepositoryListResponse
{
    /// <summary>
    /// Repository list
    /// </summary>
    public List<RepositoryItemResponse> Items { get; set; } = [];

    /// <summary>
    /// Total count
    /// </summary>
    public int Total { get; set; }
}

/// <summary>
/// Repository list item response
/// </summary>
public class RepositoryItemResponse
{
    /// <summary>
    /// Repository ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Organization name
    /// </summary>
    public string OrgName { get; set; } = string.Empty;

    /// <summary>
    /// Repository name
    /// </summary>
    public string RepoName { get; set; } = string.Empty;

    /// <summary>
    /// Git URL
    /// </summary>
    public string GitUrl { get; set; } = string.Empty;

    /// <summary>
    /// Processing status
    /// </summary>
    public RepositoryStatus Status { get; set; }

    /// <summary>
    /// Status name
    /// </summary>
    public string StatusName => Status.ToString();

    /// <summary>
    /// Whether public
    /// </summary>
    public bool IsPublic { get; set; }

    /// <summary>
    /// Whether a password is set
    /// </summary>
    public bool HasPassword { get; set; }

    /// <summary>
    /// Created time
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Updated time
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Star count
    /// </summary>
    public int StarCount { get; set; }

    /// <summary>
    /// Fork count
    /// </summary>
    public int ForkCount { get; set; }

    /// <summary>
    /// Primary programming language
    /// </summary>
    public string? PrimaryLanguage { get; set; }
}
