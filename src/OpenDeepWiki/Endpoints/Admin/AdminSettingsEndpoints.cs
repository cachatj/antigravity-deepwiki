using Microsoft.AspNetCore.Mvc;
using OpenDeepWiki.Models.Admin;
using OpenDeepWiki.Services.Admin;

namespace OpenDeepWiki.Endpoints.Admin;

/// <summary>
/// 管理端设置端点
/// </summary>
public static class AdminSettingsEndpoints
{
    public static RouteGroupBuilder MapAdminSettingsEndpoints(this RouteGroupBuilder group)
    {
        var settingsGroup = group.MapGroup("/settings")
            .WithTags("管理端-设置");

        // 获取设置列表
        settingsGroup.MapGet("/", async (
            [FromQuery] string? category,
            [FromServices] IAdminSettingsService settingsService) =>
        {
            var settings = await settingsService.GetSettingsAsync(category);
            return Results.Ok(new { success = true, data = settings });
        })
        .WithName("AdminGetSettings")
        .WithSummary("获取设置列表");

        // 获取单个设置
        settingsGroup.MapGet("/{key}", async (
            string key,
            [FromServices] IAdminSettingsService settingsService) =>
        {
            var setting = await settingsService.GetSettingByKeyAsync(key);
            if (setting == null)
                return Results.NotFound(new { success = false, message = "设置不存在" });
            return Results.Ok(new { success = true, data = setting });
        })
        .WithName("AdminGetSettingByKey")
        .WithSummary("获取单个设置");

        // 更新设置
        settingsGroup.MapPut("/", async (
            [FromBody] List<UpdateSettingRequest> requests,
            [FromServices] IAdminSettingsService settingsService,
            [FromServices] IDynamicConfigManager configManager) =>
        {
            await settingsService.UpdateSettingsAsync(requests);
            
            // 刷新配置以应用新的设置
            await configManager.RefreshWikiGeneratorOptionsAsync();
            
            return Results.Ok(new { success = true, message = "设置更新成功" });
        })
        .WithName("AdminUpdateSettings")
        .WithSummary("更新设置");

        return group;
    }
}
