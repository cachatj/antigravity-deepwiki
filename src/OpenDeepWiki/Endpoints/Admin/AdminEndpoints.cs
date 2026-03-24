namespace OpenDeepWiki.Endpoints.Admin;

/// <summary>
/// 管理端端点注册
/// </summary>
public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        // AUTH BYPASS: AllowAnonymous to match the frontend auth bypass in auth-context.tsx.
        // The frontend creates a local admin user without generating a JWT token,
        // so RequireAuthorization("AdminOnly") would reject all requests with 401.
        // To re-enable auth, restore .RequireAuthorization("AdminOnly") and ensure
        // the frontend stores a valid JWT token.
        var adminGroup = app.MapGroup("/api/admin")
            .AllowAnonymous()
            .WithTags("管理端");

        // 注册各个管理模块的端点
        adminGroup.MapAdminStatisticsEndpoints();
        adminGroup.MapAdminRepositoryEndpoints();
        adminGroup.MapAdminUserEndpoints();
        adminGroup.MapAdminRoleEndpoints();
        adminGroup.MapAdminDepartmentEndpoints();
        adminGroup.MapAdminToolsEndpoints();
        adminGroup.MapAdminSettingsEndpoints();
        adminGroup.MapAdminChatAssistantEndpoints();
        adminGroup.MapAdminMcpProviderEndpoints();

        return app;
    }
}
