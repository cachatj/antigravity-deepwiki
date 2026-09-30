namespace OpenDeepWiki.Models;

/// <summary>
/// Repository branches and languages response (from the database)
/// </summary>
public class RepositoryBranchesResponse
{
    /// <summary>
    /// Branch list
    /// </summary>
    public List<BranchItem> Branches { get; set; } = [];

    /// <summary>
    /// All available languages
    /// </summary>
    public List<string> Languages { get; set; } = [];

    /// <summary>
    /// Default branch
    /// </summary>
    public string DefaultBranch { get; set; } = string.Empty;

    /// <summary>
    /// Default language
    /// </summary>
    public string DefaultLanguage { get; set; } = string.Empty;
}

/// <summary>
/// Branch item
/// </summary>
public class BranchItem
{
    /// <summary>
    /// Branch name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Languages supported by this branch
    /// </summary>
    public List<string> Languages { get; set; } = [];
}

/// <summary>
/// Git platform branch list response (fetched from the remote API)
/// </summary>
public class GitBranchesResponse
{
    /// <summary>
    /// Branch list
    /// </summary>
    public List<GitBranchItem> Branches { get; set; } = [];

    /// <summary>
    /// Default branch
    /// </summary>
    public string? DefaultBranch { get; set; }

    /// <summary>
    /// Whether fetching branches is supported (platform support)
    /// </summary>
    public bool IsSupported { get; set; }
}

/// <summary>
/// GitBranch item
/// </summary>
public class GitBranchItem
{
    /// <summary>
    /// Branch name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is the default branch
    /// </summary>
    public bool IsDefault { get; set; }
}
