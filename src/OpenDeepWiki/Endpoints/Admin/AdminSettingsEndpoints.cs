using Microsoft.AspNetCore.Mvc;
using OpenDeepWiki.Models.Admin;
using OpenDeepWiki.Services.Admin;

namespace OpenDeepWiki.Endpoints.Admin;

/// <summary>
/// Admin settings endpoints
/// </summary>
public static class AdminSettingsEndpoints
{
    public static RouteGroupBuilder MapAdminSettingsEndpoints(this RouteGroupBuilder group)
    {
        var settingsGroup = group.MapGroup("/settings")
            .WithTags("Admin - Settings");

        // Get settings list
        settingsGroup.MapGet("/", async (
            [FromQuery] string? category,
            [FromServices] IAdminSettingsService settingsService) =>
        {
            var settings = await settingsService.GetSettingsAsync(category);
            return Results.Ok(new { success = true, data = settings });
        })
        .WithName("AdminGetSettings")
        .WithSummary("Get settings list");

        // Get a single setting
        settingsGroup.MapGet("/{key}", async (
            string key,
            [FromServices] IAdminSettingsService settingsService) =>
        {
            var setting = await settingsService.GetSettingByKeyAsync(key);
            if (setting == null)
                return Results.NotFound(new { success = false, message = "Setting not found" });
            return Results.Ok(new { success = true, data = setting });
        })
        .WithName("AdminGetSettingByKey")
        .WithSummary("Get a single setting");

        // Update setting
        settingsGroup.MapPut("/", async (
            [FromBody] List<UpdateSettingRequest> requests,
            [FromServices] IAdminSettingsService settingsService,
            [FromServices] IDynamicConfigManager configManager) =>
        {
            await settingsService.UpdateSettingsAsync(requests);
            
            // Refresh configuration to apply the new setting
            await configManager.RefreshWikiGeneratorOptionsAsync();
            
            return Results.Ok(new { success = true, message = "Setting updated" });
        })
        .WithName("AdminUpdateSettings")
        .WithSummary("Update setting");

        return group;
    }
}
