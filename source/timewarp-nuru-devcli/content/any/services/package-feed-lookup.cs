#region Purpose
// One package's versions on the feeds its repo declares, and the publish
// verdict the check-version and `dev release` gates act on (kanban tasks 536,
// 564).
#endregion
#region Design
// TimeWarp publishes to two feeds only: nuget.org and the org GitHub Packages
// feed. A repo declares which of them it publishes to (checkVersionConfig
// .feeds, default nuget.org, task 564), and only the declared feeds are read;
// an undeclared feed's version list is empty and it has no failure. A version
// is published when any declared feed lists it. A nuget.org failure is thrown
// by NuGetVersionService and never reaches this type. A GitHub Packages
// failure is kept here instead, because a version found on nuget.org is
// published whatever GitHub Packages says. Only "on no declared feed, and the
// declared GitHub Packages feed unreadable" is Unknown, and the gates refuse
// on Unknown. A nuget.org-only repo therefore never sees Unknown.
//
// The refusal text is shared by both gates. It names the feed, the HTTP
// status and the fix, and it never says "already released" or "already
// published": in CI the usual cause is a job token without packages: read,
// and the operator must not be told to bump the version. With both feeds
// declared it is the 536 text unchanged; a GitHub-Packages-only repo gets the
// same lines without the "not on nuget.org" clause, which would be untrue.
//
// Feeds list versions in different orders (nuget.org ascending, GitHub
// Packages newest first, observed 2026-10-29), so LatestVersion is the SemVer
// maximum, never the last entry.
#endregion

namespace DevCli;

using System.Net;

internal enum PackagePublishVerdict
{
  NotPublished,
  Published,
  Unknown
}

internal sealed class PackageFeedLookup
{
  public PackageFeedLookup
  (
    string packageId,
    PackageFeeds feeds,
    IReadOnlyList<string> nuGetOrgVersions,
    IReadOnlyList<string> gitHubPackagesVersions,
    PackageFeedUnreadableException? gitHubPackagesFailure
  )
  {
    ArgumentNullException.ThrowIfNull(packageId);
    ArgumentNullException.ThrowIfNull(nuGetOrgVersions);
    ArgumentNullException.ThrowIfNull(gitHubPackagesVersions);

    if (gitHubPackagesFailure is not null && !feeds.HasFlag(PackageFeeds.GitHubPackages))
    {
      throw new ArgumentException("A GitHub Packages failure needs GitHub Packages among the declared feeds.", nameof(gitHubPackagesFailure));
    }

    PackageId = packageId;
    Feeds = feeds;
    NuGetOrgVersions = nuGetOrgVersions;
    GitHubPackagesVersions = gitHubPackagesVersions;
    GitHubPackagesFailure = gitHubPackagesFailure;
  }

  public string PackageId { get; }

  /// <summary>The feeds that were read: the repo's declared feed set.</summary>
  public PackageFeeds Feeds { get; }

  /// <summary>Empty when nuget.org answered 404, or is not a declared feed.</summary>
  public IReadOnlyList<string> NuGetOrgVersions { get; }

  /// <summary>Empty when GitHub Packages answered 404, could not be read, or is not a declared feed.</summary>
  public IReadOnlyList<string> GitHubPackagesVersions { get; }

  /// <summary>Why GitHub Packages could not be read, or <see langword="null"/> when it answered.</summary>
  public PackageFeedUnreadableException? GitHubPackagesFailure { get; }

  /// <summary>The highest version on any declared feed (SemVer precedence), or <see langword="null"/>.</summary>
  public string? LatestVersion
  {
    get
    {
      string? latest = null;
      foreach (string candidate in NuGetOrgVersions.Concat(GitHubPackagesVersions))
      {
        if (latest is null || NuGetVersionService.CompareVersions(candidate, latest) > 0)
        {
          latest = candidate;
        }
      }

      return latest;
    }
  }

  public PackagePublishVerdict Check(string version)
  {
    ArgumentNullException.ThrowIfNull(version);

    if (NuGetVersionService.IsVersionPublished(version, NuGetOrgVersions) ||
        NuGetVersionService.IsVersionPublished(version, GitHubPackagesVersions))
    {
      return PackagePublishVerdict.Published;
    }

    return GitHubPackagesFailure is null ? PackagePublishVerdict.NotPublished : PackagePublishVerdict.Unknown;
  }

  /// <summary>
  /// The lines both gates print when <see cref="Check"/> is Unknown.
  /// </summary>
  public IReadOnlyList<string> FormatUnknownRefusal(string version)
  {
    ArgumentNullException.ThrowIfNull(version);

    PackageFeedUnreadableException failure = GitHubPackagesFailure
      ?? throw new InvalidOperationException($"'{PackageId}' has no GitHub Packages failure to report.");

    string feedLine = Feeds.HasFlag(PackageFeeds.NuGetOrg)
      ? $"  {PackageId} {version} is not on nuget.org, and the org GitHub Packages feed ({NuGetVersionService.GitHubPackagesServiceIndexUrl}) could not be read, so whether it is published is unknown. Refusing to continue."
      : $"  The org GitHub Packages feed ({NuGetVersionService.GitHubPackagesServiceIndexUrl}) could not be read, so whether {PackageId} {version} is published is unknown. Refusing to continue.";

    return
    [
      $"Error: GitHub Packages lookup for '{PackageId}' failed ({failure.StatusDisplay}): {failure.Message}",
      feedLine,
      "  Fix (local): gh auth refresh -h github.com -s read:packages, or set GITHUB_TOKEN / GH_TOKEN to a token with read:packages.",
      "  Fix (GitHub Actions): add 'packages: read' to the job's permissions."
    ];
  }
}

/// <summary>
/// The org GitHub Packages feed could not be read: no token, 401/403, any other
/// non-404 status, a timeout or network error, or a malformed answer. Derives
/// from <see cref="HttpRequestException"/> so a caller that catches lookup
/// failures generically still fails closed.
/// </summary>
internal sealed class PackageFeedUnreadableException : HttpRequestException
{
  public PackageFeedUnreadableException(string message, Exception? inner, HttpStatusCode? statusCode)
    : base(message, inner, statusCode)
  {
  }

  public PackageFeedUnreadableException()
  {
  }

  public PackageFeedUnreadableException(string message)
    : base(message)
  {
  }

  public PackageFeedUnreadableException(string message, Exception inner)
    : base(message, inner)
  {
  }

  public PackageFeedUnreadableException(HttpRequestError httpRequestError, string? message, Exception? inner, HttpStatusCode? statusCode)
    : base(httpRequestError, message, inner, statusCode)
  {
  }

  /// <summary>"HTTP 401 Unauthorized", or "no HTTP response" for a missing token or network failure.</summary>
  public string StatusDisplay => StatusCode is HttpStatusCode status
    ? $"HTTP {(int)status} {status}"
    : "no HTTP response";
}
