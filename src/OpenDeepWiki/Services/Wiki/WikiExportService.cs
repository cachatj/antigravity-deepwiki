using System.IO.Compression;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OpenDeepWiki.EFCore;

namespace OpenDeepWiki.Services.Wiki;

public class WikiExportService
{
    private readonly IContext _context;

    public WikiExportService(IContext context)
    {
        _context = context;
    }

    public async Task<byte[]> ExportWikiAsync(string org, string repo, string? branchName = null, string? languageCode = null)
    {
        var repository = await _context.Repositories
            .FirstOrDefaultAsync(r => r.OrgName == org && r.RepoName == repo && !r.IsDeleted);

        if (repository == null)
        {
            throw new KeyNotFoundException($"Repository {org}/{repo} not found.");
        }

        var branch = await _context.RepositoryBranches
            .FirstOrDefaultAsync(b => b.RepositoryId == repository.Id && (string.IsNullOrEmpty(branchName) || b.BranchName == branchName) && !b.IsDeleted);

        if (branch == null)
        {
            throw new InvalidOperationException("No active branch found for this repository.");
        }

        // Use provided language code or default to English
        var langToExport = string.IsNullOrEmpty(languageCode) ? "en" : languageCode;
        var branchLanguage = await _context.BranchLanguages
            .FirstOrDefaultAsync(bl => bl.RepositoryBranchId == branch.Id && bl.LanguageCode == langToExport && !bl.IsDeleted);

        if (branchLanguage == null)
        {
            // If requested language not found, try any from the branch
            branchLanguage = await _context.BranchLanguages
                .FirstOrDefaultAsync(bl => bl.RepositoryBranchId == branch.Id && !bl.IsDeleted);
        }

        if (branchLanguage == null)
        {
            throw new InvalidOperationException("No documentation found for this repository.");
        }

        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            await ExportCatalogToZipRecursiveAsync(archive, branchLanguage.Id, null, "");
        }

        return ms.ToArray();
    }

    private async Task ExportCatalogToZipRecursiveAsync(ZipArchive archive, string branchLanguageId, string? parentId, string currentPath)
    {
        var catalogs = await _context.DocCatalogs
            .Where(c => c.BranchLanguageId == branchLanguageId && c.ParentId == parentId && !c.IsDeleted)
            .OrderBy(c => c.Order)
            .ToListAsync();

        foreach (var catalog in catalogs)
        {
            var sanitizedTitle = SanitizeFileName(catalog.Title);
            var entryPath = string.IsNullOrEmpty(currentPath) ? sanitizedTitle : $"{currentPath}/{sanitizedTitle}";

            if (!string.IsNullOrEmpty(catalog.DocFileId))
            {
                var docFile = await _context.DocFiles.FirstOrDefaultAsync(f => f.Id == catalog.DocFileId && !f.IsDeleted);
                if (docFile != null && !string.IsNullOrEmpty(docFile.Content))
                {
                    var entry = archive.CreateEntry($"{entryPath}.md");
                    using var entryStream = entry.Open();
                    var contentBytes = Encoding.UTF8.GetBytes(docFile.Content);
                    await entryStream.WriteAsync(contentBytes);
                }
            }

            // Recurse into children
            await ExportCatalogToZipRecursiveAsync(archive, branchLanguageId, catalog.Id, entryPath);
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder();
        foreach (var c in fileName)
        {
            if (Array.IndexOf(invalidChars, c) < 0 && c != '/' && c != '\\')
            {
                sb.Append(c);
            }
            else
            {
                sb.Append('_');
            }
        }
        return sb.ToString();
    }
}
