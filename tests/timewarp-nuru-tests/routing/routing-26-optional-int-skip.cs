#!/usr/bin/dotnet --

// ═══════════════════════════════════════════════════════════════════════════════
// REGRESSION TEST: GitHub Issue #152
// ═══════════════════════════════════════════════════════════════════════════════
//
// PURPOSE: Verify the source generator correctly handles optional int options
// in [NuruRoute] endpoints and skips to next route when not matched.
//
// BUG DESCRIPTION:
// When running "repo setup", the router incorrectly errors with:
//   "Error: Invalid value '(missing)' for option '--days'. Expected: int"
// instead of skipping the "workspace commits" route and matching "repo setup".
//
// ROOT CAUSE:
// The generated code checks `if (flagFound && raw is null) goto skip;`
// but when flagFound is FALSE and raw is NULL, it still tries to TryParse(null)
// which fails, causing an error instead of a route skip.
//
// ═══════════════════════════════════════════════════════════════════════════════

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing.Issue152
{

/// <summary>
/// Tests for GitHub issue #152: Source generator incorrectly errors on missing optional int options
/// instead of skipping route.
/// </summary>
[TestTag("Routing")]
[TestTag("Issue152")]
public class OptionalIntSkipTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<OptionalIntSkipTests>();

  /// <summary>
  /// Reproduces the bug from issue #152:
  /// When "repo setup" is called, the router should skip "workspace commits"
  /// (which has optional --days int option) and match "repo setup".
  /// </summary>
  public static async Task Should_skip_endpoint_route_with_optional_int_when_running_different_command()
  {
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    // This should match Issue152RepoSetupCommand, NOT error from Issue152WorkspaceCommitsCommand
    int exitCode = await app.RunAsync(["issue152-repo", "setup"]);

    Console.WriteLine($"Exit code: {exitCode}");
    Console.WriteLine($"Output: {terminal.AllOutput}");

    exitCode.ShouldBe(0);
    terminal.OutputContains("Setting up repo").ShouldBeTrue();
    terminal.OutputContains("Error").ShouldBeFalse($"Should not contain error, got: {terminal.AllOutput}");
  }

  /// <summary>
  /// Verifies that the workspace commits command works when --days is provided.
  /// </summary>
  public static async Task Should_match_endpoint_route_with_optional_int_when_option_provided()
  {
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    int exitCode = await app.RunAsync(["issue152-workspace", "commits", "--days", "7"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("Showing commits from last 7 days").ShouldBeTrue();
  }

  /// <summary>
  /// Verifies that workspace commits works without --days (uses default 0).
  /// </summary>
  public static async Task Should_use_default_when_optional_int_not_provided_on_matching_route()
  {
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    int exitCode = await app.RunAsync(["issue152-workspace", "commits"]);

    Console.WriteLine($"Exit code: {exitCode}");
    Console.WriteLine($"Output: {terminal.AllOutput}");

    exitCode.ShouldBe(0);
    terminal.OutputContains("Showing commits from last 0 days").ShouldBeTrue();
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Command definitions - These reproduce the ganda scenario
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Base class for workspace group routes.
/// </summary>
[NuruRouteGroup("issue152-workspace")]
public abstract class Issue152WorkspaceGroupBase;

/// <summary>
/// Simulates the WorkspaceCommitsCommand from ganda.
/// Has an optional --days int option.
/// </summary>
[NuruRoute("commits", Description = "Show commits across workspace")]
public sealed class Issue152WorkspaceCommitsCommand : Issue152WorkspaceGroupBase, ICommand<Unit>
{
  [Option("days", "d", Description = "Show commits from last N days")]
  public int Days { get; set; }  // No default = uses 0, but option is optional

  [Option("count", "n", Description = "Number of commits per repo")]
  public int Count { get; set; } = 3;

  public sealed class Handler(ITerminal terminal) : ICommandHandler<Issue152WorkspaceCommitsCommand, Unit>
  {
    public async ValueTask<Unit> Handle(Issue152WorkspaceCommitsCommand command, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(command);
      await terminal.WriteLineAsync($"Showing commits from last {command.Days} days").ConfigureAwait(false);
      await terminal.WriteLineAsync($"Count per repo: {command.Count}").ConfigureAwait(false);
      return default;
    }
  }
}

/// <summary>
/// Base class for repo group routes.
/// </summary>
[NuruRouteGroup("issue152-repo")]
public abstract class Issue152RepoGroupBase;

/// <summary>
/// Simulates the RepoSetupCommand from ganda.
/// This should match when running "repo setup" without getting blocked by WorkspaceCommitsCommand.
/// </summary>
[NuruRoute("setup", Description = "Configure repository")]
public sealed class Issue152RepoSetupCommand : Issue152RepoGroupBase, ICommand<Unit>
{
  [Option("dry-run", Description = "Preview changes")]
  public bool DryRun { get; set; }

  public sealed class Handler(ITerminal terminal) : ICommandHandler<Issue152RepoSetupCommand, Unit>
  {
    public async ValueTask<Unit> Handle(Issue152RepoSetupCommand command, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(command);
      await terminal.WriteLineAsync($"Setting up repo (dry-run: {command.DryRun})").ConfigureAwait(false);
      return default;
    }
  }
}

}
