namespace TimeWarp.Nuru;

using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

/// <summary>
/// Update checking route handler for NuruAppBuilderExtensions.
/// </summary>
/// <remarks>
/// This partial class contains:
/// <list type="bullet">
/// <item><description><see cref="AddCheckUpdatesRoute{TBuilder}"/> - Registers the --check-updates route</description></item>
/// <item><description><see cref="CheckForUpdatesAsync"/> - Handler that queries GitHub for newer versions</description></item>
/// <item><description><see cref="FetchGitHubReleasesAsync"/> - HTTP client for GitHub API</description></item>
/// <item><description><see cref="FindLatestRelease"/> - Finds the latest applicable release</description></item>
/// </list>
/// </remarks>
public static partial class NuruAppBuilderExtensions
{
  /// <summary>
  /// Adds a <c>--check-updates</c> route that checks GitHub for newer versions.
  /// </summary>
  /// <typeparam name="TBuilder">The builder type.</typeparam>
  /// <param name="builder">The NuruCoreAppBuilder instance.</param>
  /// <returns>The builder for chaining.</returns>
  /// <remarks>
  /// The check-updates route:
  /// <list type="bullet">
  /// <item><description>Queries GitHub releases API for available versions</description></item>
  /// <item><description>Compares current version against latest release</description></item>
  /// <item><description>Pre-release versions only compare against pre-releases</description></item>
  /// <item><description>Displays colored output (green = up-to-date, yellow = update available)</description></item>
  /// </list>
  /// Requires <c>RepositoryUrl</c> assembly metadata pointing to a GitHub repository.
  /// </remarks>
  public static TBuilder AddCheckUpdatesRoute<TBuilder>(this TBuilder builder)
    where TBuilder : NuruCoreAppBuilder<TBuilder>
  {
    ArgumentNullException.ThrowIfNull(builder);
    builder.Map("--check-updates")
      .WithHandler(CheckForUpdatesAsync)
      .WithDescription("Check for newer versions on GitHub")
      .Done();
    return builder;
  }

  private const string RepositoryUrlKey = "RepositoryUrl";
  private const string GitHubApiBaseUrl = "https://api.github.com/repos/";
  private const string ReleasesEndpoint = "/releases";

  // Regex to parse GitHub URLs like https://github.com/owner/repo or https://github.com/owner/repo.git
  [GeneratedRegex(@"^https?://github\.com/([^/]+)/([^/\.]+)(?:\.git)?/?$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
  private static partial Regex GitHubUrlPattern();

  /// <summary>
  /// Handler for the check-updates route that queries GitHub for newer versions.
  /// Uses Task return to support async HTTP calls while using the common "NoParams_Task" invoker signature.
  /// </summary>
  internal static async Task CheckForUpdatesAsync()
  {
    Assembly? entryAssembly = Assembly.GetEntryAssembly();

    if (entryAssembly is null)
    {
      Console.WriteLine("Unable to check for updates: Cannot determine entry assembly");
      return;
    }

    // Get repository URL from assembly metadata
    List<AssemblyMetadataAttribute> metadata = [.. entryAssembly.GetCustomAttributes<AssemblyMetadataAttribute>()];
    string? repositoryUrl = metadata.Find(m => m.Key == RepositoryUrlKey)?.Value;

    if (string.IsNullOrEmpty(repositoryUrl))
    {
      Console.WriteLine("Unable to check for updates: RepositoryUrl not configured in project");
      return;
    }

    // Parse GitHub owner and repo from URL
    Match match = GitHubUrlPattern().Match(repositoryUrl);
    if (!match.Success)
    {
      Console.WriteLine("Unable to check for updates: RepositoryUrl is not a GitHub URL");
      return;
    }

    string owner = match.Groups[1].Value;
    string repo = match.Groups[2].Value;

    // Get current version
    string currentVersion = entryAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
      ?? entryAssembly.GetName().Version?.ToString()
      ?? string.Empty;

    if (string.IsNullOrEmpty(currentVersion))
    {
      Console.WriteLine("Unable to check for updates: Cannot determine current version");
      return;
    }

    // Strip build metadata (+hash) from current version
    int plusIndex = currentVersion.IndexOf('+', StringComparison.Ordinal);
    if (plusIndex >= 0)
    {
      currentVersion = currentVersion[..plusIndex];
    }

    // Determine if current version is pre-release (contains -)
    bool isPrerelease = currentVersion.Contains('-', StringComparison.Ordinal);

    // Get app name for User-Agent header
    string appName = entryAssembly.GetName().Name ?? "NuruApp";

    try
    {
      GitHubRelease[] releases = await FetchGitHubReleasesAsync(owner, repo, appName).ConfigureAwait(false);

      if (releases.Length == 0)
      {
        Console.WriteLine("Unable to check for updates: No releases found");
        return;
      }

      // Filter releases based on pre-release status
      // If current is stable, only consider stable releases
      // If current is pre-release, consider all releases
      GitHubRelease[] candidateReleases = isPrerelease
        ? releases
        : [.. releases.Where(r => !r.Prerelease)];

      if (candidateReleases.Length == 0)
      {
        Console.WriteLine("✓ You are on the latest version".Green());
        return;
      }

      // Find the latest release by comparing versions
      GitHubRelease? latestRelease = FindLatestRelease(candidateReleases, currentVersion);

      if (latestRelease is null)
      {
        Console.WriteLine("✓ You are on the latest version".Green());
        return;
      }

      // Compare versions
      string latestVersion = NormalizeTagVersion(latestRelease.TagName);
      int comparison = SemVerComparer.Compare(currentVersion, latestVersion);

      if (comparison >= 0)
      {
        Console.WriteLine("✓ You are on the latest version".Green());
      }
      else
      {
        Console.WriteLine($"⚠ A newer version is available: {latestVersion}".Yellow());
        if (latestRelease.PublishedAt.HasValue)
        {
          Console.WriteLine($"  Released: {latestRelease.PublishedAt.Value:yyyy-MM-dd}");
        }

        Console.WriteLine($"  {latestRelease.HtmlUrl}");
      }
    }
    catch (HttpRequestException ex)
    {
      Console.WriteLine($"Unable to check for updates: Network error - {ex.Message}");
    }
    catch (TaskCanceledException)
    {
      Console.WriteLine("Unable to check for updates: Request timed out");
    }
    catch (JsonException)
    {
      Console.WriteLine("Unable to check for updates: Invalid response from GitHub");
    }
  }

  /// <summary>
  /// Fetches releases from the GitHub API.
  /// </summary>
  private static async Task<GitHubRelease[]> FetchGitHubReleasesAsync(string owner, string repo, string userAgent)
  {
    using HttpClient client = new();
    client.DefaultRequestHeaders.Add("User-Agent", userAgent);
    client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
    client.Timeout = TimeSpan.FromSeconds(10);

    Uri apiUri = new($"{GitHubApiBaseUrl}{owner}/{repo}{ReleasesEndpoint}");
    using HttpResponseMessage response = await client.GetAsync(apiUri).ConfigureAwait(false);
    response.EnsureSuccessStatusCode();

    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    return JsonSerializer.Deserialize(json, GitHubJsonSerializerContext.Default.GitHubReleaseArray) ?? [];
  }

  /// <summary>
  /// Finds the latest release that is newer than the current version.
  /// </summary>
  private static GitHubRelease? FindLatestRelease(GitHubRelease[] releases, string currentVersion)
  {
    GitHubRelease? latest = null;
    string latestVersion = currentVersion;

    foreach (GitHubRelease release in releases)
    {
      string releaseVersion = NormalizeTagVersion(release.TagName);
      if (SemVerComparer.Compare(releaseVersion, latestVersion) > 0)
      {
        latest = release;
        latestVersion = releaseVersion;
      }
    }

    return latest;
  }

  /// <summary>
  /// Normalizes a tag version by stripping the leading 'v' or 'V' prefix.
  /// </summary>
  private static string NormalizeTagVersion(string tagName)
  {
    if (tagName.StartsWith('v') || tagName.StartsWith('V'))
    {
      return tagName[1..];
    }

    return tagName;
  }
}
