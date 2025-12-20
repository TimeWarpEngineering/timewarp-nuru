namespace TimeWarp.Nuru;

using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

/// <summary>
/// Extension methods that auto-wire all Nuru extensions to an existing builder.
/// </summary>
public static partial class NuruAppBuilderExtensions
{
  /// <summary>
  /// Adds all Nuru extensions to the builder: Telemetry, Logging, REPL, Shell Completion, and Interactive routes.
  /// This is called automatically by <see cref="NuruApp.CreateBuilder"/>.
  /// </summary>
  /// <param name="builder">The builder to extend.</param>
  /// <param name="options">Optional configuration for extensions. If null, uses defaults.</param>
  /// <returns>The builder for chaining.</returns>
  /// <remarks>
  /// This method adds:
  /// <list type="bullet">
  /// <item>OpenTelemetry integration via <c>UseTelemetry()</c></item>
  /// <item>REPL mode support via <c>AddReplSupport()</c></item>
  /// <item>Dynamic shell completion via <c>EnableDynamicCompletion()</c> (routes: <c>--generate-completion</c>, <c>__complete</c>, <c>--install-completion</c>)</item>
  /// <item>Interactive route (<c>--interactive</c>, <c>-i</c>) via <c>AddInteractiveRoute()</c></item>
  /// </list>
  /// Logging is already configured by <c>UseTelemetry()</c>.
  /// </remarks>
  public static NuruAppBuilder UseAllExtensions(this NuruAppBuilder builder, NuruAppOptions? options = null)
  {
    ArgumentNullException.ThrowIfNull(builder);
    options ??= new NuruAppOptions();

    // Configure help options if provided
    if (options.ConfigureHelp is not null)
    {
      builder.ConfigureHelp(options.ConfigureHelp);
    }

    // Add telemetry unless disabled
    if (!options.DisableTelemetry)
    {
      builder.UseTelemetry(options.ConfigureTelemetry ?? (_ => { }));
    }

    // Add REPL support unless disabled
    if (!options.DisableRepl)
    {
      builder.AddReplSupport(options.ConfigureRepl);
    }

    // Add dynamic shell completion unless disabled
    if (!options.DisableCompletion)
    {
      builder.EnableDynamicCompletion(configure: options.ConfigureCompletion);
    }

    // Add interactive route unless disabled
    if (!options.DisableInteractiveRoute)
    {
      builder.AddInteractiveRoute(options.InteractiveRoutePatterns);
    }

    // Add version route unless disabled
    if (!options.DisableVersionRoute)
    {
      builder.AddVersionRoute();
    }

    // Add check-updates route unless disabled
    if (!options.DisableCheckUpdatesRoute)
    {
      builder.AddCheckUpdatesRoute();
    }

    return builder;
  }

  /// <summary>
  /// Adds a <c>--version,-v</c> route that displays version information including commit hash and date when available.
  /// </summary>
  /// <typeparam name="TBuilder">The builder type.</typeparam>
  /// <param name="builder">The NuruCoreAppBuilder instance.</param>
  /// <returns>The builder for chaining.</returns>
  /// <remarks>
  /// The version output includes:
  /// <list type="bullet">
  /// <item><description>Assembly informational version (or simple version as fallback)</description></item>
  /// <item><description>Commit hash (if available from <c>AssemblyMetadataAttribute</c> with key "CommitHash")</description></item>
  /// <item><description>Commit date (if available from <c>AssemblyMetadataAttribute</c> with key "CommitDate")</description></item>
  /// </list>
  /// This information is automatically injected by TimeWarp.Build.Tasks which is a transitive dependency.
  /// </remarks>
  public static TBuilder AddVersionRoute<TBuilder>(this TBuilder builder)
    where TBuilder : NuruCoreAppBuilder<TBuilder>
  {
    ArgumentNullException.ThrowIfNull(builder);
    builder.Map("--version,-v")
      .WithHandler(DisplayVersion)
      .WithDescription("Display version information")
      .Done();
    return builder;
  }

  private const string VersionUnavailableMessage = "Version information unavailable";
  private const string UnknownVersion = "Unknown";
  private const string CommitHashKey = "CommitHash";
  private const string CommitDateKey = "CommitDate";

  /// <summary>
  /// Handler for the version route that displays version information.
  /// Uses Action (void return) so it uses the common "NoParams" invoker signature
  /// that virtually every consuming app will generate.
  /// </summary>
  internal static void DisplayVersion()
  {
    Assembly? entryAssembly = Assembly.GetEntryAssembly();

    if (entryAssembly is null)
    {
      Console.WriteLine(VersionUnavailableMessage);
      return;
    }

    // Get informational version or fall back to simple version
    string version = entryAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
      ?? entryAssembly.GetName().Version?.ToString()
      ?? UnknownVersion;

    // Strip build metadata suffix (+<hash>) if present, per SemVer 2.0 convention
    // The full commit hash is displayed separately below
    int plusIndex = version.IndexOf('+', StringComparison.Ordinal);
    string displayVersion = plusIndex >= 0 ? version[..plusIndex] : version;
    Console.WriteLine(displayVersion);

    // Get commit hash and date from AssemblyMetadataAttribute (injected by TimeWarp.Build.Tasks)
    // Materialize to list to avoid multiple enumeration
    List<AssemblyMetadataAttribute> metadata = [.. entryAssembly.GetCustomAttributes<AssemblyMetadataAttribute>()];

    string? commitHash = metadata.Find(m => m.Key == CommitHashKey)?.Value;
    string? commitDate = metadata.Find(m => m.Key == CommitDateKey)?.Value;

    if (!string.IsNullOrEmpty(commitHash))
    {
      Console.WriteLine($"Commit: {commitHash}");
    }

    if (!string.IsNullOrEmpty(commitDate))
    {
      Console.WriteLine($"Date: {commitDate}");
    }
  }

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
      int comparison = CompareVersions(currentVersion, latestVersion);

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
      if (CompareVersions(releaseVersion, latestVersion) > 0)
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

  /// <summary>
  /// Compares two SemVer version strings.
  /// Returns: negative if v1 &lt; v2, zero if equal, positive if v1 &gt; v2.
  /// </summary>
  /// <remarks>
  /// Comparison strategy:
  /// 1. Compare major.minor.patch numerically
  /// 2. A stable version (no prerelease) is greater than any prerelease of the same base version
  /// 3. Prerelease labels are compared lexicographically (with numeric segment comparison)
  /// 4. Falls back to published date comparison if versions are unparseable
  /// </remarks>
  internal static int CompareVersions(string version1, string version2)
  {
    // Split into base version and prerelease parts
    (string baseVersion1, string? prerelease1) = SplitVersion(version1);
    (string baseVersion2, string? prerelease2) = SplitVersion(version2);

    // Compare base versions (major.minor.patch)
    int baseComparison = CompareBaseVersions(baseVersion1, baseVersion2);
    if (baseComparison != 0)
    {
      return baseComparison;
    }

    // Base versions are equal, compare prerelease labels
    // Rule: no prerelease > any prerelease (stable is newer)
    if (prerelease1 is null && prerelease2 is null)
    {
      return 0;
    }

    if (prerelease1 is null)
    {
      return 1; // v1 is stable, v2 is prerelease -> v1 is newer
    }

    if (prerelease2 is null)
    {
      return -1; // v1 is prerelease, v2 is stable -> v2 is newer
    }

    // Both have prerelease labels - compare them
    return ComparePrereleaseLabels(prerelease1, prerelease2);
  }

  /// <summary>
  /// Splits a version string into base version and prerelease parts.
  /// </summary>
  private static (string BaseVersion, string? Prerelease) SplitVersion(string version)
  {
    int dashIndex = version.IndexOf('-', StringComparison.Ordinal);
    if (dashIndex >= 0)
    {
      return (version[..dashIndex], version[(dashIndex + 1)..]);
    }

    return (version, null);
  }

  /// <summary>
  /// Compares base versions (major.minor.patch) numerically.
  /// </summary>
  private static int CompareBaseVersions(string baseVersion1, string baseVersion2)
  {
    string[] parts1 = baseVersion1.Split('.');
    string[] parts2 = baseVersion2.Split('.');

    int maxParts = Math.Max(parts1.Length, parts2.Length);
    for (int i = 0; i < maxParts; i++)
    {
      int num1 = i < parts1.Length && int.TryParse(parts1[i], out int n1) ? n1 : 0;
      int num2 = i < parts2.Length && int.TryParse(parts2[i], out int n2) ? n2 : 0;

      if (num1 != num2)
      {
        return num1.CompareTo(num2);
      }
    }

    return 0;
  }

  /// <summary>
  /// Compares prerelease labels following SemVer rules.
  /// Segments are compared: numeric segments numerically, others lexicographically.
  /// </summary>
  private static int ComparePrereleaseLabels(string prerelease1, string prerelease2)
  {
    string[] segments1 = prerelease1.Split('.');
    string[] segments2 = prerelease2.Split('.');

    int maxSegments = Math.Max(segments1.Length, segments2.Length);
    for (int i = 0; i < maxSegments; i++)
    {
      // Missing segments are considered "less than" present ones
      if (i >= segments1.Length)
      {
        return -1;
      }

      if (i >= segments2.Length)
      {
        return 1;
      }

      string seg1 = segments1[i];
      string seg2 = segments2[i];

      bool isNum1 = int.TryParse(seg1, out int num1);
      bool isNum2 = int.TryParse(seg2, out int num2);

      if (isNum1 && isNum2)
      {
        // Both numeric - compare numerically
        int numCompare = num1.CompareTo(num2);
        if (numCompare != 0)
        {
          return numCompare;
        }
      }
      else if (isNum1)
      {
        // Numeric < non-numeric in SemVer
        return -1;
      }
      else if (isNum2)
      {
        // Non-numeric > numeric in SemVer
        return 1;
      }
      else
      {
        // Both non-numeric - compare lexicographically
        int strCompare = StringComparer.Ordinal.Compare(seg1, seg2);
        if (strCompare != 0)
        {
          return strCompare;
        }
      }
    }

    return 0;
  }
}
