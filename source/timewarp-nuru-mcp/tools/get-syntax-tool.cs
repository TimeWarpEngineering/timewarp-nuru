namespace TimeWarp.Nuru.Mcp.Tools;

using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Text;

internal sealed class GetSyntaxTool
{
  private const string ResourceName = "TimeWarp.Nuru.Mcp.SyntaxExamples.cs";
  private static string? CachedSyntaxExamples;

  // Endpoint DSL regions (RECOMMENDED - Priority 1)
  private static readonly Dictionary<string, string> EndpointRegionTitles = new()
  {
    ["endpoint-literals"] = "Endpoint DSL: Literal Segments",
    ["endpoint-parameters"] = "Endpoint DSL: Parameters",
    ["endpoint-types"] = "Endpoint DSL: Parameter Types",
    ["endpoint-optional"] = "Endpoint DSL: Optional Parameters",
    ["endpoint-catchall"] = "Endpoint DSL: Catch-all Parameters",
    ["endpoint-options"] = "Endpoint DSL: Options",
    ["endpoint-descriptions"] = "Endpoint DSL: Descriptions",
    ["endpoint-complex"] = "Endpoint DSL: Complex Patterns"
  };

  // Fluent DSL regions (Alternative - Priority 2)
  private static readonly Dictionary<string, string> FluentRegionTitles = new()
  {
    ["fluent-literals"] = "Fluent DSL: Literal Segments",
    ["fluent-parameters"] = "Fluent DSL: Parameters",
    ["fluent-types"] = "Fluent DSL: Parameter Types",
    ["fluent-optional"] = "Fluent DSL: Optional Parameters",
    ["fluent-catchall"] = "Fluent DSL: Catch-all Parameters",
    ["fluent-options"] = "Fluent DSL: Options",
    ["fluent-descriptions"] = "Fluent DSL: Descriptions",
    ["fluent-complex"] = "Fluent DSL: Complex Patterns"
  };

  // Legacy region mapping for backward compatibility
  private static readonly Dictionary<string, string> LegacyRegionMapping = new()
  {
    ["literals"] = "endpoint-literals",
    ["parameters"] = "endpoint-parameters",
    ["types"] = "endpoint-types",
    ["optional"] = "endpoint-optional",
    ["catchall"] = "endpoint-catchall",
    ["options"] = "endpoint-options",
    ["descriptions"] = "endpoint-descriptions",
    ["complex"] = "endpoint-complex"
  };

  [McpServerTool]
  [Description("Get TimeWarp.Nuru route pattern syntax documentation. Endpoint DSL is recommended for production use.")]
  public static string GetSyntax(
      [Description("Syntax element (literals, parameters, types, optional, catchall, options, descriptions, complex, all) or prefix with 'endpoint-' or 'fluent-' for specific DSL")] string element = "all")
  {
    if (element.Equals("all", StringComparison.OrdinalIgnoreCase))
    {
      return GetAllSyntax();
    }

    string normalizedElement = element.ToLowerInvariant()
        .Replace("-", "", StringComparison.Ordinal)
        .Replace("_", "", StringComparison.Ordinal);

    // Check for DSL-specific prefixes
    bool preferEndpoint = element.StartsWith("endpoint", StringComparison.OrdinalIgnoreCase);
    bool preferFluent = element.StartsWith("fluent", StringComparison.OrdinalIgnoreCase);

    // Try exact match in both dictionaries
    if (EndpointRegionTitles.TryGetValue(normalizedElement, out string? endpointTitle))
    {
      return ExtractRegion(normalizedElement, endpointTitle);
    }

    if (FluentRegionTitles.TryGetValue(normalizedElement, out string? fluentTitle))
    {
      return ExtractRegion(normalizedElement, fluentTitle);
    }

    // Try legacy mapping (maps to endpoint by default)
    if (!preferFluent && LegacyRegionMapping.TryGetValue(normalizedElement, out string? endpointRegion))
    {
      return ExtractRegion(endpointRegion, EndpointRegionTitles[endpointRegion]);
    }

    // Try partial match
    string? matchedEndpointKey = EndpointRegionTitles.Keys
        .FirstOrDefault(k => k.Replace("-", "", StringComparison.Ordinal).Contains(normalizedElement, StringComparison.Ordinal) ||
                            normalizedElement.Contains(k.Replace("-", "", StringComparison.Ordinal), StringComparison.Ordinal));

    string? matchedFluentKey = FluentRegionTitles.Keys
        .FirstOrDefault(k => k.Replace("-", "", StringComparison.Ordinal).Contains(normalizedElement, StringComparison.Ordinal) ||
                            normalizedElement.Contains(k.Replace("-", "", StringComparison.Ordinal), StringComparison.Ordinal));

    if (matchedEndpointKey is not null && (preferEndpoint || matchedFluentKey is null))
    {
      return ExtractRegion(matchedEndpointKey, EndpointRegionTitles[matchedEndpointKey]);
    }

    if (matchedFluentKey is not null)
    {
      return ExtractRegion(matchedFluentKey, FluentRegionTitles[matchedFluentKey]);
    }

    return $"Unknown syntax element '{element}'. Available elements:\n\n" +
           "**Endpoint DSL (Recommended):**\n" +
           string.Join("\n", EndpointRegionTitles.Keys.Select(k => $"- {k}")) + "\n\n" +
           "**Fluent DSL (Alternative):**\n" +
           string.Join("\n", FluentRegionTitles.Keys.Select(k => $"- {k}")) + "\n\n" +
           "**Legacy (maps to Endpoint DSL):**\n" +
           string.Join("\n", LegacyRegionMapping.Keys.Select(k => $"- {k}"));
  }

  [McpServerTool]
  [Description("Get examples of specific route pattern features. Returns Endpoint DSL examples by default.")]
  public static string GetPatternExamples(
      [Description("Pattern feature (basic, typed, optional, catchall, options, complex) or 'fluent-{feature}' for Fluent DSL examples")] string feature = "basic")
  {
    string normalizedFeature = feature.ToLowerInvariant();

    // Check if user explicitly wants Fluent DSL
    bool wantFluent = normalizedFeature.StartsWith("fluent", StringComparison.Ordinal);

    // Map feature names to region names
    string? regionName = normalizedFeature switch
    {
      "fluent-basic" or "basic" => wantFluent ? "fluent-literals" : "endpoint-literals",
      "fluent-typed" or "typed" => wantFluent ? "fluent-types" : "endpoint-types",
      "fluent-optional" or "optional" => wantFluent ? "fluent-optional" : "endpoint-optional",
      "fluent-catchall" or "catchall" => wantFluent ? "fluent-catchall" : "endpoint-catchall",
      "fluent-options" or "options" => wantFluent ? "fluent-options" : "endpoint-options",
      "fluent-complex" or "complex" => wantFluent ? "fluent-complex" : "endpoint-complex",
      "fluent-literals" => "fluent-literals",
      "fluent-parameters" => "fluent-parameters",
      "fluent-descriptions" => "fluent-descriptions",
      _ => null
    };

    if (regionName is not null)
    {
      string title = EndpointRegionTitles.TryGetValue(regionName, out string? endpointTitle)
          ? endpointTitle
          : FluentRegionTitles[regionName];
      return ExtractRegion(regionName, title);
    }

    return $"Unknown feature '{feature}'. Available features:\n\n" +
           "**Endpoint DSL (default):**\n" +
           "basic, typed, optional, catchall, options, complex\n\n" +
           "**Fluent DSL (prefix with 'fluent-'):**\n" +
           "fluent-basic, fluent-typed, fluent-optional, fluent-catchall, fluent-options, fluent-complex\n" +
           "fluent-literals, fluent-parameters, fluent-descriptions";
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
    sb.AppendLine("## DSL Priority");
    sb.AppendLine();
    sb.AppendLine("### 1. Endpoint DSL (RECOMMENDED)");
    sb.AppendLine("Uses classes with `[NuruRoute]` attributes.");
    sb.AppendLine("**Benefits:** Single responsibility, easier to test, scales better, saves context for agents.");
    sb.AppendLine("**Use for:** Production apps, complex scenarios, agent-friendly codebases.");
    sb.AppendLine();
    sb.AppendLine("### 2. Fluent DSL (Alternative)");
    sb.AppendLine("Uses `builder.Map().WithHandler().AsCommand().Done()` pattern.");
    sb.AppendLine("**Benefits:** Minimal API style, quick prototyping.");
    sb.AppendLine("**Use for:** Simple scripts, quick prototypes, migration from minimal APIs.");
    sb.AppendLine();
    sb.AppendLine("## Endpoint DSL Examples (Recommended)");
    sb.AppendLine();

    // Extract all Endpoint DSL regions first
    foreach (string key in EndpointRegionTitles.Keys)
    {
      sb.AppendLine(ExtractRegion(key, EndpointRegionTitles[key]));
      sb.AppendLine();
    }

    sb.AppendLine("## Fluent DSL Examples (Alternative)");
    sb.AppendLine();

    // Extract all Fluent DSL regions second
    foreach (string key in FluentRegionTitles.Keys)
    {
      sb.AppendLine(ExtractRegion(key, FluentRegionTitles[key]));
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

  private static string ExtractRegion(string regionName, string displayTitle)
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
    sb.AppendLine(CultureInfo.InvariantCulture, $"### {displayTitle}");
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
