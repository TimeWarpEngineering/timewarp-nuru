#!/usr/bin/dotnet --

// ═══════════════════════════════════════════════════════════════════════════════
// REGRESSION TEST: GitHub Issue #178
// ═══════════════════════════════════════════════════════════════════════════════
//
// PURPOSE: Verify [NuruRouteAlias] attribute propagates from group base classes
// to derived commands.
//
// BUG DESCRIPTION:
// When [NuruRouteAlias] is defined on a base class with [NuruRouteGroup],
// the alias does not work for commands that inherit from that group.
//
// Example:
//   [NuruRouteGroup("workspace")]
//   [NuruRouteAlias("ws")]
//   public abstract class WorkspaceGroupBase { }
//
//   [NuruRoute("info")]
//   public sealed class WorkspaceInfoCommand : WorkspaceGroupBase, ICommand<Unit> { }
//
// Expected: "workspace info" and "ws info" both work
// Actual (bug): only "workspace info" works, "ws info" returns "Unknown command"
//
// ═══════════════════════════════════════════════════════════════════════════════

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing.Issue178
{

/// <summary>
/// Tests for GitHub issue #178: NuruRouteAlias does not propagate from group base classes.
/// </summary>
[TestTag("Routing")]
[TestTag("Issue178")]
public class GroupAliasTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<GroupAliasTests>();

  // ═══════════════════════════════════════════════════════════════════════════════
  // Test 1: Direct alias on command class (should work if endpoint extractor extracts it)
  // ═══════════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Verifies [NuruRouteAlias] directly on command class works.
  /// Pattern: "goodbye" with aliases "bye" and "cya"
  /// </summary>
  public static async Task Should_match_command_with_direct_alias_bye()
  {
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    int exitCode = await app.RunAsync(["issue178-bye"]);

    Console.WriteLine($"Exit code: {exitCode}");
    Console.WriteLine($"Output: {terminal.AllOutput}");

    exitCode.ShouldBe(0);
    terminal.OutputContains("Goodbye!").ShouldBeTrue();
  }

  public static async Task Should_match_command_with_direct_alias_cya()
  {
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    int exitCode = await app.RunAsync(["issue178-cya"]);

    Console.WriteLine($"Exit code: {exitCode}");
    Console.WriteLine($"Output: {terminal.AllOutput}");

    exitCode.ShouldBe(0);
    terminal.OutputContains("Goodbye!").ShouldBeTrue();
  }

  // ═══════════════════════════════════════════════════════════════════════════════
  // Test 2: Alias on group base class - single level (the main bug scenario)
  // ═══════════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Verifies the primary route (without alias) works on a grouped command.
  /// Pattern: "issue178-workspace info"
  /// </summary>
  public static async Task Should_match_workspace_info_without_alias()
  {
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    int exitCode = await app.RunAsync(["issue178-workspace", "info"]);

    Console.WriteLine($"Exit code: {exitCode}");
    Console.WriteLine($"Output: {terminal.AllOutput}");

    exitCode.ShouldBe(0);
    terminal.OutputContains("Workspace info").ShouldBeTrue();
  }

  /// <summary>
  /// Verifies [NuruRouteAlias] on group base class propagates to commands.
  /// Pattern: "ws info" should match "issue178-workspace info"
  /// This is the main bug scenario from issue #178.
  /// </summary>
  public static async Task Should_match_workspace_info_with_group_alias()
  {
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    int exitCode = await app.RunAsync(["issue178-ws", "info"]);

    Console.WriteLine($"Exit code: {exitCode}");
    Console.WriteLine($"Output: {terminal.AllOutput}");

    exitCode.ShouldBe(0);
    terminal.OutputContains("Workspace info").ShouldBeTrue();
  }

  /// <summary>
  /// Verifies second alias "work" also works.
  /// Pattern: "work info" should match "issue178-workspace info"
  /// </summary>
  public static async Task Should_match_workspace_info_with_second_group_alias()
  {
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    int exitCode = await app.RunAsync(["issue178-work", "info"]);

    Console.WriteLine($"Exit code: {exitCode}");
    Console.WriteLine($"Output: {terminal.AllOutput}");

    exitCode.ShouldBe(0);
    terminal.OutputContains("Workspace info").ShouldBeTrue();
  }

  // ═══════════════════════════════════════════════════════════════════════════════
  // Test 3: Alias on nested group base class
  // ═══════════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Verifies the primary route (without alias) works on a nested grouped command.
  /// Pattern: "issue178-workspace repo info"
  /// </summary>
  public static async Task Should_match_nested_workspace_repo_info_without_alias()
  {
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    int exitCode = await app.RunAsync(["issue178-workspace", "repo", "info"]);

    Console.WriteLine($"Exit code: {exitCode}");
    Console.WriteLine($"Output: {terminal.AllOutput}");

    exitCode.ShouldBe(0);
    terminal.OutputContains("Repo info").ShouldBeTrue();
  }

  /// <summary>
  /// Verifies [NuruRouteAlias] on parent group propagates through nested groups.
  /// Pattern: "ws repo info" should match "issue178-workspace repo info"
  /// </summary>
  public static async Task Should_match_nested_workspace_repo_info_with_parent_alias()
  {
    using TestTerminal terminal = new();

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    int exitCode = await app.RunAsync(["issue178-ws", "repo", "info"]);

    Console.WriteLine($"Exit code: {exitCode}");
    Console.WriteLine($"Output: {terminal.AllOutput}");

    exitCode.ShouldBe(0);
    terminal.OutputContains("Repo info").ShouldBeTrue();
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Endpoints for Test 1: Direct alias on command class
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Command with direct alias - no group inheritance involved.
/// </summary>
[NuruRoute("issue178-goodbye", Description = "Say goodbye")]
[NuruRouteAlias("issue178-bye", "issue178-cya")]
public sealed class Issue178GoodbyeCommand : ICommand<Unit>
{
  public sealed class Handler(ITerminal terminal) : ICommandHandler<Issue178GoodbyeCommand, Unit>
  {
    public async ValueTask<Unit> Handle(Issue178GoodbyeCommand command, CancellationToken cancellationToken)
    {
      await terminal.WriteLineAsync("Goodbye!").ConfigureAwait(false);
      return default;
    }
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Endpoints for Test 2: Alias on group base class - single level
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Group base class with alias.
/// Commands inheriting from this should be accessible via "issue178-workspace" or "issue178-ws" or "issue178-work".
/// </summary>
[NuruRouteGroup("issue178-workspace")]
[NuruRouteAlias("issue178-ws", "issue178-work")]
public abstract class Issue178WorkspaceGroup;

/// <summary>
/// Simple command in the workspace group.
/// </summary>
[NuruRoute("info", Description = "Show workspace info")]
public sealed class Issue178WorkspaceInfoCommand : Issue178WorkspaceGroup, ICommand<Unit>
{
  public sealed class Handler(ITerminal terminal) : ICommandHandler<Issue178WorkspaceInfoCommand, Unit>
  {
    public async ValueTask<Unit> Handle(Issue178WorkspaceInfoCommand command, CancellationToken cancellationToken)
    {
      await terminal.WriteLineAsync("Workspace info").ConfigureAwait(false);
      return default;
    }
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Endpoints for Test 3: Alias on nested group base class
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Repo group nested under workspace group.
/// </summary>
[NuruRouteGroup("repo")]
public abstract class Issue178RepoGroup : Issue178WorkspaceGroup;

/// <summary>
/// Nested command: workspace repo info should be accessible via ws repo info.
/// </summary>
[NuruRoute("info", Description = "Show repo info")]
public sealed class Issue178RepoInfoCommand : Issue178RepoGroup, ICommand<Unit>
{
  public sealed class Handler(ITerminal terminal) : ICommandHandler<Issue178RepoInfoCommand, Unit>
  {
    public async ValueTask<Unit> Handle(Issue178RepoInfoCommand command, CancellationToken cancellationToken)
    {
      await terminal.WriteLineAsync("Repo info").ConfigureAwait(false);
      return default;
    }
  }
}

}
