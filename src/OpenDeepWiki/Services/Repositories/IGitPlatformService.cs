namespace OpenDeepWiki.Services.Repositories;

/// <summary>
/// Git platform repository statistics
/// </summary>
public record GitRepoStats(int StarCount, int ForkCount);

/// <summary>
/// Git repository branch info
/// </summary>
public record GitBranchInfo(string Name, bool IsDefault);

/// <summary>
/// Result of fetching the branch list
/// </summary>
public record GitBranchesResult(List<GitBranchInfo> Branches, string? DefaultBranch, bool IsSupported);

/// <summary>
/// Git repository basic info
/// </summary>
public record GitRepoInfo(
    bool Exists,
    string? Name,
    string? Description,
    string? DefaultBranch,
    int StarCount,
    int ForkCount,
    string? Language,
    string? AvatarUrl
);

/// <summary>
/// Git platform service interface
/// </summary>
public interface IGitPlatformService
{
    /// <summary>
    /// Get repository statistics (star count, fork count)
    /// </summary>
    /// <param name="gitUrl">Git repository URL</param>
    /// <returns>Statistics, or null if fetching failed</returns>
    Task<GitRepoStats?> GetRepoStatsAsync(string gitUrl);

    /// <summary>
    /// Get the repository branch list
    /// </summary>
    /// <param name="gitUrl">Git repository URL</param>
    /// <returns>Branch list result</returns>
    Task<GitBranchesResult> GetBranchesAsync(string gitUrl);

    /// <summary>
    /// Check whether the repository exists and get basic info
    /// </summary>
    /// <param name="owner">Repository owner</param>
    /// <param name="repo">Repository name</param>
    /// <returns>Repository info</returns>
    Task<GitRepoInfo> CheckRepoExistsAsync(string owner, string repo);
}
