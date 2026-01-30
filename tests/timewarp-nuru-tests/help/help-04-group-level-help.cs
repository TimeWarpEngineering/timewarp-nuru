#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj
#pragma warning disable RCS1163 // Unused parameter - parameters must match route pattern names for binding

#region Purpose
// Tests group-level help support.
// Validates "group --help" shows help for all commands within that group.
// Covers: basic group help, short form (-h), and ensuring per-route help still works.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Help
{

[TestTag("Help")]
public class GroupLevelHelpTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<GroupLevelHelpTests>();

  /// <summary>
  /// Test: "worktree --help" should show all worktree commands.
  /// </summary>
  public static async Task Should_show_group_level_help()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithGroupPrefix("worktree")
        .Map("add {path}").WithHandler((string path) => "Added " + path).WithDescription("Add a new worktree").Done()
        .Map("list").WithHandler(() => "Listed").WithDescription("List all worktrees").Done()
        .Map("remove {path}").WithHandler((string path) => "Removed " + path).WithDescription("Remove a worktree").Done()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["worktree", "--help"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("worktree commands:").ShouldBeTrue("Should show group header");
    terminal.OutputContains("add").ShouldBeTrue("Should list add command");
    terminal.OutputContains("list").ShouldBeTrue("Should list list command");
    terminal.OutputContains("remove").ShouldBeTrue("Should list remove command");
    terminal.OutputContains("Add a new worktree").ShouldBeTrue("Should show add description");
    terminal.OutputContains("List all worktrees").ShouldBeTrue("Should show list description");
  }

  /// <summary>
  /// Test: "worktree -h" should also show group help (short form).
  /// </summary>
  public static async Task Should_show_group_level_help_with_short_form()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithGroupPrefix("config")
        .Map("set {key} {value}").WithHandler((string key, string value) => "Set").WithDescription("Set a config value").Done()
        .Map("get {key}").WithHandler((string key) => "Got").WithDescription("Get a config value").Done()
      .Done()
      .Build();

    // Act - using -h instead of --help
    int exitCode = await app.RunAsync(["config", "-h"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("config commands:").ShouldBeTrue("Should show group header");
    terminal.OutputContains("set").ShouldBeTrue("Should list set command");
    terminal.OutputContains("get").ShouldBeTrue("Should list get command");
  }

  /// <summary>
  /// Test: "worktree add --help" should still show per-route help, not group help.
  /// </summary>
  public static async Task Should_not_intercept_per_route_help()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithGroupPrefix("worktree")
        .Map("add {path} --force,-f?").WithHandler((string path, bool force) => "Added").WithDescription("Add a new worktree").Done()
        .Map("list").WithHandler(() => "Listed").WithDescription("List all worktrees").Done()
      .Done()
      .Build();

    // Act - request help for specific route, not the group
    int exitCode = await app.RunAsync(["worktree", "add", "--help"]);

    // Assert - should show route-specific help, not group help
    exitCode.ShouldBe(0);
    terminal.OutputContains("worktree commands:").ShouldBeFalse("Should NOT show group header");
    terminal.OutputContains("Add a new worktree").ShouldBeTrue("Should show route description");
    terminal.OutputContains("--force").ShouldBeTrue("Should show route options");
    terminal.OutputContains("path").ShouldBeTrue("Should show route parameters");
  }

  /// <summary>
  /// Test: Multi-word group prefix should work correctly.
  /// </summary>
  public static async Task Should_show_help_for_multi_word_group_prefix()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithGroupPrefix("remote origin")
        .Map("push").WithHandler(() => "Pushed").WithDescription("Push to remote origin").Done()
        .Map("pull").WithHandler(() => "Pulled").WithDescription("Pull from remote origin").Done()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["remote", "origin", "--help"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("remote origin commands:").ShouldBeTrue("Should show multi-word group header");
    terminal.OutputContains("push").ShouldBeTrue("Should list push command");
    terminal.OutputContains("pull").ShouldBeTrue("Should list pull command");
  }

  /// <summary>
  /// Test: Multiple groups should each have their own help.
  /// </summary>
  public static async Task Should_handle_multiple_groups()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithGroupPrefix("worktree")
        .Map("add {path}").WithHandler((string path) => "Added").WithDescription("Add worktree").Done()
        .Map("remove {path}").WithHandler((string path) => "Removed").WithDescription("Remove worktree").Done()
      .Done()
      .WithGroupPrefix("branch")
        .Map("create {name}").WithHandler((string name) => "Created").WithDescription("Create branch").Done()
        .Map("delete {name}").WithHandler((string name) => "Deleted").WithDescription("Delete branch").Done()
      .Done()
      .Build();

    // Act - get help for worktree group
    int worktreeExitCode = await app.RunAsync(["worktree", "--help"]);

    // Assert - worktree help should only show worktree commands
    worktreeExitCode.ShouldBe(0);
    terminal.OutputContains("worktree commands:").ShouldBeTrue("Should show worktree group header");
    terminal.OutputContains("Add worktree").ShouldBeTrue("Should show worktree commands");
    terminal.OutputContains("Create branch").ShouldBeFalse("Should NOT show branch commands in worktree help");
  }

  /// <summary>
  /// Test: Group help should not execute any handlers.
  /// </summary>
  public static async Task Should_not_execute_handlers_when_group_help_requested()
  {
    // Arrange
    GroupHelpHandlerFlags.HandlerExecuted = false;

    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithGroupPrefix("dangerous")
        .Map("destroy").WithHandler(GroupHelpHandlerFlags.DangerousHandler).WithDescription("Destroy everything").Done()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["dangerous", "--help"]);

    // Assert
    exitCode.ShouldBe(0);
    GroupHelpHandlerFlags.HandlerExecuted.ShouldBeFalse("Handler should NOT be executed when group help is requested");
  }
}

// Static class to hold handler flags (avoiding lambda closures)
public static class GroupHelpHandlerFlags
{
  public static bool HandlerExecuted { get; set; }

  public static string DangerousHandler()
  {
    HandlerExecuted = true;
    return "destroyed";
  }
}

} // namespace TimeWarp.Nuru.Tests.Help
