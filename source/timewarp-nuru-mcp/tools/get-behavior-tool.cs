namespace TimeWarp.Nuru.Mcp.Tools;

using System.ComponentModel;

/// <summary>
/// MCP tool that provides information about pipeline behaviors in TimeWarp.Nuru.
/// </summary>
internal sealed class GetBehaviorTool
{
  private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(10) };
  private static readonly Dictionary<string, CachedContent> MemoryCache = [];
  private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);
  private const string BehaviorDocPath = "documentation/developer/reference/pipeline-behaviors.md";
  private const string GitHubRawBaseUrl = "https://raw.githubusercontent.com/TimeWarpEngineering/timewarp-nuru/master/";

  private static string CacheDirectory => Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
      "TimeWarp.Nuru.Mcp",
      "cache",
      "behaviors"
  );

  [McpServerTool]
  [Description("Get information about TimeWarp.Nuru pipeline behaviors")]
  public static async Task<string> GetBehaviorInfoAsync(
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

    return GetBehaviorOverviewFallback();
  }

  private static async Task<string?> GetDocumentationContentAsync(bool forceRefresh)
  {
    // Check memory cache first (unless force refresh)
    if (!forceRefresh && MemoryCache.TryGetValue(BehaviorDocPath, out CachedContent? cached) && cached.IsValid)
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
        MemoryCache[BehaviorDocPath] = new CachedContent(diskCached, DateTime.UtcNow);
        return diskCached;
      }
    }

    // Fetch from GitHub
    try
    {
      string content = await FetchFromGitHubAsync();
      // Update caches
      MemoryCache[BehaviorDocPath] = new CachedContent(content, DateTime.UtcNow);
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
    Uri url = new($"{GitHubRawBaseUrl}{BehaviorDocPath}");
    HttpResponseMessage response = await HttpClient.GetAsync(url);
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadAsStringAsync();
  }

  private static async Task<string?> ReadFromDiskCacheAsync()
  {
    try
    {
      string cacheFile = Path.Combine(CacheDirectory, "behaviors.md");
      if (!File.Exists(cacheFile))
        return null;

      string metaFile = Path.Combine(CacheDirectory, "behaviors.meta");
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

      string cacheFile = Path.Combine(CacheDirectory, "behaviors.md");
      string metaFile = Path.Combine(CacheDirectory, "behaviors.meta");

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

  private static string GetBehaviorOverviewFallback()
  {
    return """
            # Pipeline Behaviors in TimeWarp.Nuru
            
            Pipeline behaviors provide a middleware-like pattern for adding cross-cutting 
            concerns to your CLI routes. They wrap route handlers and can execute code 
            before and after the handler runs.
            
            ## Behavior Types
            
            ### Global Behaviors: `INuruBehavior<IRouteContext>`
            
            Apply to ALL routes in the application. Use for:
            - Logging
            - Performance monitoring
            - Exception handling
            - Telemetry
            
            ### Filtered Behaviors: `INuruBehavior<TFilter>`
            
            Apply only to routes that implement the marker interface `TFilter`. Use for:
            - Authentication/Authorization
            - Auditing
            - Validation
            - Rate limiting
            
            ## Registration
            
            ```csharp
            NuruApp app = NuruApp.CreateBuilder(args)
              .AddBehavior(typeof(LoggingBehavior))      // Global
              .AddBehavior(typeof(AuthBehavior))         // Filtered: IRequireAuth
              .Map("deploy {env}")
                .WithHandler(...)
                .Implements<IRequireAuth>()  // Opt-in to AuthBehavior
                .AsCommand()
                .Done()
              .Build();
            ```
            
            ## Execution Order
            
            Behaviors execute in registration order (first registered = outermost):
            
            1. First behavior's pre-handler code
            2. Second behavior's pre-handler code
            3. Route handler
            4. Second behavior's post-handler code
            5. First behavior's post-handler code
            
            Use `get_example("behaviors-basic")` and `get_example("behaviors-filtered")`
            for complete code samples.
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
