using Microsoft.EntityFrameworkCore;
using OpenDeepWiki.EFCore;
using OpenDeepWiki.Entities;
using OpenDeepWiki.Services.Admin;

namespace OpenDeepWiki.Infrastructure;

/// <summary>
/// 数据库初始化服务
/// </summary>
public static class DbInitializer
{
    /// <summary>
    /// 初始化数据库（创建默认角色和OAuth提供商）
    /// </summary>
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // 确保数据库已创建
        if (context is DbContext dbContext)
        {
            await dbContext.Database.EnsureCreatedAsync();
        }

        // 初始化默认角色
        await InitializeRolesAsync(context);

        // 初始化默认管理员账户
        await InitializeAdminUserAsync(context);

        // 初始化OAuth提供商
        await InitializeOAuthProvidersAsync(context);

        // 初始化系统设置默认值（仅在首次运行时从环境变量创建）
        await SystemSettingDefaults.InitializeDefaultsAsync(configuration, context);

        // 初始化默认 MCP 提供商
        await InitializeMcpProvidersAsync(context);
    }

    private static async Task InitializeAdminUserAsync(IContext context)
    {
        const string adminEmail = "admin@opendeepwiki.com";
        const string adminPassword = "123456";

        // Clean up any legacy admin users that might cause ID mismatches
        var legacyAdmins = await context.Users
            .Where(u => (u.Email == "admin@routin.ai" || u.Name == "admin") && u.Id != "00000000-0000-0000-0000-000000000001" && !u.IsDeleted)
            .ToListAsync();
        
        if (legacyAdmins.Any())
        {
            context.Users.RemoveRange(legacyAdmins);
            await context.SaveChangesAsync();
        }

        var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Id == "00000000-0000-0000-0000-000000000001" && !u.IsDeleted);
        
        if (adminUser == null)
        {
            var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin" && !r.IsDeleted);
            if (adminRole == null) return;

            adminUser = new User
            {
                Id = "00000000-0000-0000-0000-000000000001", // Stable ID
                Name = "admin",
                Email = adminEmail,
                Password = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                Status = 1,
                IsSystem = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(adminUser);

            var userRole = new UserRole
            {
                Id = Guid.NewGuid().ToString(),
                UserId = adminUser.Id,
                RoleId = adminRole.Id,
                CreatedAt = DateTime.UtcNow
            };

            context.UserRoles.Add(userRole);
            await context.SaveChangesAsync();
        }
    }

    private static async Task InitializeRolesAsync(IContext context)
    {
        var roles = new[]
        {
            new Role
            {
                Id = "00000000-0000-0000-0000-admin-role-01", // Stable ID
                Name = "Admin",
                Description = "系统管理员",
                IsActive = true,
                IsSystemRole = true,
                CreatedAt = DateTime.UtcNow
            },
            new Role
            {
                Id = "00000000-0000-0000-0000-user-role-01", // Stable ID
                Name = "User",
                Description = "普通用户",
                IsActive = true,
                IsSystemRole = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        foreach (var role in roles)
        {
            var existingRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == role.Name && !r.IsDeleted);
            if (existingRole == null)
            {
                context.Roles.Add(role);
            }
            else if (existingRole.Id != role.Id)
            {
                // Fix role ID if it doesn't match our stable ID
                context.Roles.Remove(existingRole);
                await context.SaveChangesAsync();
                context.Roles.Add(role);
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task InitializeOAuthProvidersAsync(IContext context)
    {
        var providers = new[]
        {
            new OAuthProvider
            {
                Id = Guid.NewGuid().ToString(),
                Name = "github",
                DisplayName = "GitHub",
                AuthorizationUrl = "https://github.com/login/oauth/authorize",
                TokenUrl = "https://github.com/login/oauth/access_token",
                UserInfoUrl = "https://api.github.com/user",
                ClientId = "YOUR_GITHUB_CLIENT_ID",
                ClientSecret = "YOUR_GITHUB_CLIENT_SECRET",
                RedirectUri = "http://localhost:8080/api/oauth/github/callback",
                Scope = "user:email",
                UserInfoMapping = "{\"id\":\"id\",\"name\":\"login\",\"email\":\"email\",\"avatar\":\"avatar_url\"}",
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            },
            new OAuthProvider
            {
                Id = Guid.NewGuid().ToString(),
                Name = "gitee",
                DisplayName = "Gitee",
                AuthorizationUrl = "https://gitee.com/oauth/authorize",
                TokenUrl = "https://gitee.com/oauth/token",
                UserInfoUrl = "https://gitee.com/api/v5/user",
                ClientId = "YOUR_GITEE_CLIENT_ID",
                ClientSecret = "YOUR_GITEE_CLIENT_SECRET",
                RedirectUri = "http://localhost:8080/api/oauth/gitee/callback",
                Scope = "user_info emails",
                UserInfoMapping = "{\"id\":\"id\",\"name\":\"name\",\"email\":\"email\",\"avatar\":\"avatar_url\"}",
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            }
        };

        foreach (var provider in providers)
        {
            var exists = await context.OAuthProviders.AnyAsync(p => p.Name == provider.Name && !p.IsDeleted);
            if (!exists)
            {
                context.OAuthProviders.Add(provider);
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task InitializeMcpProvidersAsync(IContext context)
    {
        var providers = new[]
        {
            new McpProvider
            {
                Id = Guid.NewGuid().ToString(),
                Name = "DeepWiki Repository Tools",
                Description = "Provides tools for searching documentation and exploring repository structure.",
                ServerUrl = "/api/mcp/{owner}/{repo}",
                TransportType = "sse",
                RequiresApiKey = false, // Set to false to remove "API Key Required" message
                IsActive = true,
                SortOrder = 1,
                CreatedAt = DateTime.UtcNow
            }
        };

        foreach (var provider in providers)
        {
            var existing = await context.McpProviders.FirstOrDefaultAsync(p => p.Name == provider.Name && !p.IsDeleted);
            if (existing == null)
            {
                context.McpProviders.Add(provider);
            }
            else if (existing.RequiresApiKey)
            {
                existing.RequiresApiKey = false;
                context.McpProviders.Update(existing);
            }
        }

        await context.SaveChangesAsync();
    }
}
