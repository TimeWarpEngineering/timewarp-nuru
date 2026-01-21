namespace TimeWarp.Nuru.Mcp.Tools;

using TimeWarp.Nuru.Mcp.Services;

/// <summary>
/// MCP tool that provides information about error handling in TimeWarp.Nuru.
/// Requires internet access to fetch documentation from GitHub.
/// </summary>
internal sealed class ErrorHandlingTool
{
  private const string DocPath = "documentation/developer/reference/error-handling.md";
  private const string CacheCategory = "error-handling";

  // Sections in the error handling documentation
  private static readonly Dictionary<string, string> SectionHeaders = new()
  {
    ["overview"] = "# Error Handling in TimeWarp.Nuru",
    ["architecture"] = "## Error Handling Architecture",
    ["philosophy"] = "## Error Handling Philosophy",
    ["parsing"] = "### 2. **Route Parsing Errors**",
    ["binding"] = "### 3. **Parameter Binding Errors**",
    ["conversion"] = "### 4. **Type Conversion Errors**",
    ["execution"] = "### 5. **Handler Execution Errors**",
    ["matching"] = "### 6. **Command Matching Errors**"
  };

  private const string InternetRequiredMessage = "❌ **Internet access required.** Unable to fetch error handling documentation from GitHub. " +
                                                 "Please check your network connection and try again.";

  [McpServerTool]
  [Description("Get information about TimeWarp.Nuru error handling")]
  public static async Task<string> GetErrorHandlingInfoAsync(
      [Description("Specific area (overview, architecture, philosophy)")] string area = "overview",
      [Description("Force refresh from GitHub, bypassing cache")] bool forceRefresh = false)
  {
    string normalizedArea = area.ToLowerInvariant().Replace("-", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal);

    if (!SectionHeaders.ContainsKey(normalizedArea))
    {
      return $"Unknown area '{area}'. Available areas: {string.Join(", ", SectionHeaders.Keys.Where(k => k is "overview" or "architecture" or "philosophy"))}";
    }

    string? docContent = await GitHubCacheService.FetchAsync(DocPath, CacheCategory, forceRefresh);
    if (docContent is null)
    {
      return InternetRequiredMessage;
    }

    string sectionContent = ExtractSection(docContent, normalizedArea);
    if (string.IsNullOrWhiteSpace(sectionContent))
    {
      return $"Could not find content for area '{area}' in the documentation.";
    }

    return sectionContent;
  }

  [McpServerTool]
  [Description("Get information about specific error handling scenarios")]
  public static async Task<string> GetErrorScenariosAsync(
      [Description("Scenario type (parsing, binding, conversion, execution, matching, all)")] string scenario = "all",
      [Description("Force refresh from GitHub, bypassing cache")] bool forceRefresh = false)
  {
    string normalizedScenario = scenario.ToLowerInvariant().Replace("-", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal);

    string? docContent = await GitHubCacheService.FetchAsync(DocPath, CacheCategory, forceRefresh);
    if (docContent is null)
    {
      return InternetRequiredMessage;
    }

    if (normalizedScenario == "all")
    {
      List<string> allScenarios = [];
      foreach (string key in new[] { "parsing", "binding", "conversion", "execution", "matching" })
      {
        string scenarioContent = ExtractSection(docContent, key);
        if (!string.IsNullOrWhiteSpace(scenarioContent))
        {
          allScenarios.Add(scenarioContent);
        }
      }

      return """
                # TimeWarp.Nuru Error Scenarios

                TimeWarp.Nuru handles various error scenarios throughout the command processing pipeline:

                ## Available Scenarios:

                - **parsing**: Route pattern parsing errors
                - **binding**: Parameter binding errors
                - **conversion**: Type conversion errors
                - **execution**: Handler execution errors
                - **matching**: Command matching errors

                Use `GetErrorScenarios("scenario")` to get details about a specific scenario.
                """ + "\n\n" + string.Join("\n\n", allScenarios);
    }

    if (!SectionHeaders.ContainsKey(normalizedScenario))
    {
      return $"Unknown scenario '{scenario}'. Available scenarios: parsing, binding, conversion, execution, matching, all";
    }

    string sectionContent = ExtractSection(docContent, normalizedScenario);
    if (string.IsNullOrWhiteSpace(sectionContent))
    {
      return $"Could not find content for scenario '{scenario}' in the documentation.";
    }

    return sectionContent;
  }

  [McpServerTool]
  [Description("Get best practices for error handling in TimeWarp.Nuru")]
  public static async Task<string> GetErrorHandlingBestPracticesAsync(
      [Description("Force refresh from GitHub, bypassing cache")] bool forceRefresh = false)
  {
    string? docContent = await GitHubCacheService.FetchAsync(DocPath, CacheCategory, forceRefresh);
    if (docContent is null)
    {
      return InternetRequiredMessage;
    }

    string philosophySection = ExtractSection(docContent, "philosophy");
    if (string.IsNullOrWhiteSpace(philosophySection))
    {
      return "Could not find best practices content in the documentation.";
    }

    return """
            # TimeWarp.Nuru Error Handling Best Practices

            The following best practices are derived from TimeWarp.Nuru's error handling philosophy:

            """ + philosophySection.Replace("## Error Handling Philosophy", "", StringComparison.Ordinal).Trim();
  }

  private static string ExtractSection(string content, string sectionKey)
  {
    if (!SectionHeaders.TryGetValue(sectionKey, out string? sectionHeader))
    {
      return string.Empty;
    }

    int startIndex = content.IndexOf(sectionHeader, StringComparison.Ordinal);
    if (startIndex == -1)
    {
      return string.Empty;
    }

    // Find the next section header after this one
    int endIndex = content.Length;
    foreach (string header in SectionHeaders.Values)
    {
      int nextHeaderIndex = content.IndexOf(header, startIndex + sectionHeader.Length, StringComparison.Ordinal);
      if (nextHeaderIndex > startIndex && nextHeaderIndex < endIndex)
      {
        endIndex = nextHeaderIndex;
      }
    }

    string sectionContent = content.Substring(startIndex, endIndex - startIndex).Trim();
    return sectionContent;
  }
}
