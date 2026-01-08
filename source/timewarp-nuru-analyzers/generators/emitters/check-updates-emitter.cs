// Emits check-updates route generation code.
// Generates the CheckForUpdatesAsync method for --check-updates flag handling.

namespace TimeWarp.Nuru.Generators;

using System.Text;

/// <summary>
/// Emits code to check for updates from GitHub releases.
/// Creates the CheckForUpdatesAsync method and supporting helpers.
/// </summary>
internal static class CheckUpdatesEmitter
{
  /// <summary>
  /// Emits all check-updates related code for an application.
  /// </summary>
  /// <param name="sb">The StringBuilder to append to.</param>
  /// <param name="model">The application model containing metadata.</param>
  public static void Emit(StringBuilder sb, AppModel model)
  {
    EmitGitHubUrlRegex(sb);
    sb.AppendLine();
    EmitCheckForUpdatesMethod(sb);
    sb.AppendLine();
    EmitFetchGitHubReleasesMethod(sb);
    sb.AppendLine();
    EmitFindLatestRelease(sb);
    sb.AppendLine();
    EmitNormalizeTagVersion(sb);
    sb.AppendLine();
    EmitSemVerComparer(sb);
    sb.AppendLine();
    EmitGitHubReleaseRecord(sb);
    sb.AppendLine();
    EmitJsonSerializerContext(sb);
  }

  /// <summary>
  /// Emits the GeneratedRegex for parsing GitHub URLs.
  /// </summary>
  private static void EmitGitHubUrlRegex(StringBuilder sb)
  {
    sb.AppendLine("  // ═══════════════════════════════════════════════════════════════════════════════");
    sb.AppendLine("  // CHECK-UPDATES SUPPORT");
    sb.AppendLine("  // ═══════════════════════════════════════════════════════════════════════════════");
    sb.AppendLine();
    sb.AppendLine("  [global::System.Text.RegularExpressions.GeneratedRegex(@\"^https?://github\\.com/([^/]+)/([^/\\.]+)(?:\\.git)?/?$\", global::System.Text.RegularExpressions.RegexOptions.IgnoreCase)]");
    sb.AppendLine("  private static partial global::System.Text.RegularExpressions.Regex GitHubUrlPattern();");
  }

  /// <summary>
  /// Emits the main CheckForUpdatesAsync method.
  /// </summary>
  private static void EmitCheckForUpdatesMethod(StringBuilder sb)
  {
    sb.AppendLine("  private static async global::System.Threading.Tasks.Task CheckForUpdatesAsync(ITerminal terminal)");
    sb.AppendLine("  {");
    sb.AppendLine("    global::System.Reflection.Assembly? entryAssembly = global::System.Reflection.Assembly.GetEntryAssembly();");
    sb.AppendLine();
    sb.AppendLine("    if (entryAssembly is null)");
    sb.AppendLine("    {");
    sb.AppendLine("      await terminal.WriteErrorLineAsync(\"Unable to check for updates: Cannot determine entry assembly\").ConfigureAwait(false);");
    sb.AppendLine("      return;");
    sb.AppendLine("    }");
    sb.AppendLine();
    sb.AppendLine("    // Get repository URL from assembly metadata");
    sb.AppendLine("    global::System.Collections.Generic.List<global::System.Reflection.AssemblyMetadataAttribute> metadata = [.. entryAssembly.GetCustomAttributes<global::System.Reflection.AssemblyMetadataAttribute>()];");
    sb.AppendLine("    string? repositoryUrl = metadata.Find(m => m.Key == \"RepositoryUrl\")?.Value;");
    sb.AppendLine();
    sb.AppendLine("    if (string.IsNullOrEmpty(repositoryUrl))");
    sb.AppendLine("    {");
    sb.AppendLine("      await terminal.WriteErrorLineAsync(\"Unable to check for updates: RepositoryUrl not configured in project\").ConfigureAwait(false);");
    sb.AppendLine("      return;");
    sb.AppendLine("    }");
    sb.AppendLine();
    sb.AppendLine("    // Parse GitHub owner and repo from URL");
    sb.AppendLine("    global::System.Text.RegularExpressions.Match match = GitHubUrlPattern().Match(repositoryUrl);");
    sb.AppendLine("    if (!match.Success)");
    sb.AppendLine("    {");
    sb.AppendLine("      await terminal.WriteErrorLineAsync(\"Unable to check for updates: RepositoryUrl is not a GitHub URL\").ConfigureAwait(false);");
    sb.AppendLine("      return;");
    sb.AppendLine("    }");
    sb.AppendLine();
    sb.AppendLine("    string owner = match.Groups[1].Value;");
    sb.AppendLine("    string repo = match.Groups[2].Value;");
    sb.AppendLine();
    sb.AppendLine("    // Get current version");
    sb.AppendLine("    string currentVersion = entryAssembly.GetCustomAttribute<global::System.Reflection.AssemblyInformationalVersionAttribute>()?.InformationalVersion");
    sb.AppendLine("      ?? entryAssembly.GetName().Version?.ToString()");
    sb.AppendLine("      ?? string.Empty;");
    sb.AppendLine();
    sb.AppendLine("    if (string.IsNullOrEmpty(currentVersion))");
    sb.AppendLine("    {");
    sb.AppendLine("      await terminal.WriteErrorLineAsync(\"Unable to check for updates: Cannot determine current version\").ConfigureAwait(false);");
    sb.AppendLine("      return;");
    sb.AppendLine("    }");
    sb.AppendLine();
    sb.AppendLine("    // Strip build metadata (+hash) from current version");
    sb.AppendLine("    int plusIndex = currentVersion.IndexOf('+', global::System.StringComparison.Ordinal);");
    sb.AppendLine("    if (plusIndex >= 0)");
    sb.AppendLine("    {");
    sb.AppendLine("      currentVersion = currentVersion[..plusIndex];");
    sb.AppendLine("    }");
    sb.AppendLine();
    sb.AppendLine("    // Determine if current version is pre-release (contains -)");
    sb.AppendLine("    bool isPrerelease = currentVersion.Contains('-', global::System.StringComparison.Ordinal);");
    sb.AppendLine();
    sb.AppendLine("    // Get app name for User-Agent header");
    sb.AppendLine("    string appName = entryAssembly.GetName().Name ?? \"NuruApp\";");
    sb.AppendLine();
    sb.AppendLine("    try");
    sb.AppendLine("    {");
    sb.AppendLine("      CheckUpdatesGitHubRelease[] releases = await FetchGitHubReleasesAsync(owner, repo, appName).ConfigureAwait(false);");
    sb.AppendLine();
    sb.AppendLine("      if (releases.Length == 0)");
    sb.AppendLine("      {");
    sb.AppendLine("        await terminal.WriteErrorLineAsync(\"Unable to check for updates: No releases found\").ConfigureAwait(false);");
    sb.AppendLine("        return;");
    sb.AppendLine("      }");
    sb.AppendLine();
    sb.AppendLine("      // Filter releases based on pre-release status");
    sb.AppendLine("      // If current is stable, only consider stable releases");
    sb.AppendLine("      // If current is pre-release, consider all releases");
    sb.AppendLine("      CheckUpdatesGitHubRelease[] candidateReleases = isPrerelease");
    sb.AppendLine("        ? releases");
    sb.AppendLine("        : [.. releases.Where(r => !r.Prerelease)];");
    sb.AppendLine();
    sb.AppendLine("      if (candidateReleases.Length == 0)");
    sb.AppendLine("      {");
    sb.AppendLine("        terminal.WriteLine(\"\\u2713 You are on the latest version\".Green());");
    sb.AppendLine("        return;");
    sb.AppendLine("      }");
    sb.AppendLine();
    sb.AppendLine("      // Find the latest release by comparing versions");
    sb.AppendLine("      CheckUpdatesGitHubRelease? latestRelease = FindLatestRelease(candidateReleases, currentVersion);");
    sb.AppendLine();
    sb.AppendLine("      if (latestRelease is null)");
    sb.AppendLine("      {");
    sb.AppendLine("        terminal.WriteLine(\"\\u2713 You are on the latest version\".Green());");
    sb.AppendLine("        return;");
    sb.AppendLine("      }");
    sb.AppendLine();
    sb.AppendLine("      // Compare versions");
    sb.AppendLine("      string latestVersion = NormalizeTagVersion(latestRelease.TagName);");
    sb.AppendLine("      int comparison = CheckUpdatesSemVerComparer.Compare(currentVersion, latestVersion);");
    sb.AppendLine();
    sb.AppendLine("      if (comparison >= 0)");
    sb.AppendLine("      {");
    sb.AppendLine("        terminal.WriteLine(\"\\u2713 You are on the latest version\".Green());");
    sb.AppendLine("      }");
    sb.AppendLine("      else");
    sb.AppendLine("      {");
    sb.AppendLine("        terminal.WriteLine($\"\\u26a0 A newer version is available: {latestVersion}\".Yellow());");
    sb.AppendLine("        if (latestRelease.PublishedAt.HasValue)");
    sb.AppendLine("        {");
    sb.AppendLine("          terminal.WriteLine($\"  Released: {latestRelease.PublishedAt.Value:yyyy-MM-dd}\");");
    sb.AppendLine("        }");
    sb.AppendLine("        terminal.WriteLine($\"  {latestRelease.HtmlUrl}\");");
    sb.AppendLine("      }");
    sb.AppendLine("    }");
    sb.AppendLine("    catch (global::System.Net.Http.HttpRequestException ex)");
    sb.AppendLine("    {");
    sb.AppendLine("      await terminal.WriteErrorLineAsync($\"Unable to check for updates: Network error - {ex.Message}\").ConfigureAwait(false);");
    sb.AppendLine("    }");
    sb.AppendLine("    catch (global::System.Threading.Tasks.TaskCanceledException)");
    sb.AppendLine("    {");
    sb.AppendLine("      await terminal.WriteErrorLineAsync(\"Unable to check for updates: Request timed out\").ConfigureAwait(false);");
    sb.AppendLine("    }");
    sb.AppendLine("    catch (global::System.Text.Json.JsonException)");
    sb.AppendLine("    {");
    sb.AppendLine("      await terminal.WriteErrorLineAsync(\"Unable to check for updates: Invalid response from GitHub\").ConfigureAwait(false);");
    sb.AppendLine("    }");
    sb.AppendLine("  }");
  }

  /// <summary>
  /// Emits the FetchGitHubReleasesAsync helper method.
  /// </summary>
  private static void EmitFetchGitHubReleasesMethod(StringBuilder sb)
  {
    sb.AppendLine("  private static async global::System.Threading.Tasks.Task<CheckUpdatesGitHubRelease[]> FetchGitHubReleasesAsync(string owner, string repo, string userAgent)");
    sb.AppendLine("  {");
    sb.AppendLine("    using global::System.Net.Http.HttpClient client = new();");
    sb.AppendLine("    client.DefaultRequestHeaders.Add(\"User-Agent\", userAgent);");
    sb.AppendLine("    client.DefaultRequestHeaders.Add(\"Accept\", \"application/vnd.github+json\");");
    sb.AppendLine("    client.Timeout = global::System.TimeSpan.FromSeconds(10);");
    sb.AppendLine();
    sb.AppendLine("    global::System.Uri apiUri = new($\"https://api.github.com/repos/{owner}/{repo}/releases\");");
    sb.AppendLine("    using global::System.Net.Http.HttpResponseMessage response = await client.GetAsync(apiUri).ConfigureAwait(false);");
    sb.AppendLine("    response.EnsureSuccessStatusCode();");
    sb.AppendLine();
    sb.AppendLine("    string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);");
    sb.AppendLine("    return global::System.Text.Json.JsonSerializer.Deserialize(json, CheckUpdatesJsonSerializerContext.Default.CheckUpdatesGitHubReleaseArray) ?? [];");
    sb.AppendLine("  }");
  }

  /// <summary>
  /// Emits the FindLatestRelease helper method.
  /// </summary>
  private static void EmitFindLatestRelease(StringBuilder sb)
  {
    sb.AppendLine("  private static CheckUpdatesGitHubRelease? FindLatestRelease(CheckUpdatesGitHubRelease[] releases, string currentVersion)");
    sb.AppendLine("  {");
    sb.AppendLine("    CheckUpdatesGitHubRelease? latest = null;");
    sb.AppendLine("    string latestVersion = currentVersion;");
    sb.AppendLine();
    sb.AppendLine("    foreach (CheckUpdatesGitHubRelease release in releases)");
    sb.AppendLine("    {");
    sb.AppendLine("      string releaseVersion = NormalizeTagVersion(release.TagName);");
    sb.AppendLine("      if (CheckUpdatesSemVerComparer.Compare(releaseVersion, latestVersion) > 0)");
    sb.AppendLine("      {");
    sb.AppendLine("        latest = release;");
    sb.AppendLine("        latestVersion = releaseVersion;");
    sb.AppendLine("      }");
    sb.AppendLine("    }");
    sb.AppendLine();
    sb.AppendLine("    return latest;");
    sb.AppendLine("  }");
  }

  /// <summary>
  /// Emits the NormalizeTagVersion helper method.
  /// </summary>
  private static void EmitNormalizeTagVersion(StringBuilder sb)
  {
    sb.AppendLine("  private static string NormalizeTagVersion(string tagName)");
    sb.AppendLine("  {");
    sb.AppendLine("    if (tagName.StartsWith('v') || tagName.StartsWith('V'))");
    sb.AppendLine("    {");
    sb.AppendLine("      return tagName[1..];");
    sb.AppendLine("    }");
    sb.AppendLine("    return tagName;");
    sb.AppendLine("  }");
  }

  /// <summary>
  /// Emits the SemVerComparer nested static class.
  /// </summary>
  private static void EmitSemVerComparer(StringBuilder sb)
  {
    sb.AppendLine("  private static class CheckUpdatesSemVerComparer");
    sb.AppendLine("  {");
    sb.AppendLine("    public static int Compare(string version1, string version2)");
    sb.AppendLine("    {");
    sb.AppendLine("      (string baseVersion1, string? prerelease1) = SplitVersion(version1);");
    sb.AppendLine("      (string baseVersion2, string? prerelease2) = SplitVersion(version2);");
    sb.AppendLine();
    sb.AppendLine("      int baseComparison = CompareBaseVersions(baseVersion1, baseVersion2);");
    sb.AppendLine("      if (baseComparison != 0)");
    sb.AppendLine("      {");
    sb.AppendLine("        return baseComparison;");
    sb.AppendLine("      }");
    sb.AppendLine();
    sb.AppendLine("      if (prerelease1 is null && prerelease2 is null)");
    sb.AppendLine("      {");
    sb.AppendLine("        return 0;");
    sb.AppendLine("      }");
    sb.AppendLine();
    sb.AppendLine("      if (prerelease1 is null)");
    sb.AppendLine("      {");
    sb.AppendLine("        return 1;");
    sb.AppendLine("      }");
    sb.AppendLine();
    sb.AppendLine("      if (prerelease2 is null)");
    sb.AppendLine("      {");
    sb.AppendLine("        return -1;");
    sb.AppendLine("      }");
    sb.AppendLine();
    sb.AppendLine("      return ComparePrereleaseLabels(prerelease1, prerelease2);");
    sb.AppendLine("    }");
    sb.AppendLine();
    sb.AppendLine("    private static (string BaseVersion, string? Prerelease) SplitVersion(string version)");
    sb.AppendLine("    {");
    sb.AppendLine("      int dashIndex = version.IndexOf('-', global::System.StringComparison.Ordinal);");
    sb.AppendLine("      if (dashIndex >= 0)");
    sb.AppendLine("      {");
    sb.AppendLine("        return (version[..dashIndex], version[(dashIndex + 1)..]);");
    sb.AppendLine("      }");
    sb.AppendLine("      return (version, null);");
    sb.AppendLine("    }");
    sb.AppendLine();
    sb.AppendLine("    private static int CompareBaseVersions(string baseVersion1, string baseVersion2)");
    sb.AppendLine("    {");
    sb.AppendLine("      string[] parts1 = baseVersion1.Split('.');");
    sb.AppendLine("      string[] parts2 = baseVersion2.Split('.');");
    sb.AppendLine();
    sb.AppendLine("      int maxParts = global::System.Math.Max(parts1.Length, parts2.Length);");
    sb.AppendLine("      for (int i = 0; i < maxParts; i++)");
    sb.AppendLine("      {");
    sb.AppendLine("        int num1 = i < parts1.Length && int.TryParse(parts1[i], out int n1) ? n1 : 0;");
    sb.AppendLine("        int num2 = i < parts2.Length && int.TryParse(parts2[i], out int n2) ? n2 : 0;");
    sb.AppendLine();
    sb.AppendLine("        if (num1 != num2)");
    sb.AppendLine("        {");
    sb.AppendLine("          return num1.CompareTo(num2);");
    sb.AppendLine("        }");
    sb.AppendLine("      }");
    sb.AppendLine();
    sb.AppendLine("      return 0;");
    sb.AppendLine("    }");
    sb.AppendLine();
    sb.AppendLine("    private static int ComparePrereleaseLabels(string prerelease1, string prerelease2)");
    sb.AppendLine("    {");
    sb.AppendLine("      string[] segments1 = prerelease1.Split('.');");
    sb.AppendLine("      string[] segments2 = prerelease2.Split('.');");
    sb.AppendLine();
    sb.AppendLine("      int maxSegments = global::System.Math.Max(segments1.Length, segments2.Length);");
    sb.AppendLine("      for (int i = 0; i < maxSegments; i++)");
    sb.AppendLine("      {");
    sb.AppendLine("        if (i >= segments1.Length)");
    sb.AppendLine("        {");
    sb.AppendLine("          return -1;");
    sb.AppendLine("        }");
    sb.AppendLine();
    sb.AppendLine("        if (i >= segments2.Length)");
    sb.AppendLine("        {");
    sb.AppendLine("          return 1;");
    sb.AppendLine("        }");
    sb.AppendLine();
    sb.AppendLine("        string seg1 = segments1[i];");
    sb.AppendLine("        string seg2 = segments2[i];");
    sb.AppendLine();
    sb.AppendLine("        bool isNum1 = int.TryParse(seg1, out int num1);");
    sb.AppendLine("        bool isNum2 = int.TryParse(seg2, out int num2);");
    sb.AppendLine();
    sb.AppendLine("        if (isNum1 && isNum2)");
    sb.AppendLine("        {");
    sb.AppendLine("          int numCompare = num1.CompareTo(num2);");
    sb.AppendLine("          if (numCompare != 0)");
    sb.AppendLine("          {");
    sb.AppendLine("            return numCompare;");
    sb.AppendLine("          }");
    sb.AppendLine("        }");
    sb.AppendLine("        else if (isNum1)");
    sb.AppendLine("        {");
    sb.AppendLine("          return -1;");
    sb.AppendLine("        }");
    sb.AppendLine("        else if (isNum2)");
    sb.AppendLine("        {");
    sb.AppendLine("          return 1;");
    sb.AppendLine("        }");
    sb.AppendLine("        else");
    sb.AppendLine("        {");
    sb.AppendLine("          int strCompare = global::System.StringComparer.Ordinal.Compare(seg1, seg2);");
    sb.AppendLine("          if (strCompare != 0)");
    sb.AppendLine("          {");
    sb.AppendLine("            return strCompare;");
    sb.AppendLine("          }");
    sb.AppendLine("        }");
    sb.AppendLine("      }");
    sb.AppendLine();
    sb.AppendLine("      return 0;");
    sb.AppendLine("    }");
    sb.AppendLine("  }");
  }

  /// <summary>
  /// Emits the GitHubRelease record for JSON deserialization.
  /// </summary>
  private static void EmitGitHubReleaseRecord(StringBuilder sb)
  {
    sb.AppendLine("  private sealed record CheckUpdatesGitHubRelease(");
    sb.AppendLine("    [property: global::System.Text.Json.Serialization.JsonPropertyName(\"tag_name\")] string TagName,");
    sb.AppendLine("    [property: global::System.Text.Json.Serialization.JsonPropertyName(\"prerelease\")] bool Prerelease,");
    sb.AppendLine("    [property: global::System.Text.Json.Serialization.JsonPropertyName(\"published_at\")] global::System.DateTime? PublishedAt,");
    sb.AppendLine("    [property: global::System.Text.Json.Serialization.JsonPropertyName(\"html_url\")] string HtmlUrl);");
  }

  /// <summary>
  /// Emits the JSON serializer context for AOT compatibility.
  /// </summary>
  private static void EmitJsonSerializerContext(StringBuilder sb)
  {
    sb.AppendLine("  [global::System.Text.Json.Serialization.JsonSerializable(typeof(CheckUpdatesGitHubRelease))]");
    sb.AppendLine("  [global::System.Text.Json.Serialization.JsonSerializable(typeof(CheckUpdatesGitHubRelease[]))]");
    sb.AppendLine("  [global::System.Text.Json.Serialization.JsonSourceGenerationOptions(PropertyNamingPolicy = global::System.Text.Json.Serialization.JsonKnownNamingPolicy.SnakeCaseLower)]");
    sb.AppendLine("  private partial class CheckUpdatesJsonSerializerContext : global::System.Text.Json.Serialization.JsonSerializerContext;");
  }
}
