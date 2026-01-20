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

  [McpServerTool]
  [Description("Get a code example for basic pipeline behaviors")]
  public static string GetBehaviorExample()
  {
    return """
            // ═══════════════════════════════════════════════════════════════
            // Basic Pipeline Behavior Example
            // ═══════════════════════════════════════════════════════════════
            //
            // Pipeline behaviors wrap all route handlers, executing cross-cutting
            // concerns like logging, timing, validation, etc.
            //
            // INuruBehavior<T> applies to ALL routes (global behavior)
            // INuruBehavior<TFilter> applies only to routes implementing TFilter
            
            using TimeWarp.Nuru;
            
            // Global behavior - applies to ALL routes
            public sealed class LoggingBehavior : INuruBehavior<IRouteContext>
            {
              private readonly ILogger<LoggingBehavior> Logger;
              
              public LoggingBehavior(ILogger<LoggingBehavior> logger)
              {
                Logger = logger;
              }
              
              public async ValueTask<int> HandleAsync(
                IRouteContext context,
                RouteDelegate next,
                CancellationToken ct)
              {
                Logger.LogInformation("Executing route: {Route}", context.MatchedPattern);
                
                var stopwatch = Stopwatch.StartNew();
                try
                {
                  int result = await next(context, ct);
                  Logger.LogInformation("Route completed in {Elapsed}ms with exit code {ExitCode}", 
                    stopwatch.ElapsedMilliseconds, result);
                  return result;
                }
                catch (Exception ex)
                {
                  Logger.LogError(ex, "Route failed after {Elapsed}ms", stopwatch.ElapsedMilliseconds);
                  throw;
                }
              }
            }
            
            // Performance timing behavior
            public sealed class PerformanceBehavior : INuruBehavior<IRouteContext>
            {
              public async ValueTask<int> HandleAsync(
                IRouteContext context,
                RouteDelegate next,
                CancellationToken ct)
              {
                var stopwatch = Stopwatch.StartNew();
                int result = await next(context, ct);
                stopwatch.Stop();
                
                if (stopwatch.ElapsedMilliseconds > 1000)
                {
                  Console.Error.WriteLine($"Warning: Route took {stopwatch.ElapsedMilliseconds}ms");
                }
                
                return result;
              }
            }
            
            // ═══════════════════════════════════════════════════════════════
            // Registration
            // ═══════════════════════════════════════════════════════════════
            
            NuruApp app = NuruApp.CreateBuilder(args)
              .AddBehavior(typeof(LoggingBehavior))       // Runs first (outermost)
              .AddBehavior(typeof(PerformanceBehavior))   // Runs second
              .Map("deploy {env}")
                .WithHandler((string env) => Console.WriteLine($"Deploying to {env}"))
                .AsCommand()
                .Done()
              .Build();
            
            return await app.RunAsync(args);
            
            // Execution order:
            // 1. LoggingBehavior.HandleAsync() - before
            // 2. PerformanceBehavior.HandleAsync() - before
            // 3. Route handler executes
            // 4. PerformanceBehavior.HandleAsync() - after
            // 5. LoggingBehavior.HandleAsync() - after
            """;
  }

  [McpServerTool]
  [Description("Get a code example for filtered pipeline behaviors")]
  public static string GetFilteredBehaviorExample()
  {
    return """
            // ═══════════════════════════════════════════════════════════════
            // Filtered Pipeline Behavior Example
            // ═══════════════════════════════════════════════════════════════
            //
            // Filtered behaviors only apply to routes that opt-in by implementing
            // a marker interface. This enables selective cross-cutting concerns.
            
            using TimeWarp.Nuru;
            
            // ═══════════════════════════════════════════════════════════════
            // Step 1: Define marker interfaces
            // ═══════════════════════════════════════════════════════════════
            
            public interface IRequireAuth { }
            public interface IAuditable { }
            public interface IIdempotent { }
            
            // ═══════════════════════════════════════════════════════════════
            // Step 2: Create filtered behaviors
            // ═══════════════════════════════════════════════════════════════
            
            // Only applies to routes marked with IRequireAuth
            public sealed class AuthBehavior : INuruBehavior<IRequireAuth>
            {
              private readonly IAuthService Auth;
              
              public AuthBehavior(IAuthService auth) => Auth = auth;
              
              public async ValueTask<int> HandleAsync(
                IRequireAuth context,
                RouteDelegate next,
                CancellationToken ct)
              {
                if (!await Auth.IsAuthenticatedAsync(ct))
                {
                  Console.Error.WriteLine("Error: Authentication required");
                  return 1;
                }
                
                return await next((IRouteContext)context, ct);
              }
            }
            
            // Only applies to routes marked with IAuditable
            public sealed class AuditBehavior : INuruBehavior<IAuditable>
            {
              private readonly IAuditLogger AuditLogger;
              
              public AuditBehavior(IAuditLogger auditLogger) => AuditLogger = auditLogger;
              
              public async ValueTask<int> HandleAsync(
                IAuditable context,
                RouteDelegate next,
                CancellationToken ct)
              {
                var routeContext = (IRouteContext)context;
                await AuditLogger.LogAsync($"Executing: {routeContext.MatchedPattern}", ct);
                
                int result = await next(routeContext, ct);
                
                await AuditLogger.LogAsync($"Completed with exit code: {result}", ct);
                return result;
              }
            }
            
            // ═══════════════════════════════════════════════════════════════
            // Step 3: Register behaviors and opt-in routes
            // ═══════════════════════════════════════════════════════════════
            
            NuruApp app = NuruApp.CreateBuilder(args)
              // Register filtered behaviors
              .AddBehavior(typeof(AuthBehavior))   // Only for IRequireAuth routes
              .AddBehavior(typeof(AuditBehavior))  // Only for IAuditable routes
              
              // Public route - no behaviors apply
              .Map("version")
                .WithHandler(() => Console.WriteLine("v1.0.0"))
                .AsQuery()
                .Done()
              
              // Protected route - AuthBehavior applies
              .Map("deploy {env}")
                .WithHandler((string env) => Console.WriteLine($"Deploying to {env}"))
                .Implements<IRequireAuth>()  // Opt-in to auth
                .AsCommand()
                .Done()
              
              // Audited route - both behaviors apply
              .Map("delete-user {userId:guid}")
                .WithHandler((Guid userId) => Console.WriteLine($"Deleting user {userId}"))
                .Implements<IRequireAuth>()  // Opt-in to auth
                .Implements<IAuditable>()    // Opt-in to audit
                .AsCommand()
                .Done()
              
              .Build();
            
            return await app.RunAsync(args);
            
            // For "version": No behaviors run
            // For "deploy": AuthBehavior runs
            // For "delete-user": AuthBehavior then AuditBehavior run
            """;
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
            
            Use the `GetBehaviorExample()` and `GetFilteredBehaviorExample()` tools 
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
