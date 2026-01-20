namespace TimeWarp.Nuru.Mcp.Tools;

using System.ComponentModel;

/// <summary>
/// MCP tool that provides information about custom type converters in TimeWarp.Nuru.
/// </summary>
internal sealed class GetTypeConverterTool
{
  private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(10) };
  private static readonly Dictionary<string, CachedContent> MemoryCache = [];
  private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);
  private const string TypeConverterDocPath = "documentation/developer/reference/type-converters.md";
  private const string GitHubRawBaseUrl = "https://raw.githubusercontent.com/TimeWarpEngineering/timewarp-nuru/master/";

  private static string CacheDirectory => Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
      "TimeWarp.Nuru.Mcp",
      "cache",
      "type-converters"
  );

  [McpServerTool]
  [Description("Get information about TimeWarp.Nuru custom type converters")]
  public static async Task<string> GetTypeConverterInfoAsync(
      [Description("Force refresh from GitHub, bypassing cache")] bool forceRefresh = false)
  {
    try
    {
      string? docContent = await GetDocumentationContentAsync(forceRefresh);
      if (!string.IsNullOrWhiteSpace(docContent))
      {
        return docContent;
      }
    }
    catch (HttpRequestException)
    {
      // Fall through to fallback
    }

    return GetTypeConverterOverviewFallback();
  }

  private static async Task<string?> GetDocumentationContentAsync(bool forceRefresh)
  {
    // Check memory cache first (unless force refresh)
    if (!forceRefresh && MemoryCache.TryGetValue(TypeConverterDocPath, out CachedContent? cached) && cached.IsValid)
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
        MemoryCache[TypeConverterDocPath] = new CachedContent(diskCached, DateTime.UtcNow);
        return diskCached;
      }
    }

    // Fetch from GitHub
    try
    {
      string content = await FetchFromGitHubAsync();
      // Update caches
      MemoryCache[TypeConverterDocPath] = new CachedContent(content, DateTime.UtcNow);
      await WriteToDiskCacheAsync(content);
      return content;
    }
    catch (HttpRequestException)
    {
      // Try disk cache as fallback
      string? fallback = await ReadFromDiskCacheAsync();
      if (fallback is not null)
      {
        return fallback;
      }

      return null;
    }
  }

  private static async Task<string> FetchFromGitHubAsync()
  {
    Uri url = new($"{GitHubRawBaseUrl}{TypeConverterDocPath}");
    HttpResponseMessage response = await HttpClient.GetAsync(url);
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadAsStringAsync();
  }

  private static async Task<string?> ReadFromDiskCacheAsync()
  {
    try
    {
      string cacheFile = Path.Combine(CacheDirectory, "type-converters.md");
      if (!File.Exists(cacheFile))
        return null;

      string metaFile = Path.Combine(CacheDirectory, "type-converters.meta");
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

      string cacheFile = Path.Combine(CacheDirectory, "type-converters.md");
      string metaFile = Path.Combine(CacheDirectory, "type-converters.meta");

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

  private static string GetTypeConverterOverviewFallback()
  {
    return """
            # Custom Type Converters in TimeWarp.Nuru
            
            Type converters transform command-line string arguments into strongly-typed 
            values for your route handlers. TimeWarp.Nuru provides built-in converters 
            for common types and supports custom converters for domain-specific types.
            
            ## Built-in Type Converters
            
            TimeWarp.Nuru includes converters for:
            
            | Type | Constraint | Example |
            |------|------------|---------|
            | `int` | `:int` | `{count:int}` |
            | `long` | `:long` | `{id:long}` |
            | `double` | `:double` | `{rate:double}` |
            | `bool` | `:bool` | `{enabled:bool}` |
            | `Guid` | `:guid` | `{userId:guid}` |
            | `DateTime` | `:datetime` | `{date:datetime}` |
            | Enums | `:EnumName` | `{level:LogLevel}` |
            
            ## Implementing Custom Converters
            
            1. Implement `IRouteTypeConverter<T>`:
            
            ```csharp
            public sealed class MyTypeConverter : IRouteTypeConverter<MyType>
            {
              public bool TryConvert(string input, out MyType result)
              {
                // Parse input and set result
                // Return true if successful, false otherwise
              }
            }
            ```
            
            2. Register with the app builder:
            
            ```csharp
            NuruApp.CreateBuilder(args)
              .AddTypeConverter<MyType, MyTypeConverter>()
              // ...
            ```
            
            3. Use in route patterns:
            
            ```csharp
            .Map("command {param:MyType}")
              .WithHandler((MyType param) => ...)
            ```
            
            Use `get_example("custom-type-converter")` for a complete code sample.
            """;
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
