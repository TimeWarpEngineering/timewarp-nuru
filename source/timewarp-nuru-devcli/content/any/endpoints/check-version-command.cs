#region Purpose
// Verify that the version in Directory.Build.props has not already been released
#endregion
#region Design
// Uses IRepoCheckVersionService from TimeWarp.Amuru for version checking.
// Uses IRepoConfigService for per-repo config defaults.
// Supports two strategies: nuget-search (checks NuGet) and git-tag (compares to git tag).
// Strategy defaults to per-repo config (.timewarp/dev.jsonc), then git-tag.
#endregion

namespace DevCli;

using TimeWarp.Amuru;
using TimeWarp.Nuru;
using TimeWarp.Terminal;

/// <summary>
/// Verify version is ready to release.
/// </summary>
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
    private readonly IRepoCheckVersionService CheckVersionService;
    private readonly IRepoConfigService ConfigService;

    public Handler
    (
      ITerminal terminal,
      IRepoCheckVersionService checkVersionService,
      IRepoConfigService configService
    )
    {
      Terminal = terminal;
      CheckVersionService = checkVersionService;
      ConfigService = configService;
    }

    public async ValueTask<Unit> Handle(CheckVersionCommand command, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(command);

      // Resolve strategy: CLI flag > config > default
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

      // Display strategy
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
      GitTagCheckResult result = await CheckVersionService
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

      NuGetCheckResult result = await CheckVersionService
        .CheckNuGetVersionAsync(packages, cancellationToken)
        .ConfigureAwait(false);

      Terminal.WriteLine($"Version in source: {result.Version}".Cyan());
      string latestNuGet = result.LatestNuGetVersion ?? "(none)";
      Terminal.WriteLine($"Latest NuGet version: {latestNuGet}".Cyan());

      if (result.CheckedPackages is { Count: > 0 })
      {
        Terminal.WriteLine($"Packages checked: {string.Join(", ", result.CheckedPackages)}");
      }

      Terminal.WriteLine("");

      if (result.IsNewVersion)
      {
        Terminal.WriteLine("✓ Version in source is new — safe to release.".Green());
      }
      else
      {
        Terminal.WriteLine($"✗ Version {result.Version} was already released.".Red());
        Terminal.WriteLine("  Bump the version before releasing.".Yellow());

        if (result.AlreadyPublishedPackages is { Count: > 0 })
        {
          Terminal.WriteLine($"  Already published: {string.Join(", ", result.AlreadyPublishedPackages)}".Yellow());
        }

        Environment.ExitCode = 1;
      }
    }

    /// <summary>
    /// Parses a strategy string from CLI input into a <see cref="CheckVersionStrategy"/>.
    /// Accepts hyphenated form ("git-tag", "nuget-search") or PascalCase form ("GitTag", "NuGetSearch").
    /// </summary>
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
