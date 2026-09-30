using OpenDeepWiki.Models.Admin;

namespace OpenDeepWiki.Services.Admin;

/// <summary>
/// Admin repository service interface
/// </summary>
public interface IAdminRepositoryService
{
    Task<AdminRepositoryListResponse> GetRepositoriesAsync(int page, int pageSize, string? search, int? status);
    Task<AdminRepositoryDto?> GetRepositoryByIdAsync(string id);
    Task<bool> UpdateRepositoryAsync(string id, UpdateRepositoryRequest request);
    Task<bool> DeleteRepositoryAsync(string id);
    Task<bool> UpdateRepositoryStatusAsync(string id, int status);
    
    /// <summary>
    /// Sync statistics (stars, forks, etc.) for a single repository
    /// </summary>
    Task<SyncStatsResult> SyncRepositoryStatsAsync(string id);
    
    /// <summary>
    /// Sync statistics for repositories in batch
    /// </summary>
    Task<BatchSyncStatsResult> BatchSyncRepositoryStatsAsync(string[] ids);
    
    /// <summary>
    /// Delete repositories in batch
    /// </summary>
    Task<BatchDeleteResult> BatchDeleteRepositoriesAsync(string[] ids);

    /// <summary>
    /// Get detailed repository management info (branches, languages, incremental tasks)
    /// </summary>
    Task<AdminRepositoryManagementDto?> GetRepositoryManagementAsync(string id);

    /// <summary>
    /// Admin: trigger full regeneration
    /// </summary>
    Task<AdminRepositoryOperationResult> RegenerateRepositoryAsync(string id);

    /// <summary>
    /// Admin: trigger regeneration of a specific document
    /// </summary>
    Task<AdminRepositoryOperationResult> RegenerateDocumentAsync(string id, RegenerateRepositoryDocumentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Admin: manually update the content of a specific document
    /// </summary>
    Task<AdminRepositoryOperationResult> UpdateDocumentContentAsync(string id, UpdateRepositoryDocumentContentRequest request, CancellationToken cancellationToken = default);
}
