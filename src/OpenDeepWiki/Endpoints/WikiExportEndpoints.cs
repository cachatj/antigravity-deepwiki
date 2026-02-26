namespace OpenDeepWiki.Endpoints;

using OpenDeepWiki.Services.Wiki;
using Microsoft.AspNetCore.Mvc;

public static class WikiExportEndpoints
{
    public static void MapWikiExportEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/repos")
            .WithTags("Wiki Export");

        group.MapGet("/{org}/{repo}/export", async (
            string org, 
            string repo, 
            [FromQuery] string? branch,
            [FromQuery] string? lang,
            WikiExportService exportService) =>
        {
            try
            {
                var zipContent = await exportService.ExportWikiAsync(org, repo, branch, lang);
                var fileName = $"{org}_{repo}_docs.zip";
                return Results.File(zipContent, "application/zip", fileName);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.Problem(ex.Message);
            }
        })
        .WithSummary("Export Wiki Documentation")
        .WithDescription("Generates and downloads a ZIP archive of the repository documentation in Markdown format.");
    }
}
