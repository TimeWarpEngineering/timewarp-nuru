namespace TimeWarp.Nuru.Mcp.Tools;

/// <summary>
/// MCP tool that provides information about error handling in TimeWarp.Nuru.
/// </summary>
internal sealed class ErrorHandlingTool
{
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(10) };
    private static readonly Dictionary<string, CachedContent> MemoryCache = [];
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);
    private const string ErrorHandlingDocPath = "Documentation/Developer/Reference/ErrorHandling.md";
    private const string GitHubRawBaseUrl = "https://raw.githubusercontent.com/TimeWarpEngineering/timewarp-nuru/master/";

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

    // Fallback content in case GitHub fetch fails
    private static readonly Dictionary<string, string> FallbackContent = new()
    {
        ["overview"] = """
            # Error Handling in TimeWarp.Nuru

            TimeWarp.Nuru implements a multi-layered approach to error handling that prioritizes clear error messages and graceful failure while maintaining simplicity.

            ## Key Error Handling Mechanisms

            1. **Top-Level Exception Handling** - Catches all unhandled exceptions
            2. **Route Parsing Errors** - Comprehensive parsing error handling
            3. **Parameter Binding Errors** - Validates parameters and handles conversion failures
            4. **Type Conversion Errors** - Non-throwing approach with boolean success indicators
            5. **Handler Execution Errors** - Separate handling for delegate vs Mediator commands
            6. **Command Matching Errors** - Shows help when no route matches
            7. **Output Stream Separation** - Keeps errors separate from normal output
            """
    };

    private static string CacheDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TimeWarp.Nuru.Mcp",
        "cache",
        "error-handling"
    );

    [McpServerTool]
    [Description("Get information about TimeWarp.Nuru error handling")]
    public static async Task<string> GetErrorHandlingInfoAsync(
        [Description("Specific area (overview, architecture, philosophy)")] string area = "overview",
        [Description("Force refresh from GitHub, bypassing cache")] bool forceRefresh = false)
    {
        string normalizedArea = area.ToLowerInvariant().Replace("-", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal);
        // Check if the area is valid
        if (!SectionHeaders.ContainsKey(normalizedArea))
        {
            return $"Unknown area '{area}'. Available areas: {string.Join(", ", SectionHeaders.Keys.Where(k => k is "overview" or "architecture" or "philosophy"))}";
        }

        try
        {
            // Get the full documentation content
            string docContent = await GetDocumentationContentAsync(forceRefresh);
            // Extract the requested section
            string sectionContent = ExtractSection(docContent, normalizedArea);
            if (string.IsNullOrWhiteSpace(sectionContent))
            {
                return FallbackContent.TryGetValue(normalizedArea, out string? fallback)
                    ? fallback
                    : $"Could not find content for area '{area}' in the documentation.";
            }

            return sectionContent;
        }
        catch (HttpRequestException ex)
        {
            // Return fallback content if available
            return FallbackContent.TryGetValue(normalizedArea, out string? fallback)
                ? $"{fallback}\n\n(Note: Using fallback content due to error: {ex.Message})"
                : $"Error retrieving error handling information: {ex.Message}";
        }
    }

    [McpServerTool]
    [Description("Get information about specific error handling scenarios")]
    public static async Task<string> GetErrorScenariosAsync(
        [Description("Scenario type (parsing, binding, conversion, execution, matching, all)")] string scenario = "all",
        [Description("Force refresh from GitHub, bypassing cache")] bool forceRefresh = false)
    {
        string normalizedScenario = scenario.ToLowerInvariant().Replace("-", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal);
        try
        {
            // Get the full documentation content
            string docContent = await GetDocumentationContentAsync(forceRefresh);
            if (normalizedScenario == "all")
            {
                // Combine all error scenarios
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

            // Check if the scenario is valid
            if (!SectionHeaders.ContainsKey(normalizedScenario))
            {
                return $"Unknown scenario '{scenario}'. Available scenarios: parsing, binding, conversion, execution, matching, all";
            }
            // Extract the requested section
            string sectionContent = ExtractSection(docContent, normalizedScenario);
            if (string.IsNullOrWhiteSpace(sectionContent))
            {
                return $"Could not find content for scenario '{scenario}' in the documentation.";
            }

            return sectionContent;
        }
        catch (HttpRequestException ex)
        {
            return $"Error retrieving error scenario information: {ex.Message}";
        }
    }

    [McpServerTool]
    [Description("Get best practices for error handling in TimeWarp.Nuru")]
    public static async Task<string> GetErrorHandlingBestPracticesAsync(
        [Description("Force refresh from GitHub, bypassing cache")] bool forceRefresh = false)
    {
        try
        {
            // Get the full documentation content
            string docContent = await GetDocumentationContentAsync(forceRefresh);
            // Extract the best practices section
            // Note: This is a bit of a hack since there's no explicit "Best Practices" section in the doc
            // We'll extract content from the "Error Handling Philosophy" section
            string philosophySection = ExtractSection(docContent, "philosophy");
            if (string.IsNullOrWhiteSpace(philosophySection))
            {
                return """
                    # TimeWarp.Nuru Error Handling Best Practices

                    1. **Use Top-Level Try-Catch Blocks** - Catch all unhandled exceptions
                    2. **Validate Command Input Early** - Check for invalid input as early as possible
                    3. **Separate Output Streams** - Normal output to stdout, errors to stderr
                    4. **Return Appropriate Exit Codes** - 0 for success, 1 for errors
                    5. **Provide Helpful Error Messages** - Include context, parameters, and suggested fixes
                    6. **Handle Async Properly** - Use async/await for I/O operations, ConfigureAwait(false) in libraries

                    ## Example Pattern

                    ```csharp
                    .AddRoute("deploy {environment}", async (string environment) =>
                    {
                        try
                        {
                            // Validate early
                            if (!IsValidEnvironment(environment))
                            {
                                await Console.Error.WriteLineAsync($"Invalid environment: {environment}");
                                await Console.Error.WriteLineAsync("Valid environments: dev, staging, prod");
                                return 1;
                            }

                            // Execute
                            await DeployAsync(environment);
                            await Console.Out.WriteLineAsync($"Successfully deployed to {environment}");
                            return 0;
                        }
                        catch (Exception ex)
                        {
                            await Console.Error.WriteLineAsync($"Deployment failed: {ex.Message}");
                            return 1;
                        }
                    });
                    ```
                    """;
            }
            // Add a best practices header
            return """
                # TimeWarp.Nuru Error Handling Best Practices

                The following best practices are derived from TimeWarp.Nuru's error handling philosophy:

                """ + philosophySection.Replace("## Error Handling Philosophy", "", StringComparison.Ordinal).Trim();
        }
        catch (HttpRequestException ex)
        {
            return $"Error retrieving error handling best practices: {ex.Message}";
        }
    }

    private static async Task<string> GetDocumentationContentAsync(bool forceRefresh)
    {
        // Check memory cache first (unless force refresh)
        if (!forceRefresh && MemoryCache.TryGetValue(ErrorHandlingDocPath, out CachedContent? cached) && cached.IsValid)
        {
            return cached.Content;
        }

        // Check disk cache (unless force refresh)
        if (!forceRefresh)
        {
            string? diskCached = await ReadFromDiskCacheAsync();
            if (diskCached is not null)
            {
                // Update memory cache
                MemoryCache[ErrorHandlingDocPath] = new CachedContent(diskCached, DateTime.UtcNow);
                return diskCached;
            }
        }

        // Fetch from GitHub
        try
        {
            string content = await FetchFromGitHubAsync();
            // Update caches
            MemoryCache[ErrorHandlingDocPath] = new CachedContent(content, DateTime.UtcNow);
            await WriteToDiskCacheAsync(content);
            return content;
        }
        catch (HttpRequestException ex)
        {
            // Try disk cache as fallback
            string? fallback = await ReadFromDiskCacheAsync();
            if (fallback is not null)
            {
                return fallback;
            }

            throw new HttpRequestException($"Failed to fetch documentation from GitHub: {ex.Message}", ex);
        }
    }

    private static async Task<string> FetchFromGitHubAsync()
    {
        Uri url = new($"{GitHubRawBaseUrl}{ErrorHandlingDocPath}");
        HttpResponseMessage response = await HttpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }

    private static async Task<string?> ReadFromDiskCacheAsync()
    {
        try
        {
            string cacheFile = Path.Combine(CacheDirectory, "error-handling.md");
            if (!File.Exists(cacheFile))
                return null;

            string metaFile = Path.Combine(CacheDirectory, "error-handling.meta");
            if (!File.Exists(metaFile))
                return null;

            // Check TTL
            string metaContent = await File.ReadAllTextAsync(metaFile);
            if (DateTime.TryParse(metaContent, out DateTime cachedTime))
            {
                if (DateTime.UtcNow - cachedTime < CacheTtl)
                {
                    return await File.ReadAllTextAsync(cacheFile);
                }
            }
        }
        catch (IOException)
        {
            // Ignore cache read errors
        }
        catch (UnauthorizedAccessException)
        {
            // Ignore permission errors
        }

        return null;
    }

    private static async Task WriteToDiskCacheAsync(string content)
    {
        try
        {
            Directory.CreateDirectory(CacheDirectory);

            string cacheFile = Path.Combine(CacheDirectory, "error-handling.md");
            string metaFile = Path.Combine(CacheDirectory, "error-handling.meta");

            await File.WriteAllTextAsync(cacheFile, content);
            await File.WriteAllTextAsync(metaFile, DateTime.UtcNow.ToString("O"));
        }
        catch (IOException)
        {
            // Ignore cache write errors
        }
        catch (UnauthorizedAccessException)
        {
            // Ignore permission errors
        }
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

        // Extract the section content
        string sectionContent = content.Substring(startIndex, endIndex - startIndex).Trim();
        return sectionContent;
    }

    private sealed class CachedContent
    {
        public string Content { get; }
        public DateTime CachedAt { get; }
        public bool IsValid => DateTime.UtcNow - CachedAt < CacheTtl;

        public CachedContent(string content, DateTime cachedAt)
        {
            Content = content;
            CachedAt = cachedAt;
        }
    }
}