#region Purpose
// Verify that the version in Directory.Build.props has not already been released
#endregion
#region Design
// Uses IRepoCheckVersionService from TimeWarp.Amuru for version checking.
// Supports two strategies: nuget-search (checks NuGet) and git-tag (compares to git tag).
// Strategy defaults to per-repo config, then git-tag.
// CA1849 suppressed: synchronous terminal methods are acceptable in CLI context.
#endregion

namespace DevCli.Endpoints;

using TimeWarp.Amuru;
using TimeWarp.Nuru;
using TimeWarp.Terminal;

#pragma warning disable CA1849 // Consider using an async method overload

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

      string? repoRoot = Git.FindRoot();
      if (repoRoot is null)
      {
        Terminal.WriteErrorLine("Error: not in a git repository.");
        Environment.ExitCode = 1;
        return Unit.Value;
      }

      // Get per-repo config defaults
      RepoConfig config = await ConfigService
        .GetConfigAsync(cancellationToken)
        .ConfigureAwait(false);

      string effectiveStrategy = command.Strategy ?? config.CheckVersion?.Strategy ?? "git-tag";
      string? effectivePackage = command.Package ?? config.CheckVersion?.Packages;

      CheckVersionResult result = await CheckVersionService
        .CheckAsync
        (
          effectiveStrategy,
          effectivePackage ?? string.Empty,
          command.Tag ?? string.Empty,
          cancellationToken
        )
        .ConfigureAwait(false);

      Terminal.WriteLine($"Version: {result.Version}".Cyan());
      Terminal.WriteLine($"Tag: {result.ResolvedTag}".Cyan());

      if (result.IsNewVersion)
      {
        Terminal.WriteLine("\n✓ Version is new — safe to release.".Green());
      }
      else
      {
        Terminal.WriteLine
        (
          $"\n✗ Version {result.Version} was already released.".Red()
        );
        Terminal.WriteLine("  Bump the version before releasing.");

        if (result.AlreadyPublishedPackages is { Count: > 0 })
        {
          Terminal.WriteLine
          (
            $"  Already published: {string.Join(", ", result.AlreadyPublishedPackages)}".Yellow()
          );
        }

        Environment.ExitCode = 1;
      }

      return Unit.Value;
    }
  }
}

#pragma warning restore CA1849
