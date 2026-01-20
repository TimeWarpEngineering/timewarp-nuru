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

  [McpServerTool]
  [Description("Get a code example for implementing custom type converters")]
  public static string GetTypeConverterExample()
  {
    return """
            // ═══════════════════════════════════════════════════════════════
            // Custom Type Converter Example
            // ═══════════════════════════════════════════════════════════════
            //
            // Type converters transform string arguments from the command line
            // into strongly-typed values. TimeWarp.Nuru includes built-in
            // converters for common types and supports custom converters.
            
            using TimeWarp.Nuru;
            
            // ═══════════════════════════════════════════════════════════════
            // Step 1: Define your custom type
            // ═══════════════════════════════════════════════════════════════
            
            public readonly record struct Environment
            {
              public string Name { get; init; }
              public string Region { get; init; }
              
              public static readonly Environment Dev = new() { Name = "dev", Region = "us-west-2" };
              public static readonly Environment Staging = new() { Name = "staging", Region = "us-east-1" };
              public static readonly Environment Prod = new() { Name = "prod", Region = "us-east-1" };
            }
            
            // ═══════════════════════════════════════════════════════════════
            // Step 2: Implement IRouteTypeConverter
            // ═══════════════════════════════════════════════════════════════
            
            public sealed class EnvironmentConverter : IRouteTypeConverter<Environment>
            {
              public bool TryConvert(string input, out Environment result)
              {
                result = input.ToLowerInvariant() switch
                {
                  "dev" or "development" => Environment.Dev,
                  "staging" or "stage" => Environment.Staging,
                  "prod" or "production" => Environment.Prod,
                  _ => default
                };
                
                return result.Name is not null;
              }
              
              // Optional: Provide completion suggestions
              public IEnumerable<string> GetCompletions() =>
                ["dev", "staging", "prod"];
            }
            
            // ═══════════════════════════════════════════════════════════════
            // Step 3: Register the converter
            // ═══════════════════════════════════════════════════════════════
            
            NuruApp app = NuruApp.CreateBuilder(args)
              .AddTypeConverter<Environment, EnvironmentConverter>()
              .Map("deploy {env:Environment}")
                .WithHandler((Environment env) =>
                {
                  Console.WriteLine($"Deploying to {env.Name} in {env.Region}");
                })
                .AsCommand()
                .Done()
              .Build();
            
            return await app.RunAsync(args);
            
            // Usage:
            //   myapp deploy dev        -> "Deploying to dev in us-west-2"
            //   myapp deploy production -> "Deploying to prod in us-east-1"
            //   myapp deploy invalid    -> Error: Cannot convert 'invalid' to Environment
            
            // ═══════════════════════════════════════════════════════════════
            // Advanced: Converter with Validation Messages
            // ═══════════════════════════════════════════════════════════════
            
            public sealed class SemVerConverter : IRouteTypeConverter<Version>
            {
              public bool TryConvert(string input, out Version result)
              {
                // Try parsing as semantic version (X.Y.Z)
                if (Version.TryParse(input, out Version? version))
                {
                  result = version;
                  return true;
                }
                
                // Try parsing as shorthand (X.Y)
                if (input.Count(c => c == '.') == 1 && Version.TryParse($"{input}.0", out version))
                {
                  result = version;
                  return true;
                }
                
                result = default!;
                return false;
              }
              
              public string GetValidationError(string input) =>
                $"'{input}' is not a valid version. Use format: X.Y or X.Y.Z";
            }
            
            // ═══════════════════════════════════════════════════════════════
            // Built-in Type Converters
            // ═══════════════════════════════════════════════════════════════
            //
            // TimeWarp.Nuru includes converters for:
            //   - Primitives: int, long, float, double, decimal, bool
            //   - Common types: string, Guid, DateTime, DateTimeOffset, TimeSpan
            //   - Enums: Any enum type (case-insensitive)
            //   - Uri, FileInfo, DirectoryInfo
            //
            // Use the type constraint syntax:
            //   {param:int}      -> int
            //   {param:guid}     -> Guid
            //   {param:datetime} -> DateTime
            //   {param:MyEnum}   -> Custom enum type
            """;
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
            
            Use the `GetTypeConverterExample()` tool for a complete code sample.
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
