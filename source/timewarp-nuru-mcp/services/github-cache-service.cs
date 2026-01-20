namespace TimeWarp.Nuru.Mcp.Services;

/// <summary>
/// Shared service for fetching content from GitHub with multi-tier caching.
/// </summary>
internal static class GitHubCacheService
{
  private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(10) };
  private static readonly Dictionary<string, CachedContent> MemoryCache = [];
  private static readonly TimeSpan DefaultCacheTtl = TimeSpan.FromHours(1);
  private const string GitHubRawBaseUrl = "https://raw.githubusercontent.com/TimeWarpEngineering/timewarp-nuru/master/";

  private static string BaseCacheDirectory => Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
      "TimeWarp.Nuru.Mcp",
      "cache"
  );

  /// <summary>
  /// Fetches content from GitHub with memory and disk caching.
  /// </summary>
  /// <param name="relativePath">Path relative to repo root (e.g., "documentation/reference/foo.md")</param>
  /// <param name="cacheCategory">Cache subdirectory name (e.g., "examples", "behaviors")</param>
  /// <param name="forceRefresh">Bypass cache and fetch fresh content</param>
  /// <param name="cacheTtl">Optional custom TTL (defaults to 1 hour)</param>
  /// <returns>The content, or null if fetch failed and no cache available</returns>
  public static async Task<string?> FetchAsync(
      string relativePath,
      string cacheCategory,
      bool forceRefresh = false,
      TimeSpan? cacheTtl = null)
  {
    TimeSpan ttl = cacheTtl ?? DefaultCacheTtl;
    string cacheKey = $"{cacheCategory}:{relativePath}";
    string cacheDir = Path.Combine(BaseCacheDirectory, cacheCategory);
    string safeName = GetSafeCacheFileName(relativePath);

    // Check memory cache first (unless force refresh)
    if (!forceRefresh && MemoryCache.TryGetValue(cacheKey, out CachedContent? cached) && cached.IsValid(ttl))
    {
      return cached.Content;
    }

    // Check disk cache (unless force refresh)
    if (!forceRefresh)
    {
      string? diskCached = await ReadFromDiskCacheAsync(cacheDir, safeName, ttl);
      if (diskCached is not null)
      {
        MemoryCache[cacheKey] = new CachedContent(diskCached, DateTime.UtcNow);
        return diskCached;
      }
    }

    // Fetch from GitHub
    try
    {
      string content = await FetchFromGitHubAsync(relativePath);
      MemoryCache[cacheKey] = new CachedContent(content, DateTime.UtcNow);
      await WriteToDiskCacheAsync(cacheDir, safeName, content);
      return content;
    }
    catch (HttpRequestException)
    {
      // Try disk cache as fallback (even if expired)
      string? fallback = await ReadFromDiskCacheAsync(cacheDir, safeName, TimeSpan.MaxValue);
      if (fallback is not null)
      {
        return fallback;
      }

      return null;
    }
  }

  private static async Task<string> FetchFromGitHubAsync(string relativePath)
  {
    Uri url = new($"{GitHubRawBaseUrl}{relativePath}");
    HttpResponseMessage response = await HttpClient.GetAsync(url);
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadAsStringAsync();
  }

  private static async Task<string?> ReadFromDiskCacheAsync(string cacheDir, string name, TimeSpan ttl)
  {
    try
    {
      string cacheFile = Path.Combine(cacheDir, $"{name}.cache");
      if (!File.Exists(cacheFile))
      {
        // Also try .md extension for backwards compatibility
        cacheFile = Path.Combine(cacheDir, $"{name}.md");
        if (!File.Exists(cacheFile))
          return null;
      }

      string metaFile = Path.Combine(cacheDir, $"{name}.meta");
      if (!File.Exists(metaFile))
        return null;

      string metaContent = await File.ReadAllTextAsync(metaFile);
      if (DateTime.TryParse(metaContent, out DateTime cachedTime))
      {
        if (DateTime.UtcNow - cachedTime < ttl)
        {
          return await File.ReadAllTextAsync(cacheFile);
        }
      }
    }
    catch (IOException) { }
    catch (UnauthorizedAccessException) { }

    return null;
  }

  private static async Task WriteToDiskCacheAsync(string cacheDir, string name, string content)
  {
    try
    {
      Directory.CreateDirectory(cacheDir);

      string cacheFile = Path.Combine(cacheDir, $"{name}.cache");
      string metaFile = Path.Combine(cacheDir, $"{name}.meta");

      await File.WriteAllTextAsync(cacheFile, content);
      await File.WriteAllTextAsync(metaFile, DateTime.UtcNow.ToString("O"));
    }
    catch (IOException) { }
    catch (UnauthorizedAccessException) { }
  }

  private static string GetSafeCacheFileName(string path)
  {
    // Use just the filename without extension, or hash for complex paths
    string fileName = Path.GetFileNameWithoutExtension(path);
    if (string.IsNullOrEmpty(fileName))
    {
      fileName = path.Replace('/', '-').Replace('\\', '-');
    }

    return fileName;
  }

  private sealed class CachedContent
  {
    public string Content { get; }
    public DateTime CachedAt { get; }

    public CachedContent(string content, DateTime cachedAt)
    {
      Content = content;
      CachedAt = cachedAt;
    }

    public bool IsValid(TimeSpan ttl) => DateTime.UtcNow - CachedAt < ttl;
  }
}
