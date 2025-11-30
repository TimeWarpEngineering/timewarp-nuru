namespace TimeWarp.Nuru.Mcp.Tools;

/// <summary>
/// MCP tool that provides TimeWarp.Nuru code examples from GitHub with caching.
/// </summary>
internal sealed class GetExampleTool
{
  private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(10) };
  private static readonly Dictionary<string, CachedExample> MemoryCache = [];
  private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);
  private static readonly TimeSpan ManifestCacheTtl = TimeSpan.FromHours(24);
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
  {
    TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
  };

  private static Dictionary<string, ExampleInfo>? DynamicExamples;
  private static DateTime ManifestLastFetched = DateTime.MinValue;

  private static string CacheDirectory => Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
      "TimeWarp.Nuru.Mcp",
      "cache",
      "examples"
  );

  [McpServerTool]
  [Description("Get TimeWarp.Nuru code examples from the repository")]
  public static async Task<string> GetExampleAsync(
      [Description("Example name or 'list' to see all available examples")] string name = "list",
      [Description("Force refresh from GitHub, bypassing cache")] bool forceRefresh = false)
  {
    // Get examples (dynamic or fallback)
    Dictionary<string, ExampleInfo> examples = await GetExamplesAsync(forceRefresh);

    if (string.Equals(name, "list", StringComparison.OrdinalIgnoreCase))
    {
      return "Available examples:\n" + string.Join("\n", examples.Select(kvp => $"- {kvp.Key}: {kvp.Value.Description}"));
    }

    if (!examples.TryGetValue(name.ToLowerInvariant(), out ExampleInfo? example))
    {
      return $"Example '{name}' not found. Available examples: {string.Join(", ", examples.Keys)}";
    }

    // Check memory cache first (unless force refresh)
    if (!forceRefresh && MemoryCache.TryGetValue(name, out CachedExample? cached) && cached.IsValid)
    {
      return cached.Content;
    }

    // Check disk cache (unless force refresh)
    if (!forceRefresh)
    {
      string? diskCached = await ReadFromDiskCacheAsync(name);
      if (diskCached is not null)
      {
        // Update memory cache
        MemoryCache[name] = new CachedExample(diskCached, DateTime.UtcNow);
        return diskCached;
      }
    }

    // Fetch from GitHub
    try
    {
      string content = await FetchFromGitHubAsync(example.Path);
      string result = FormatExample(Path.GetFileName(example.Path), example.Description, example.Path, content);

      // Update caches
      MemoryCache[name] = new CachedExample(result, DateTime.UtcNow);
      await WriteToDiskCacheAsync(name, result);

      return result;
    }
    catch (HttpRequestException ex)
    {
      // Try disk cache as fallback
      string? fallback = await ReadFromDiskCacheAsync(name);
      if (fallback is not null)
      {
        return $"{fallback}\n\n// Note: Fetched from cache (GitHub unavailable: {ex.Message})";
      }

      return $"Error fetching example from GitHub: {ex.Message}. No cached version available.";
    }
    catch (TaskCanceledException ex)
    {
      return $"Error getting example: {ex.Message}";
    }
  }

  [McpServerTool]
  [Description("List all available TimeWarp.Nuru examples")]
  public static async Task<string> ListExamplesAsync()
  {
    Dictionary<string, ExampleInfo> examples = await GetExamplesAsync(false);
    return "Available TimeWarp.Nuru examples:\n\n" +
           string.Join("\n", examples.Select(kvp =>
               $"**{kvp.Key}**\n  {kvp.Value.Description}\n  File: {kvp.Value.Path}\n"));
  }

  private static async Task<string> FetchFromGitHubAsync(string path)
  {
    Uri url = new($"https://raw.githubusercontent.com/TimeWarpEngineering/timewarp-nuru/master/{path}");
    HttpResponseMessage response = await HttpClient.GetAsync(url);
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadAsStringAsync();
  }

  private static async Task<string?> ReadFromDiskCacheAsync(string name)
  {
    try
    {
      string cacheFile = Path.Combine(CacheDirectory, $"{name}.cache");
      if (!File.Exists(cacheFile))
        return null;

      string metaFile = Path.Combine(CacheDirectory, $"{name}.meta");
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

  private static async Task WriteToDiskCacheAsync(string name, string content)
  {
    try
    {
      Directory.CreateDirectory(CacheDirectory);

      string cacheFile = Path.Combine(CacheDirectory, $"{name}.cache");
      string metaFile = Path.Combine(CacheDirectory, $"{name}.meta");

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

  private static string FormatExample(string fileName, string description, string path, string content)
  {
    return $"// {fileName}\n// {description}\n// Path: {path}\n\n{content}";
  }

  private static async Task<Dictionary<string, ExampleInfo>> GetExamplesAsync(bool forceRefresh)
  {
    // Check if we need to refresh the manifest
    if (!forceRefresh &&
        DynamicExamples is not null &&
        DateTime.UtcNow - ManifestLastFetched < ManifestCacheTtl)
    {
      return DynamicExamples;
    }

    try
    {
      // Fetch manifest from GitHub
      Uri manifestUrl = new("https://raw.githubusercontent.com/TimeWarpEngineering/timewarp-nuru/master/samples/examples.json");
      HttpResponseMessage response = await HttpClient.GetAsync(manifestUrl);

      if (response.IsSuccessStatusCode)
      {
        string json = await response.Content.ReadAsStringAsync();
        ExampleManifest? manifest = JsonSerializer.Deserialize<ExampleManifest>(json, JsonOptions);

        if (manifest?.Examples is not null)
        {
          DynamicExamples = manifest.Examples.ToDictionary(
              e => e.Id,
              e => new ExampleInfo(e.Path, e.Description)
          );
          ManifestLastFetched = DateTime.UtcNow;
          return DynamicExamples;
        }
      }
    }
    catch (HttpRequestException ex)
    {
      throw new InvalidOperationException($"Failed to fetch examples manifest from GitHub: {ex.Message}", ex);
    }
    catch (TaskCanceledException ex)
    {
      throw new InvalidOperationException($"Request timed out fetching examples manifest: {ex.Message}", ex);
    }
    catch (JsonException ex)
    {
      throw new InvalidOperationException($"Failed to parse examples manifest JSON: {ex.Message}", ex);
    }

    throw new InvalidOperationException("Examples manifest was empty or invalid");
  }

  private sealed record ExampleInfo(string Path, string Description);

  private sealed class ExampleManifest
  {
    public string? Version { get; set; }
    public string? Description { get; set; }
    public List<ExampleEntry>? Examples { get; set; }
  }

  private sealed class ExampleEntry
  {
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Path { get; set; } = "";
    public List<string>? Tags { get; set; }
    public string? Difficulty { get; set; }
  }

  private sealed class CachedExample
  {
    public string Content { get; }
    public DateTime CachedAt { get; }
    public bool IsValid => DateTime.UtcNow - CachedAt < CacheTtl;

    public CachedExample(string content, DateTime cachedAt)
    {
      Content = content;
      CachedAt = cachedAt;
    }
  }
}
