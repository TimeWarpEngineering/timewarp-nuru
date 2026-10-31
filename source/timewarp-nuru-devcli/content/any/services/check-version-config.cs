#region Purpose
// Configuration model for the check-version command.
// Deserialized from .timewarp/dev.jsonc under the "checkVersionConfig" key.
// PackageFeedsResolver turns checkVersionConfig.feeds into the feed set both
// publish gates (check-version and `dev release`) read (kanban task 564).
#endregion
#region Design
// feeds uses the grammar of packages: comma-separated, entries trimmed, empty
// entries dropped. Names are case-insensitive: "nuget.org" and
// "github-packages". Absent (null) means nuget.org only: the feed set is a
// property of the repo, and a repo that does not publish to GitHub Packages
// must not need a GitHub token to release (see nuget-version-service.cs).
//
// A present value that names no feed ("", ",") or names an unknown feed is a
// configuration error, never a silent fallback to the default. The gates
// report it before any network call. Silently reading nuget.org only for a
// typo such as "github-package" would let a version already on GitHub
// Packages look unpublished.
#endregion

namespace DevCli;

/// <summary>
/// Per-repo configuration for version checking.
/// </summary>
internal sealed class CheckVersionConfig
{
  /// <summary>
  /// Comma-separated NuGet package IDs to check.
  /// </summary>
  public string? Packages { get; init; }

  /// <summary>
  /// Comma-separated feeds the repo publishes to: <c>nuget.org</c>, <c>github-packages</c>.
  /// Absent means <c>nuget.org</c>. See <see cref="PackageFeedsResolver"/>.
  /// </summary>
  public string? Feeds { get; init; }
}

/// <summary>The feeds a repo publishes to, and so the feeds the publish gates read.</summary>
[Flags]
internal enum PackageFeeds
{
  None = 0,

  /// <summary>https://api.nuget.org.</summary>
  NuGetOrg = 1,

  /// <summary>The org GitHub Packages feed (<see cref="NuGetVersionService.GitHubPackagesServiceIndexUrl"/>).</summary>
  GitHubPackages = 2,

  Both = NuGetOrg | GitHubPackages
}

/// <summary>
/// Result of <see cref="PackageFeedsResolver.Resolve"/>: <see cref="Error"/> is
/// non-null when the value is invalid, and then <see cref="Feeds"/> is
/// <see cref="PackageFeeds.None"/>.
/// </summary>
internal readonly record struct PackageFeedsResolution(PackageFeeds Feeds, string? Error);

/// <summary>
/// Pure resolver for <c>checkVersionConfig.feeds</c>. No I/O.
/// </summary>
internal static class PackageFeedsResolver
{
  public const string NuGetOrgName = "nuget.org";

  public const string GitHubPackagesName = "github-packages";

  /// <summary>The feed set when <c>feeds</c> is absent.</summary>
  public const PackageFeeds DefaultFeeds = PackageFeeds.NuGetOrg;

  /// <summary>
  /// Resolve a raw <c>checkVersionConfig.feeds</c> value. <see langword="null"/> →
  /// <see cref="DefaultFeeds"/>. Known names, comma-separated → their union. A value
  /// with no names, or with an unknown name → an error naming the valid values.
  /// </summary>
  public static PackageFeedsResolution Resolve(string? rawFeeds)
  {
    if (rawFeeds is null)
    {
      return new PackageFeedsResolution(DefaultFeeds, null);
    }

    string[] names = rawFeeds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    if (names.Length == 0)
    {
      return new PackageFeedsResolution(PackageFeeds.None, $"checkVersionConfig.feeds in .timewarp/dev.jsonc names no feed. Valid feeds: {NuGetOrgName}, {GitHubPackagesName} (comma-separated); delete the key for the default ({NuGetOrgName}).");
    }

    PackageFeeds feeds = PackageFeeds.None;
    List<string> unknown = [];

    foreach (string name in names)
    {
      if (string.Equals(name, NuGetOrgName, StringComparison.OrdinalIgnoreCase))
      {
        feeds |= PackageFeeds.NuGetOrg;
      }
      else if (string.Equals(name, GitHubPackagesName, StringComparison.OrdinalIgnoreCase))
      {
        feeds |= PackageFeeds.GitHubPackages;
      }
      else
      {
        unknown.Add(name);
      }
    }

    if (unknown.Count > 0)
    {
      return new PackageFeedsResolution(PackageFeeds.None, $"checkVersionConfig.feeds in .timewarp/dev.jsonc has unknown feed(s): {string.Join(", ", unknown.Select(n => $"'{n}'"))}. Valid feeds: {NuGetOrgName}, {GitHubPackagesName} (comma-separated).");
    }

    return new PackageFeedsResolution(feeds, null);
  }
}
