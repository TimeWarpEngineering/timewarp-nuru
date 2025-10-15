namespace TimeWarp.Nuru.Mcp.Tools;

using System.ComponentModel;
using System.Reflection;
using System.Text;

internal sealed class GetSyntaxTool
{
    private const string ResourceName = "TimeWarp.Nuru.Mcp.SyntaxExamples.cs";
    private static string? CachedSyntaxExamples;

    private static readonly Dictionary<string, string> RegionTitles = new()
    {
        ["literals"] = "Literal Segments",
        ["parameters"] = "Parameters",
        ["types"] = "Parameter Types",
        ["optional"] = "Optional Parameters",
        ["catchall"] = "Catch-all Parameters",
        ["options"] = "Options",
        ["descriptions"] = "Descriptions",
        ["complex"] = "Complex Patterns"
    };

    [McpServerTool]
    [Description("Get TimeWarp.Nuru route pattern syntax documentation")]
    public static string GetSyntax(
        [Description("Syntax element (literals, parameters, types, optional, catchall, options, descriptions, all)")] string element = "all")
    {
        if (element.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return GetAllSyntax();
        }

        string normalizedElement = element.ToLowerInvariant()
            .Replace("-", "", StringComparison.Ordinal)
            .Replace("_", "", StringComparison.Ordinal);

        // Try exact match first
        if (RegionTitles.ContainsKey(normalizedElement))
        {
            return ExtractRegion(normalizedElement);
        }

        // Try partial match
        string? matchedKey = RegionTitles.Keys
            .FirstOrDefault(k => k.Contains(normalizedElement, StringComparison.Ordinal) ||
                                normalizedElement.Contains(k, StringComparison.Ordinal));

        if (matchedKey is not null)
        {
            return ExtractRegion(matchedKey);
        }

        return $"Unknown syntax element '{element}'. Available elements:\n" +
               string.Join("\n", RegionTitles.Keys.Select(k => $"- {k}"));
    }

    [McpServerTool]
    [Description("Get examples of specific route pattern features")]
    public static string GetPatternExamples(
        [Description("Pattern feature (basic, typed, optional, catchall, options, complex)")] string feature = "basic")
    {
        string normalizedFeature = feature.ToLowerInvariant();

        // Map feature names to region names
        string? regionName = normalizedFeature switch
        {
            "basic" => "literals",  // Basic examples are in literals
            "typed" => "types",
            _ => RegionTitles.ContainsKey(normalizedFeature) ? normalizedFeature : null
        };

        if (regionName is not null)
        {
            return ExtractRegion(regionName);
        }

        return $"Unknown feature '{feature}'. Available features: basic, typed, optional, catchall, options, complex";
    }

    private static string GetAllSyntax()
    {
        StringBuilder sb = new();
        sb.AppendLine("# Route Pattern Syntax Reference");
        sb.AppendLine();
        sb.AppendLine("## Quick Reference");
        sb.AppendLine();
        sb.AppendLine("- **Literals**: `status`, `git commit`");
        sb.AppendLine("- **Parameters**: `{name}`, `{id:int}`");
        sb.AppendLine("- **Optional**: `{tag?}`, `{count:int?}`");
        sb.AppendLine("- **Catch-all**: `{*args}`");
        sb.AppendLine("- **Options**: `--verbose`, `-v`, `--config {mode}`");
        sb.AppendLine("- **Repeated Options**: `--tag {t}*` (can be used multiple times)");
        sb.AppendLine("- **Descriptions**: `{env|Environment name}`, `--force|Skip confirmations`");
        sb.AppendLine();
        sb.AppendLine("## Examples");
        sb.AppendLine();

        // Extract all regions
        foreach (string key in RegionTitles.Keys)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"### {RegionTitles[key]}");
            sb.AppendLine();
            sb.AppendLine(ExtractRegion(key));
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string LoadSyntaxExamples()
    {
        if (CachedSyntaxExamples is not null)
        {
            return CachedSyntaxExamples;
        }

        Assembly assembly = typeof(GetSyntaxTool).Assembly;
        using Stream? stream = assembly.GetManifestResourceStream(ResourceName);

        if (stream is null)
        {
            return $"Error: Embedded resource '{ResourceName}' not found. Available resources: {string.Join(", ", assembly.GetManifestResourceNames())}";
        }

        using StreamReader reader = new(stream);
        CachedSyntaxExamples = reader.ReadToEnd();
        return CachedSyntaxExamples;
    }

    private static string ExtractRegion(string regionName)
    {
        string content = LoadSyntaxExamples();

        if (content.StartsWith("Error:", StringComparison.Ordinal))
        {
            return content;
        }

        string[] lines = content.Split('\n');
        const string EndRegionMarker = "#endregion";
        string regionMarker = $"#region MCP:{regionName}";

        int startIndex = -1;
        int endIndex = -1;

        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].TrimStart().StartsWith(regionMarker, StringComparison.Ordinal))
            {
                startIndex = i + 1; // Skip the region line itself
            }
            else if (startIndex != -1 && lines[i].TrimStart().StartsWith(EndRegionMarker, StringComparison.Ordinal))
            {
                endIndex = i;
                break;
            }
        }

        if (startIndex == -1)
        {
            return $"Error: Region 'MCP:{regionName}' not found in SyntaxExamples.cs";
        }

        if (endIndex == -1)
        {
            return $"Error: Region 'MCP:{regionName}' not properly closed in SyntaxExamples.cs";
        }

        // Extract the region content
        string[] regionLines = lines[startIndex..endIndex];

        // Format as markdown with title and code block
        StringBuilder sb = new();
        sb.AppendLine(CultureInfo.InvariantCulture, $"## {RegionTitles[regionName]}");
        sb.AppendLine();
        sb.AppendLine("```csharp");

        foreach (string line in regionLines)
        {
            sb.AppendLine(line);
        }

        sb.AppendLine("```");

        return sb.ToString();
    }
}
