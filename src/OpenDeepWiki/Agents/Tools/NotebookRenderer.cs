using System.Text;
using System.Text.Json;

namespace OpenDeepWiki.Agents.Tools;

/// <summary>
/// Renders Jupyter notebooks (.ipynb) into a compact, readable text form for the agent tools.
/// Raw notebook JSON interleaves escaped source fragments with large outputs (base64 images,
/// dataframe dumps), which wastes the model's context. This renderer emits each cell in
/// execution order as a fenced code or markdown block and keeps only a short, textual summary
/// of outputs.
/// </summary>
public static class NotebookRenderer
{
    /// <summary>Maximum output lines kept per output entry.</summary>
    private const int MaxOutputLines = 15;

    /// <summary>Maximum characters kept per output line.</summary>
    private const int MaxOutputLineLength = 300;

    public static bool IsNotebook(string path)
    {
        return string.Equals(Path.GetExtension(path), ".ipynb", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Renders notebook JSON into text lines. Throws on malformed JSON so callers can fall back to raw content.
    /// </summary>
    public static string[] Render(string notebookJson, string displayName)
    {
        using var doc = JsonDocument.Parse(notebookJson);
        var root = doc.RootElement;

        if (!root.TryGetProperty("cells", out var cells) || cells.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("Notebook has no 'cells' array.");
        }

        var language = DetectLanguage(root);
        var codeCells = 0;
        var markdownCells = 0;
        foreach (var cell in cells.EnumerateArray())
        {
            var type = GetString(cell, "cell_type");
            if (type == "code") codeCells++;
            else if (type == "markdown") markdownCells++;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"# Jupyter Notebook: {displayName}");
        sb.AppendLine($"# {cells.GetArrayLength()} cells ({codeCells} code, {markdownCells} markdown), language: {language}. Cells are listed in notebook order.");
        sb.AppendLine("# Cell outputs are summarized: text is truncated, images/HTML are omitted.");
        sb.AppendLine();

        var index = 0;
        foreach (var cell in cells.EnumerateArray())
        {
            index++;
            var type = GetString(cell, "cell_type") ?? "raw";
            var source = ReadTextField(cell, "source");

            if (type == "code")
            {
                var execCount = cell.TryGetProperty("execution_count", out var ec) && ec.ValueKind == JsonValueKind.Number
                    ? $" (execution_count={ec.GetInt32()})"
                    : string.Empty;
                sb.AppendLine($"## Cell {index} [code]{execCount}");
                sb.AppendLine($"```{language}");
                sb.AppendLine(source.TrimEnd());
                sb.AppendLine("```");
                RenderOutputs(cell, sb);
            }
            else
            {
                sb.AppendLine($"## Cell {index} [{type}]");
                sb.AppendLine(source.TrimEnd());
            }

            sb.AppendLine();
        }

        return sb.ToString().Split('\n').Select(l => l.TrimEnd('\r')).ToArray();
    }

    private static void RenderOutputs(JsonElement cell, StringBuilder sb)
    {
        if (!cell.TryGetProperty("outputs", out var outputs) || outputs.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var output in outputs.EnumerateArray())
        {
            var outputType = GetString(output, "output_type");
            switch (outputType)
            {
                case "stream":
                    AppendTruncated(sb, $"### Output (stream/{GetString(output, "name") ?? "stdout"})", ReadTextField(output, "text"));
                    break;

                case "execute_result":
                case "display_data":
                    if (output.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
                    {
                        if (data.TryGetProperty("text/plain", out var plain))
                        {
                            AppendTruncated(sb, $"### Output ({outputType})", ReadText(plain));
                        }
                        else
                        {
                            var mimeTypes = string.Join(", ", data.EnumerateObject().Select(p => p.Name));
                            sb.AppendLine($"### Output ({outputType}): [{mimeTypes} omitted]");
                        }

                        // Note rich mime types that were dropped alongside text/plain
                        var rich = data.EnumerateObject()
                            .Select(p => p.Name)
                            .Where(n => n != "text/plain" && (n.StartsWith("image/") || n == "text/html" || n.StartsWith("application/")))
                            .ToList();
                        if (rich.Count > 0 && data.TryGetProperty("text/plain", out _))
                        {
                            sb.AppendLine($"[rich output omitted: {string.Join(", ", rich)}]");
                        }
                    }
                    break;

                case "error":
                    var ename = GetString(output, "ename") ?? "Error";
                    var evalue = GetString(output, "evalue") ?? string.Empty;
                    sb.AppendLine($"### Output (error): {ename}: {Truncate(evalue, MaxOutputLineLength)}");
                    break;
            }
        }
    }

    private static void AppendTruncated(StringBuilder sb, string header, string text)
    {
        var lines = text.Replace("\r\n", "\n").Split('\n');
        // Drop trailing blank line produced by a terminating newline
        if (lines.Length > 0 && lines[^1].Length == 0)
        {
            lines = lines[..^1];
        }

        if (lines.Length == 0)
        {
            return;
        }

        sb.AppendLine(header);
        foreach (var line in lines.Take(MaxOutputLines))
        {
            sb.AppendLine(Truncate(line, MaxOutputLineLength));
        }

        if (lines.Length > MaxOutputLines)
        {
            sb.AppendLine($"[... {lines.Length - MaxOutputLines} more output lines omitted]");
        }
    }

    private static string DetectLanguage(JsonElement root)
    {
        if (root.TryGetProperty("metadata", out var metadata) && metadata.ValueKind == JsonValueKind.Object)
        {
            if (metadata.TryGetProperty("language_info", out var info) && info.ValueKind == JsonValueKind.Object)
            {
                var name = GetString(info, "name");
                if (!string.IsNullOrWhiteSpace(name)) return name;
            }

            if (metadata.TryGetProperty("kernelspec", out var kernel) && kernel.ValueKind == JsonValueKind.Object)
            {
                var name = GetString(kernel, "language");
                if (!string.IsNullOrWhiteSpace(name)) return name;
            }
        }

        return "python";
    }

    /// <summary>
    /// Reads a notebook text field, which may be a single string or an array of string fragments.
    /// </summary>
    private static string ReadTextField(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) ? ReadText(value) : string.Empty;
    }

    private static string ReadText(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.Array => string.Concat(value.EnumerateArray()
                .Where(v => v.ValueKind == JsonValueKind.String)
                .Select(v => v.GetString())),
            _ => string.Empty
        };
    }

    private static string? GetString(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static string Truncate(string text, int max)
    {
        return text.Length > max ? text[..max] + "..." : text;
    }
}
