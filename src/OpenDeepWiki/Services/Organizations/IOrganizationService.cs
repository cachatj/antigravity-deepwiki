using OpenDeepWiki.Models;

namespace OpenDeepWiki.Services.Organizations;

/// <summary>
/// 组织服务接口
/// </summary>
public interface IOrganizationService
{
    Task<List<UserDepartmentInfo>> GetUserDepartmentsAsync(string userId);
    Task<List<DepartmentRepositoryInfo>> GetDepartmentRepositoriesAsync(string userId);
}

/// <summary>
/// 用户部门信息
/// </summary>
public class UserDepartmentInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsManager { get; set; }
}

/// <summary>
/// 部门仓库信息
/// </summary>
public class DepartmentRepositoryInfo
{
    public string RepositoryId { get; set; } = string.Empty;
    public string RepoName { get; set; } = string.Empty;
    public string OrgName { get; set; } = string.Empty;
    public string? GitUrl { get; set; }
    public int Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string DepartmentId { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
}
