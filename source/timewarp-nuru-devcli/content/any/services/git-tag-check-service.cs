#region Purpose
// Git tag version checking — replaces TimeWarp.Amuru's RepoCheckVersionService
// to avoid the INuGetPackageService dependency (which pulls in NuGet.Protocol).
// Only implements git-tag strategy; nuget-search is handled by NuGetVersionService.
#endregion

namespace DevCli;

public sealed class GitTagCheckService
{
  public static async Task<GitTagCheckResult> CheckGitTagVersionAsync
  (
    string? tag,
    CancellationToken cancellationToken
  )
  {
    string? repoRoot = Git.FindRoot();
    if (repoRoot is null)
    {
      return new GitTagCheckResult(false, string.Empty, null);
    }

    string? version = GetVersionFromSource(repoRoot);
    if (version is null)
    {
      return new GitTagCheckResult(false, string.Empty, null);
    }

    string? latestTag = tag;

    if (string.IsNullOrWhiteSpace(latestTag))
    {
      latestTag = Environment.GetEnvironmentVariable("GITHUB_REF_NAME");
    }

    if (string.IsNullOrWhiteSpace(latestTag))
    {
      latestTag = await GetLatestGitTagAsync(cancellationToken).ConfigureAwait(false);
    }

    if (string.IsNullOrWhiteSpace(latestTag))
    {
      return new GitTagCheckResult(true, version, null);
    }

    string tagVersion = latestTag.StartsWith('v') ? latestTag[1..] : latestTag;
    bool isNewVersion = !string.Equals(version, tagVersion, StringComparison.OrdinalIgnoreCase);

    return new GitTagCheckResult(isNewVersion, version, latestTag);
  }

  private static string? GetVersionFromSource(string repoRoot)
  {
    string sourceDir = Path.Combine(repoRoot, "source");
    if (!Directory.Exists(sourceDir))
    {
      return null;
    }

    string[] buildPropsFiles = Directory.GetFiles(sourceDir, "Directory.Build.props", SearchOption.TopDirectoryOnly);
    if (buildPropsFiles is not { Length: > 0 })
    {
      return null;
    }

    string xml = File.ReadAllText(buildPropsFiles[0]);
#pragma warning disable IDE0007
    XDocument doc = XDocument.Parse(xml);
#pragma warning restore IDE0007
    XNamespace ns = "http://schemas.microsoft.com/developer/msbuild/2003";

      XElement? versionElement = doc.Descendants(ns + "Version").FirstOrDefault();
      return (versionElement ?? doc.Descendants("Version").FirstOrDefault())?.Value;
  }

  private static async Task<string?> GetLatestGitTagAsync(CancellationToken cancellationToken)
  {
    CommandOutput result = await Shell.Builder("git")
      .WithArguments("tag", "--sort=-v:refname")
      .CaptureAsync(cancellationToken);

    if (result.ExitCode != 0)
    {
      return null;
    }

    string tagOutput = result.Stdout.Trim();
    if (string.IsNullOrWhiteSpace(tagOutput))
    {
      return null;
    }

    string normalizedOutput = tagOutput.Replace("\r\n", "\n", StringComparison.Ordinal);
    string[] lines = normalizedOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    return lines is { Length: > 0 } ? lines[0] : null;
  }
}

public sealed record GitTagCheckResult
(
  bool IsNewVersion,
  string Version,
  string? LatestReleaseTag
);
