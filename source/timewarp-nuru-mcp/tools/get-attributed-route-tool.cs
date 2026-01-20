namespace TimeWarp.Nuru.Mcp.Tools;

using System.ComponentModel;

/// <summary>
/// MCP tool that provides information about attributed routes in TimeWarp.Nuru.
/// </summary>
internal sealed class GetAttributedRouteTool
{
  private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(10) };
  private static readonly Dictionary<string, CachedContent> MemoryCache = [];
  private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);
  private const string AttributedRouteDocPath = "documentation/developer/reference/attributed-routes.md";
  private const string GitHubRawBaseUrl = "https://raw.githubusercontent.com/TimeWarpEngineering/timewarp-nuru/master/";

  private static string CacheDirectory => Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
      "TimeWarp.Nuru.Mcp",
      "cache",
      "attributed-routes"
  );

  [McpServerTool]
  [Description("Get information about TimeWarp.Nuru attributed routes ([NuruRoute])")]
  public static async Task<string> GetAttributedRouteInfoAsync(
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

    return GetAttributedRouteOverviewFallback();
  }

  private static async Task<string?> GetDocumentationContentAsync(bool forceRefresh)
  {
    // Check memory cache first (unless force refresh)
    if (!forceRefresh && MemoryCache.TryGetValue(AttributedRouteDocPath, out CachedContent? cached) && cached.IsValid)
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
        MemoryCache[AttributedRouteDocPath] = new CachedContent(diskCached, DateTime.UtcNow);
        return diskCached;
      }
    }

    // Fetch from GitHub
    try
    {
      string content = await FetchFromGitHubAsync();
      // Update caches
      MemoryCache[AttributedRouteDocPath] = new CachedContent(content, DateTime.UtcNow);
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
    Uri url = new($"{GitHubRawBaseUrl}{AttributedRouteDocPath}");
    HttpResponseMessage response = await HttpClient.GetAsync(url);
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadAsStringAsync();
  }

  private static async Task<string?> ReadFromDiskCacheAsync()
  {
    try
    {
      string cacheFile = Path.Combine(CacheDirectory, "attributed-routes.md");
      if (!File.Exists(cacheFile))
        return null;

      string metaFile = Path.Combine(CacheDirectory, "attributed-routes.meta");
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

      string cacheFile = Path.Combine(CacheDirectory, "attributed-routes.md");
      string metaFile = Path.Combine(CacheDirectory, "attributed-routes.meta");

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

  private static string GetAttributedRouteOverviewFallback()
  {
    return """
            # Attributed Routes in TimeWarp.Nuru
            
            The `[NuruRoute]` attribute enables auto-discovery of routes through source 
            generation. This pattern is ideal for larger applications where you want 
            clean separation between command definitions and their handlers.
            
            ## Basic Structure
            
            ```csharp
            [NuruRoute("pattern {param}", Description = "...")]
            public sealed class MyEndpoint
            {
              // Route parameters become properties
              public required string Param { get; init; }
              
              // Nested Handler class with DI support
              internal sealed class Handler(IMyService service)
              {
                public ValueTask<Unit> Handle(MyEndpoint request, CancellationToken ct)
                {
                  // Implementation
                  return ValueTask.FromResult(Unit.Value);
                }
              }
            }
            ```
            
            ## Key Features
            
            ### Auto-Discovery
            Routes are discovered at compile time via source generation. Register with:

            ```csharp
            NuruApp.CreateBuilder(args)
              .DiscoverEndpoints()
              .Build();
            ```
            
            ### Dependency Injection
            The Handler class supports constructor injection:
            
            ```csharp
            internal sealed class Handler(ILogger logger, IDatabase db, ITerminal terminal)
            ```
            
            ### Filtered Behaviors
            Implement marker interfaces to opt-in to behaviors:
            
            ```csharp
            [NuruRoute("admin delete {id}")]
            public sealed class DeleteEndpoint : IRequireAuth, IAuditable
            ```
            
            ### Return Types
            
            | Return Type | Exit Code |
            |-------------|-----------|
            | `ValueTask<Unit>` | 0 (success) |
            | `ValueTask<int>` | Custom exit code |
            | `ValueTask<T>` | 0, result serialized |
            | `void` | 0 (success) |
            | `int` | Custom exit code |
            
            Use the `get_example("attributed-routes")` tool for complete code samples.
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
