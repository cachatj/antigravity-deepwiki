namespace OpenDeepWiki.Endpoints.Admin;

/// <summary>
/// Admin endpoint registration
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
            .WithTags("Admin");

        // Register endpoints for each admin module
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
