using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using OpenDeepWiki.Entities;

namespace OpenDeepWiki.Services.Wiki;

/// <summary>
/// Builds a self-contained, offline-browsable HTML site (as a ZIP archive) from a wiki's document catalog.
/// Open index.html from the extracted folder and every page, diagram and code block works without a server.
/// </summary>
public class WikiHtmlExportService(ILogger<WikiHtmlExportService> logger)
{
    private const string AssetsDir = "assets";
    private const string PagesDir = "pages";

    private static readonly string[] BundledAssets =
    [
        "mermaid.min.js",
        "highlight.min.js",
        "hljs-github.min.css",
        "hljs-github-dark.min.css",
    ];

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseYamlFrontMatter()
        .UseAutoIdentifiers()
        .Build();

    public sealed record ExportRequest(
        string Owner,
        string Repo,
        string Branch,
        string LanguageCode,
        IReadOnlyList<DocCatalog> Catalogs);

    private sealed class PageNode
    {
        public required DocCatalog Catalog { get; init; }
        public required string Title { get; init; }
        public string? FilePath { get; set; } // zip path of the rendered page, null when the node has no content
        public List<PageNode> Children { get; } = [];
    }

    public byte[] Build(ExportRequest request)
    {
        var roots = BuildTree(request.Catalogs);
        var flat = new List<PageNode>();
        Flatten(roots, flat);

        AssignFilePaths(flat);

        var slugToFile = flat
            .Where(n => n.FilePath is not null)
            .GroupBy(n => NormalizeSlug(n.Catalog.Path))
            .ToDictionary(g => g.Key, g => g.First().FilePath!, StringComparer.OrdinalIgnoreCase);

        var pages = flat.Where(n => n.FilePath is not null).ToList();

        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            WriteAssets(archive);

            for (var i = 0; i < pages.Count; i++)
            {
                var page = pages[i];
                var prev = i > 0 ? pages[i - 1] : null;
                var next = i + 1 < pages.Count ? pages[i + 1] : null;
                var html = RenderPage(request, roots, page, page.FilePath!, prev, next, slugToFile);
                WriteText(archive, page.FilePath!, html);
            }

            // index.html shows the first page with content so the reader lands on the overview.
            var home = pages.FirstOrDefault();
            var indexHtml = home is null
                ? RenderEmptySite(request)
                : RenderPage(request, roots, home, "index.html", null, pages.Count > 1 ? pages[1] : null, slugToFile);
            WriteText(archive, "index.html", indexHtml);
        }

        return ms.ToArray();
    }

    // ---------------------------------------------------------------------
    // Tree / paths
    // ---------------------------------------------------------------------

    private static List<PageNode> BuildTree(IReadOnlyList<DocCatalog> catalogs)
    {
        var nodes = catalogs.ToDictionary(c => c.Id, c => new PageNode
        {
            Catalog = c,
            Title = string.IsNullOrWhiteSpace(c.Title) ? "Untitled" : c.Title.Trim(),
        });

        var roots = new List<PageNode>();
        foreach (var catalog in catalogs.OrderBy(c => c.Order))
        {
            var node = nodes[catalog.Id];
            if (catalog.ParentId is not null && nodes.TryGetValue(catalog.ParentId, out var parent))
            {
                parent.Children.Add(node);
            }
            else
            {
                roots.Add(node);
            }
        }

        return roots;
    }

    private static void Flatten(IEnumerable<PageNode> nodes, List<PageNode> output)
    {
        foreach (var node in nodes)
        {
            output.Add(node);
            Flatten(node.Children, output);
        }
    }

    private static void AssignFilePaths(List<PageNode> flat)
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "index.html" };

        foreach (var node in flat)
        {
            if (node.Catalog.DocFile is null || string.IsNullOrWhiteSpace(node.Catalog.DocFile.Content))
            {
                continue;
            }

            var slug = NormalizeSlug(node.Catalog.Path);
            var segments = (string.IsNullOrEmpty(slug) ? node.Title : slug)
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(SanitizeSegment)
                .ToList();

            if (segments.Count == 0)
            {
                segments.Add("untitled");
            }

            var basePath = $"{PagesDir}/{string.Join('/', segments)}";
            var candidate = $"{basePath}.html";
            for (var i = 2; !used.Add(candidate); i++)
            {
                candidate = $"{basePath}-{i}.html";
            }

            node.FilePath = candidate;
        }
    }

    private static string NormalizeSlug(string? path)
    {
        return (path ?? string.Empty).Trim().Trim('/');
    }

    private static string SanitizeSegment(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(value.Select(ch => invalid.Contains(ch) || ch == '#' || ch == '?' || ch == '%' ? '_' : ch).ToArray())
            .Trim()
            .TrimEnd('.', ' ');
        return string.IsNullOrWhiteSpace(cleaned) ? "untitled" : cleaned;
    }

    /// <summary>Relative URL from one zip entry to another, e.g. pages/a/b.html -> assets/x.css = ../../assets/x.css</summary>
    private static string Relative(string fromFile, string toFile)
    {
        var depth = fromFile.Count(c => c == '/');
        var prefix = string.Concat(Enumerable.Repeat("../", depth));
        return prefix + string.Join('/', toFile.Split('/').Select(Uri.EscapeDataString));
    }

    // ---------------------------------------------------------------------
    // Markdown rendering
    // ---------------------------------------------------------------------

    private string RenderMarkdown(string markdown, string currentFile, ExportRequest request, IReadOnlyDictionary<string, string> slugToFile)
    {
        var document = Markdown.Parse(markdown, Pipeline);

        foreach (var link in document.Descendants<LinkInline>())
        {
            if (link.IsImage || string.IsNullOrWhiteSpace(link.Url))
            {
                continue;
            }

            var rewritten = TryResolveInternalLink(link.Url, currentFile, request, slugToFile);
            if (rewritten is not null)
            {
                link.Url = rewritten;
            }
        }

        var sb = new StringBuilder();
        var renderer = new HtmlRenderer(new StringWriter(sb));
        Pipeline.Setup(renderer);
        renderer.ObjectRenderers.Replace<CodeBlockRenderer>(new MermaidAwareCodeBlockRenderer());
        renderer.Render(document);
        renderer.Writer.Flush();
        return sb.ToString();
    }

    /// <summary>
    /// Turns links that point at other wiki pages (by slug, absolute site path or .md filename) into relative HTML links.
    /// Returns null when the link is external or does not match any page.
    /// </summary>
    private static string? TryResolveInternalLink(string url, string currentFile, ExportRequest request, IReadOnlyDictionary<string, string> slugToFile)
    {
        if (url.StartsWith('#') || url.Contains("://", StringComparison.Ordinal) || url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var fragment = string.Empty;
        var hashIndex = url.IndexOf('#');
        if (hashIndex >= 0)
        {
            fragment = url[hashIndex..];
            url = url[..hashIndex];
        }

        var queryIndex = url.IndexOf('?');
        if (queryIndex >= 0)
        {
            url = url[..queryIndex];
        }

        var candidate = WebUtility.UrlDecode(url).Trim();
        if (candidate.StartsWith("./", StringComparison.Ordinal))
        {
            candidate = candidate[2..];
        }

        candidate = candidate.Trim('/');

        // Strip the site prefix used by the live app: /{owner}/{repo}/{slug}
        var sitePrefix = $"{request.Owner}/{request.Repo}/";
        if (candidate.StartsWith(sitePrefix, StringComparison.OrdinalIgnoreCase))
        {
            candidate = candidate[sitePrefix.Length..];
        }

        if (candidate.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            candidate = candidate[..^3];
        }

        if (candidate.Length == 0)
        {
            return null;
        }

        if (slugToFile.TryGetValue(candidate, out var target))
        {
            return Relative(currentFile, target) + fragment;
        }

        // Last segment match, for links written as bare page names.
        var lastSegment = candidate.Split('/').Last();
        var match = slugToFile.Keys.FirstOrDefault(k => k.Split('/').Last().Equals(lastSegment, StringComparison.OrdinalIgnoreCase));
        return match is null ? null : Relative(currentFile, slugToFile[match]) + fragment;
    }

    /// <summary>Emits ```mermaid fences as &lt;pre class="mermaid"&gt; so Mermaid renders them in the browser.</summary>
    private sealed class MermaidAwareCodeBlockRenderer : CodeBlockRenderer
    {
        protected override void Write(HtmlRenderer renderer, CodeBlock obj)
        {
            if (obj is FencedCodeBlock fenced &&
                string.Equals(fenced.Info?.Trim(), "mermaid", StringComparison.OrdinalIgnoreCase))
            {
                renderer.EnsureLine();
                renderer.Write("<pre class=\"mermaid\">");
                renderer.WriteLeafRawLines(obj, true, true);
                renderer.WriteLine("</pre>");
                return;
            }

            base.Write(renderer, obj);
        }
    }

    // ---------------------------------------------------------------------
    // Page shell
    // ---------------------------------------------------------------------

    private string RenderPage(
        ExportRequest request,
        List<PageNode> roots,
        PageNode page,
        string outputFile,
        PageNode? prev,
        PageNode? next,
        IReadOnlyDictionary<string, string> slugToFile)
    {
        var body = RenderMarkdown(page.Catalog.DocFile!.Content, outputFile, request, slugToFile);
        var siteTitle = $"{request.Owner}/{request.Repo}";
        var pageTitle = WebUtility.HtmlEncode(page.Title);

        var sb = new StringBuilder();
        sb.Append("<!doctype html>\n<html lang=\"").Append(WebUtility.HtmlEncode(request.LanguageCode)).Append("\">\n<head>\n");
        sb.Append("<meta charset=\"utf-8\">\n<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n");
        sb.Append("<title>").Append(pageTitle).Append(" · ").Append(WebUtility.HtmlEncode(siteTitle)).Append("</title>\n");
        sb.Append("<script>try{var t=localStorage.getItem('odw-theme');if(!t){t=matchMedia('(prefers-color-scheme: dark)').matches?'dark':'light'}document.documentElement.dataset.theme=t}catch(e){document.documentElement.dataset.theme='light'}</script>\n");
        sb.Append("<link rel=\"stylesheet\" href=\"").Append(Relative(outputFile, $"{AssetsDir}/site.css")).Append("\">\n");
        sb.Append("<link rel=\"stylesheet\" id=\"hljs-light\" href=\"").Append(Relative(outputFile, $"{AssetsDir}/hljs-github.min.css")).Append("\">\n");
        sb.Append("<link rel=\"stylesheet\" id=\"hljs-dark\" href=\"").Append(Relative(outputFile, $"{AssetsDir}/hljs-github-dark.min.css")).Append("\" disabled>\n");
        sb.Append("</head>\n<body>\n");

        // Sidebar
        sb.Append("<aside class=\"sidebar\">\n<div class=\"brand\">\n");
        sb.Append("<a class=\"brand-link\" href=\"").Append(Relative(outputFile, "index.html")).Append("\">").Append(WebUtility.HtmlEncode(siteTitle)).Append("</a>\n");
        sb.Append("<div class=\"brand-meta\">").Append(WebUtility.HtmlEncode(request.Branch)).Append(" · ").Append(WebUtility.HtmlEncode(request.LanguageCode)).Append("</div>\n</div>\n");
        sb.Append("<input class=\"filter\" type=\"search\" placeholder=\"Filter pages…\" aria-label=\"Filter pages\">\n");
        sb.Append("<nav class=\"toc\">\n");
        RenderToc(sb, roots, outputFile, page);
        sb.Append("</nav>\n</aside>\n");

        // Main
        sb.Append("<div class=\"main\">\n<header class=\"topbar\">\n");
        sb.Append("<button class=\"menu-toggle\" aria-label=\"Toggle navigation\">☰</button>\n");
        sb.Append("<div class=\"crumbs\">").Append(RenderBreadcrumbs(roots, page)).Append("</div>\n");
        sb.Append("<button class=\"theme-toggle\" aria-label=\"Toggle theme\">◐</button>\n</header>\n");
        sb.Append("<article class=\"content\">\n").Append(body).Append("\n</article>\n");

        sb.Append("<nav class=\"pager\">\n");
        if (prev is not null)
        {
            sb.Append("<a class=\"prev\" href=\"").Append(Relative(outputFile, prev.FilePath!)).Append("\"><span>Previous</span>").Append(WebUtility.HtmlEncode(prev.Title)).Append("</a>\n");
        }
        else
        {
            sb.Append("<span></span>\n");
        }

        if (next is not null)
        {
            sb.Append("<a class=\"next\" href=\"").Append(Relative(outputFile, next.FilePath!)).Append("\"><span>Next</span>").Append(WebUtility.HtmlEncode(next.Title)).Append("</a>\n");
        }
        sb.Append("</nav>\n");

        sb.Append("<footer class=\"footer\">Exported from OpenDeepWiki · ").Append(DateTime.UtcNow.ToString("yyyy-MM-dd")).Append(" UTC</footer>\n</div>\n");

        sb.Append("<script src=\"").Append(Relative(outputFile, $"{AssetsDir}/highlight.min.js")).Append("\"></script>\n");
        sb.Append("<script src=\"").Append(Relative(outputFile, $"{AssetsDir}/mermaid.min.js")).Append("\"></script>\n");
        sb.Append("<script src=\"").Append(Relative(outputFile, $"{AssetsDir}/site.js")).Append("\"></script>\n");
        sb.Append("</body>\n</html>\n");
        return sb.ToString();
    }

    private static void RenderToc(StringBuilder sb, List<PageNode> nodes, string currentFile, PageNode active)
    {
        sb.Append("<ul>\n");
        foreach (var node in nodes)
        {
            var isActive = ReferenceEquals(node, active);
            var title = WebUtility.HtmlEncode(node.Title);
            sb.Append("<li").Append(isActive ? " class=\"active\"" : string.Empty).Append('>');

            if (node.FilePath is not null)
            {
                sb.Append("<a href=\"").Append(Relative(currentFile, node.FilePath)).Append("\">").Append(title).Append("</a>");
            }
            else
            {
                sb.Append("<span class=\"group\">").Append(title).Append("</span>");
            }

            if (node.Children.Count > 0)
            {
                RenderToc(sb, node.Children, currentFile, active);
            }

            sb.Append("</li>\n");
        }
        sb.Append("</ul>\n");
    }

    private static string RenderBreadcrumbs(List<PageNode> roots, PageNode page)
    {
        var chain = new List<PageNode>();
        FindChain(roots, page, chain);
        return string.Join(" <span class=\"sep\">/</span> ", chain.Select(n => WebUtility.HtmlEncode(n.Title)));
    }

    private static bool FindChain(List<PageNode> nodes, PageNode target, List<PageNode> chain)
    {
        foreach (var node in nodes)
        {
            chain.Add(node);
            if (ReferenceEquals(node, target) || FindChain(node.Children, target, chain))
            {
                return true;
            }
            chain.RemoveAt(chain.Count - 1);
        }
        return false;
    }

    private static string RenderEmptySite(ExportRequest request)
    {
        return $"<!doctype html><html><head><meta charset=\"utf-8\"><title>{WebUtility.HtmlEncode(request.Owner)}/{WebUtility.HtmlEncode(request.Repo)}</title></head>" +
               "<body><p>This wiki has no generated pages yet.</p></body></html>";
    }

    // ---------------------------------------------------------------------
    // Assets
    // ---------------------------------------------------------------------

    private void WriteAssets(ZipArchive archive)
    {
        var assetRoot = ResolveAssetRoot();
        foreach (var asset in BundledAssets)
        {
            var source = Path.Combine(assetRoot, asset);
            if (!File.Exists(source))
            {
                logger.LogWarning("Export asset {Asset} not found at {Path}; the exported site will reference a missing file", asset, source);
                continue;
            }

            var entry = archive.CreateEntry($"{AssetsDir}/{asset}", CompressionLevel.Optimal);
            using var target = entry.Open();
            using var file = File.OpenRead(source);
            file.CopyTo(target);
        }

        WriteText(archive, $"{AssetsDir}/site.css", SiteCss);
        WriteText(archive, $"{AssetsDir}/site.js", SiteJs);
    }

    private static string ResolveAssetRoot()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "ExportAssets"),
            Path.Combine(Directory.GetCurrentDirectory(), "ExportAssets"),
        };
        return candidates.FirstOrDefault(Directory.Exists) ?? candidates[0];
    }

    private static void WriteText(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
        using var stream = entry.Open();
        var bytes = Encoding.UTF8.GetBytes(content);
        stream.Write(bytes, 0, bytes.Length);
    }

    private const string SiteCss = """
        :root{--bg:#ffffff;--fg:#1f2328;--muted:#656d76;--border:#d0d7de;--accent:#0969da;--side:#f6f8fa;--code:#f6f8fa;--active:#ddf4ff}
        :root[data-theme="dark"]{--bg:#0d1117;--fg:#e6edf3;--muted:#8b949e;--border:#30363d;--accent:#58a6ff;--side:#161b22;--code:#161b22;--active:#1f3a5f}
        *{box-sizing:border-box}
        html,body{margin:0;padding:0;background:var(--bg);color:var(--fg);font:16px/1.6 -apple-system,BlinkMacSystemFont,"Segoe UI",Helvetica,Arial,sans-serif}
        a{color:var(--accent);text-decoration:none}a:hover{text-decoration:underline}
        body{display:flex;min-height:100vh}
        .sidebar{width:300px;flex:none;background:var(--side);border-right:1px solid var(--border);padding:16px;position:sticky;top:0;height:100vh;overflow-y:auto}
        .brand-link{font-weight:700;font-size:1.05rem;color:var(--fg)}
        .brand-meta{font-size:.8rem;color:var(--muted);margin-top:2px;margin-bottom:12px}
        .filter{width:100%;padding:6px 10px;border:1px solid var(--border);border-radius:6px;background:var(--bg);color:var(--fg);margin-bottom:12px}
        .toc ul{list-style:none;margin:0;padding-left:0}
        .toc ul ul{padding-left:14px;border-left:1px solid var(--border);margin-left:6px}
        .toc li{margin:2px 0}
        .toc a,.toc .group{display:block;padding:4px 8px;border-radius:6px;color:var(--fg);font-size:.92rem}
        .toc .group{color:var(--muted);font-weight:600}
        .toc a:hover{background:var(--active);text-decoration:none}
        .toc li.active>a{background:var(--active);font-weight:600}
        .toc li.hidden{display:none}
        .main{flex:1;min-width:0;display:flex;flex-direction:column}
        .topbar{display:flex;align-items:center;gap:12px;padding:10px 24px;border-bottom:1px solid var(--border);position:sticky;top:0;background:var(--bg);z-index:5}
        .crumbs{flex:1;font-size:.9rem;color:var(--muted);overflow:hidden;white-space:nowrap;text-overflow:ellipsis}
        .crumbs .sep{margin:0 6px;opacity:.6}
        .menu-toggle,.theme-toggle{border:1px solid var(--border);background:var(--side);color:var(--fg);border-radius:6px;padding:4px 10px;cursor:pointer;font-size:1rem}
        .menu-toggle{display:none}
        .content{max-width:900px;width:100%;margin:0 auto;padding:24px 32px 48px;flex:1}
        .content h1,.content h2,.content h3{line-height:1.3;margin-top:1.6em}
        .content h1{border-bottom:1px solid var(--border);padding-bottom:.3em}
        .content h2{border-bottom:1px solid var(--border);padding-bottom:.2em}
        .content img{max-width:100%}
        .content table{border-collapse:collapse;display:block;overflow-x:auto;max-width:100%}
        .content th,.content td{border:1px solid var(--border);padding:6px 12px}
        .content th{background:var(--side)}
        .content blockquote{margin:0;padding:0 1em;color:var(--muted);border-left:4px solid var(--border)}
        .content code{font-family:ui-monospace,SFMono-Regular,Menlo,Consolas,monospace;font-size:.9em;background:var(--code);padding:.15em .35em;border-radius:4px}
        .content pre{position:relative;background:var(--code);border:1px solid var(--border);border-radius:8px;padding:14px;overflow:auto}
        .content pre code{background:none;padding:0;font-size:.85em}
        .content pre.mermaid{background:transparent;border:none;text-align:center;overflow:visible}
        .content pre.mermaid svg{max-width:100%;height:auto}
        .copy-btn{position:absolute;top:8px;right:8px;font-size:.75rem;padding:2px 8px;border:1px solid var(--border);border-radius:4px;background:var(--bg);color:var(--muted);cursor:pointer;opacity:0;transition:opacity .15s}
        .content pre:hover .copy-btn{opacity:1}
        .pager{display:flex;justify-content:space-between;gap:16px;max-width:900px;width:100%;margin:0 auto;padding:0 32px 32px}
        .pager a{display:flex;flex-direction:column;border:1px solid var(--border);border-radius:8px;padding:10px 16px;color:var(--fg);min-width:0;max-width:48%}
        .pager a span{font-size:.75rem;color:var(--muted)}
        .pager .next{margin-left:auto;text-align:right}
        .pager a:hover{border-color:var(--accent);text-decoration:none}
        .footer{text-align:center;color:var(--muted);font-size:.8rem;padding:16px;border-top:1px solid var(--border)}
        @media (max-width:900px){
          .sidebar{position:fixed;left:0;top:0;transform:translateX(-100%);transition:transform .2s;z-index:10;width:280px}
          body.nav-open .sidebar{transform:none;box-shadow:0 0 0 100vmax rgba(0,0,0,.4)}
          .menu-toggle{display:inline-block}
          .content,.pager{padding-left:16px;padding-right:16px}
        }
        """;

    private const string SiteJs = """
        (function(){
          var root=document.documentElement;
          function applyTheme(t){
            root.dataset.theme=t;
            var l=document.getElementById('hljs-light'),d=document.getElementById('hljs-dark');
            if(l&&d){l.disabled=t==='dark';d.disabled=t!=='dark';}
          }
          applyTheme(root.dataset.theme||'light');

          var themeBtn=document.querySelector('.theme-toggle');
          if(themeBtn){themeBtn.addEventListener('click',function(){
            var t=root.dataset.theme==='dark'?'light':'dark';
            try{localStorage.setItem('odw-theme',t)}catch(e){}
            applyTheme(t);
            location.reload(); // Mermaid theme is fixed at init time
          });}

          var menuBtn=document.querySelector('.menu-toggle');
          if(menuBtn){menuBtn.addEventListener('click',function(){document.body.classList.toggle('nav-open')});}
          document.addEventListener('click',function(e){
            if(document.body.classList.contains('nav-open')&&!e.target.closest('.sidebar')&&!e.target.closest('.menu-toggle')){document.body.classList.remove('nav-open')}
          });

          var filter=document.querySelector('.filter');
          if(filter){filter.addEventListener('input',function(){
            var q=filter.value.trim().toLowerCase();
            document.querySelectorAll('.toc li').forEach(function(li){
              var text=li.textContent.toLowerCase();
              li.classList.toggle('hidden',q.length>0&&text.indexOf(q)===-1);
            });
          });}

          if(window.hljs){document.querySelectorAll('pre code').forEach(function(el){try{hljs.highlightElement(el)}catch(e){}});}

          document.querySelectorAll('.content pre:not(.mermaid)').forEach(function(pre){
            var btn=document.createElement('button');btn.className='copy-btn';btn.textContent='Copy';
            btn.addEventListener('click',function(){
              var code=pre.querySelector('code');var text=code?code.innerText:pre.innerText;
              var done=function(){btn.textContent='Copied';setTimeout(function(){btn.textContent='Copy'},1500)};
              if(navigator.clipboard&&navigator.clipboard.writeText){navigator.clipboard.writeText(text).then(done,done)}
              else{var ta=document.createElement('textarea');ta.value=text;document.body.appendChild(ta);ta.select();try{document.execCommand('copy')}catch(e){}document.body.removeChild(ta);done()}
            });
            pre.appendChild(btn);
          });

          if(window.mermaid){
            mermaid.initialize({startOnLoad:true,theme:root.dataset.theme==='dark'?'dark':'default',securityLevel:'loose',suppressErrorRendering:true});
          }

          // Scroll the active TOC entry into view
          var active=document.querySelector('.toc li.active>a');
          if(active&&active.scrollIntoView){active.scrollIntoView({block:'center'})}
        })();
        """;
}
