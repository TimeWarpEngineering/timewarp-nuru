namespace TimeWarp.Nuru.Mcp.Services;

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Shared service for fetching content from GitHub with multi-tier caching.
/// </summary>
internal static class GitHubCacheService
{
  private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(10) };
  private static readonly ConcurrentDictionary<string, CachedContent> MemoryCache = [];
  private static readonly TimeSpan DefaultCacheTtl = TimeSpan.FromHours(1);

  internal const string GitHubRawBaseUrl = "https://raw.githubusercontent.com/TimeWarpEngineering/timewarp-nuru/master/";
  internal const string AllowedRawHost = "raw.githubusercontent.com";
  internal const string AllowedRepoPathPrefix = "/TimeWarpEngineering/timewarp-nuru/";

  /// <summary>
  /// Prefixes allowed for shared doc/example fetches from this repo.
  /// </summary>
  internal static readonly string[] DefaultAllowedPathPrefixes =
  [
    "samples/",
    "documentation/"
  ];

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
    if (!TryResolveRawContentUri(relativePath, out _, DefaultAllowedPathPrefixes))
    {
      return null;
    }

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

  /// <summary>
  /// Validates a relative repo path and builds a raw.githubusercontent.com URI that
  /// stays under TimeWarpEngineering/timewarp-nuru (rejects .., absolute roots, schemes).
  /// </summary>
  /// <param name="relativePath">Path relative to the repo root.</param>
  /// <param name="uri">Resolved URI when validation succeeds.</param>
  /// <param name="allowedPrefixes">When non-empty, path must start with one of these (forward-slash form).</param>
  internal static bool TryResolveRawContentUri(
      string? relativePath,
      [NotNullWhen(true)] out Uri? uri,
      params string[] allowedPrefixes)
  {
    uri = null;

    if (string.IsNullOrWhiteSpace(relativePath))
    {
      return false;
    }

    string normalized = relativePath.Replace('\\', '/').Trim();

    // Reject schemes, traversal, and percent-encoding (Uri would decode %2e%2e → "..").
    if (normalized.Contains("://", StringComparison.Ordinal) ||
        normalized.Contains("..", StringComparison.Ordinal) ||
        normalized.Contains('%', StringComparison.Ordinal))
    {
      return false;
    }

    if (Path.IsPathRooted(normalized) ||
        normalized.StartsWith('/') ||
        (normalized.Length >= 2 && char.IsAsciiLetter(normalized[0]) && normalized[1] == ':'))
    {
      return false;
    }

    string? matchedPrefix = null;
    if (allowedPrefixes.Length > 0)
    {
      foreach (string prefix in allowedPrefixes)
      {
        if (normalized.StartsWith(prefix, StringComparison.Ordinal))
        {
          matchedPrefix = prefix;
          break;
        }
      }

      if (matchedPrefix is null)
      {
        return false;
      }
    }

    if (!Uri.TryCreate($"{GitHubRawBaseUrl}{normalized}", UriKind.Absolute, out Uri? resolved))
    {
      return false;
    }

    if (!string.Equals(resolved.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
        !string.Equals(resolved.Host, AllowedRawHost, StringComparison.OrdinalIgnoreCase) ||
        !resolved.AbsolutePath.StartsWith(AllowedRepoPathPrefix, StringComparison.Ordinal))
    {
      return false;
    }

    // After Uri normalization, AbsolutePath must still sit under master/<allowlist>.
    // Blocks any future encoding tricks that slip past the literal checks above.
    if (matchedPrefix is not null)
    {
      string resolvedAllowlistPrefix = $"{AllowedRepoPathPrefix}master/{matchedPrefix}";
      if (!resolved.AbsolutePath.StartsWith(resolvedAllowlistPrefix, StringComparison.Ordinal))
      {
        return false;
      }
    }

    uri = resolved;
    return true;
  }

  /// <summary>
  /// Returns whether <paramref name="id"/> is safe to use as a cache / manifest key
  /// (no traversal, absolute roots, URI schemes, or path separators).
  /// </summary>
  internal static bool IsSafeCacheId(string? id)
  {
    if (string.IsNullOrWhiteSpace(id))
    {
      return false;
    }

    if (id.Contains("..", StringComparison.Ordinal) ||
        id.Contains("://", StringComparison.Ordinal) ||
        id.Contains('/', StringComparison.Ordinal) ||
        id.Contains('\\', StringComparison.Ordinal) ||
        Path.IsPathRooted(id) ||
        (id.Length >= 2 && char.IsAsciiLetter(id[0]) && id[1] == ':'))
    {
      return false;
    }

    return true;
  }

  private static async Task<string> FetchFromGitHubAsync(string relativePath)
  {
    if (!TryResolveRawContentUri(relativePath, out Uri? url, DefaultAllowedPathPrefixes))
    {
      throw new InvalidOperationException($"Refusing to fetch disallowed GitHub path: {relativePath}");
    }

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
      if (DateTime.TryParse(metaContent, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime cachedTime))
      {
        if (DateTime.UtcNow - cachedTime.ToUniversalTime() < ttl)
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
      await File.WriteAllTextAsync(metaFile, DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
    }
    catch (IOException) { }
    catch (UnauthorizedAccessException) { }
  }

  internal static string GetSafeCacheFileName(string path)
  {
    // Use the full relative path with separators replaced by '-', KEEPING the
    // extension. This prevents collisions both between directories sharing a
    // filename ("examples/routing/foo.md" vs "examples/parser/foo.md") and between
    // same-directory files differing only by extension ("docs/foo.md" vs
    // "docs/foo.json"). Also collapses ".." / rooted forms so Path.Combine cannot
    // escape the cache directory.
    string safe = path.Replace('/', '-').Replace('\\', '-');

    if (safe is "." or ".." ||
        safe.Contains("..", StringComparison.Ordinal) ||
        Path.IsPathRooted(safe) ||
        safe.Contains(':', StringComparison.Ordinal))
    {
      // Hash fallback for pathological inputs that survive separator replacement.
      byte[] hash = System.Security.Cryptography.SHA256.HashData(
          System.Text.Encoding.UTF8.GetBytes(path));
      return Convert.ToHexString(hash).ToLowerInvariant();
    }

    return safe;
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
