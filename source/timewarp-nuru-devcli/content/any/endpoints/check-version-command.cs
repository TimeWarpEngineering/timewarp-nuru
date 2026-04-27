#region Purpose
// Verify that the version in Directory.Build.props has not already been released
#endregion
#region Design
// Uses NuGetVersionService (HttpClient-based) for NuGet checks — no NuGet.Protocol dependency.
// Uses GitTagCheckService for git-tag checks — no INuGetPackageService dependency.
// Uses IRepoConfigService for per-repo config defaults.
// Supports two strategies: nuget-search (checks NuGet) and git-tag (compares to git tag).
// Strategy defaults to per-repo config (.timewarp/dev.jsonc), then nuget-search.
#endregion

namespace DevCli;

using System.Xml.Linq;
using TimeWarp.Nuru;
using TimeWarp.Terminal;

[NuruRoute("check-version", Description = "Verify version is ready to release")]
public sealed class CheckVersionCommand : ICommand<Unit>
{
  [Option("strategy", Description = "Check strategy: git-tag (GitHub releases) or nuget-search (NuGet packages)")]
  public string? Strategy { get; set; }

  [Option("package", Description = "NuGet package ID to check (comma-separated, nuget-search only)")]
  public string? Package { get; set; }

  [Option("tag", Description = "Git tag to compare against (git-tag strategy only)")]
  public string? Tag { get; set; }

  public sealed class Handler : ICommandHandler<CheckVersionCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private readonly NuGetVersionService NuGetVersionService;
    private readonly GitTagCheckService GitTagCheckService;
    private readonly IRepoConfigService ConfigService;

    public Handler
    (
      ITerminal terminal,
      NuGetVersionService nuGetVersionService,
      GitTagCheckService gitTagCheckService,
      IRepoConfigService configService
    )
    {
      Terminal = terminal;
      NuGetVersionService = nuGetVersionService;
      GitTagCheckService = gitTagCheckService;
      ConfigService = configService;
    }

    public async ValueTask<Unit> Handle(CheckVersionCommand command, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(command);

      string? strategyInput = command.Strategy;
      CheckVersionStrategy effectiveStrategy;
      if (strategyInput is not null && TryParseStrategy(strategyInput, out CheckVersionStrategy parsed))
      {
        effectiveStrategy = parsed;
      }
      else if (strategyInput is not null)
      {
        Terminal.WriteErrorLine($"Error: unknown strategy '{strategyInput}'. Valid values: git-tag, nuget-search");
        Environment.ExitCode = 1;
        return Value;
      }
      else
      {
        RepoConfig config = await ConfigService
          .GetConfigAsync(cancellationToken)
          .ConfigureAwait(false);

        effectiveStrategy = config.CheckVersionConfig?.CheckVersionStrategy ?? CheckVersionStrategy.NuGetSearch;
      }

      string strategyDisplay = effectiveStrategy switch
      {
        CheckVersionStrategy.GitTag => "git-tag (GitHub releases)",
        CheckVersionStrategy.NuGetSearch => "nuget-search (NuGet packages)",
        _ => effectiveStrategy.ToString()
      };
      Terminal.WriteLine($"Strategy: {strategyDisplay}");
      Terminal.WriteLine("");

      if (effectiveStrategy == CheckVersionStrategy.GitTag)
      {
        await HandleGitTagAsync(command, cancellationToken).ConfigureAwait(false);
      }
      else if (effectiveStrategy == CheckVersionStrategy.NuGetSearch)
      {
        await HandleNuGetSearchAsync(command, cancellationToken).ConfigureAwait(false);
      }

      return Value;
    }

    private async ValueTask HandleGitTagAsync(CheckVersionCommand command, CancellationToken cancellationToken)
    {
      GitTagCheckResult result = await GitTagCheckService
        .CheckGitTagVersionAsync(command.Tag, cancellationToken)
        .ConfigureAwait(false);

      Terminal.WriteLine($"Version in source: {result.Version}".Cyan());
      string latestTag = result.LatestReleaseTag ?? "(none)";
      Terminal.WriteLine($"Latest release tag on GitHub: {latestTag}".Cyan());
      Terminal.WriteLine("");

      if (result.IsNewVersion)
      {
        Terminal.WriteLine("✓ Version in source is new — safe to release.".Green());
      }
      else
      {
        Terminal.WriteLine($"✗ Version {result.Version} was already released.".Red());
        Terminal.WriteLine("  Bump the version before releasing.".Yellow());
        Environment.ExitCode = 1;
      }
    }

    private async ValueTask HandleNuGetSearchAsync(CheckVersionCommand command, CancellationToken cancellationToken)
    {
      RepoConfig config = await ConfigService
        .GetConfigAsync(cancellationToken)
        .ConfigureAwait(false);

      string? packageInput = command.Package ?? config.CheckVersionConfig?.Packages;
      if (string.IsNullOrWhiteSpace(packageInput))
      {
        Terminal.WriteErrorLine("Error: no packages specified. Use --package or configure Packages in .timewarp/dev.jsonc");
        Environment.ExitCode = 1;
        return;
      }

      IReadOnlyList<string> packages = packageInput.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

      string? version = GetVersionFromSource();
      if (version is null)
      {
        Terminal.WriteErrorLine("Error: could not read Version from source/Directory.Build.props");
        Environment.ExitCode = 1;
        return;
      }

      List<string> checkedPackages = [];
      List<string> alreadyPublished = [];
      string? latestNuGetVersion = null;

      foreach (string pkg in packages)
      {
        checkedPackages.Add(pkg);

        IReadOnlyList<string> versions = await NuGetVersionService
          .GetPackageVersionsAsync(pkg, cancellationToken)
          .ConfigureAwait(false);

        if (versions.Count == 0)
        {
          continue;
        }

        string highestVersion = versions[^1];

        if (latestNuGetVersion is null || NuGetVersionService.CompareVersions(highestVersion, latestNuGetVersion) > 0)
        {
          latestNuGetVersion = highestVersion;
        }

        if (string.Equals(version, highestVersion, StringComparison.OrdinalIgnoreCase))
        {
          alreadyPublished.Add(pkg);
        }
      }

      Terminal.WriteLine($"Version in source: {version}".Cyan());
      string latestDisplay = latestNuGetVersion ?? "(none)";
      Terminal.WriteLine($"Latest NuGet version: {latestDisplay}".Cyan());

      if (checkedPackages.Count > 0)
      {
        Terminal.WriteLine($"Packages checked: {string.Join(", ", checkedPackages)}");
      }

      Terminal.WriteLine("");

      bool isNewVersion = alreadyPublished.Count == 0;

      if (isNewVersion)
      {
        Terminal.WriteLine("✓ Version in source is new — safe to release.".Green());
      }
      else
      {
        Terminal.WriteLine($"✗ Version {version} was already released.".Red());
        Terminal.WriteLine("  Bump the version before releasing.".Yellow());

        if (alreadyPublished.Count > 0)
        {
          Terminal.WriteLine($"  Already published: {string.Join(", ", alreadyPublished)}".Yellow());
        }

        Environment.ExitCode = 1;
      }
    }

    private static string? GetVersionFromSource()
    {
      string? repoRoot = Git.FindRoot();
      if (repoRoot is null)
      {
        return null;
      }

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

    private static bool TryParseStrategy(string input, out CheckVersionStrategy strategy)
    {
      return input switch
      {
        "git-tag" => SetResult(CheckVersionStrategy.GitTag, out strategy),
        "nuget-search" => SetResult(CheckVersionStrategy.NuGetSearch, out strategy),
        _ => Enum.TryParse(input, ignoreCase: true, out strategy)
      };
    }

    private static bool SetResult(CheckVersionStrategy value, out CheckVersionStrategy strategy)
    {
      strategy = value;
      return true;
    }
  }
}
