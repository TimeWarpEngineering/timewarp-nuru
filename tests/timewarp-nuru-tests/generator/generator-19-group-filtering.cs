#!/usr/bin/dotnet --
// ═══════════════════════════════════════════════════════════════════════════════
// GENERATOR TEST: Group Filtering (#421)
// ═══════════════════════════════════════════════════════════════════════════════
//
// PURPOSE: Verify the DiscoverEndpoints(typeof(...)) feature that filters endpoints
// by their group type. This enables subset publishing of CLI applications.
//
// HOW IT WORKS:
// 1. Define test group base classes with [NuruRouteGroup]
// 2. Define commands inheriting from those groups
// 3. Test filtering behavior with various scenarios
//
// REGRESSION TESTS FOR:
// - Filter by single type includes all descendants
// - Filter by multiple types uses OR logic
// - Parent prefix stripping behavior
// - Ungrouped endpoints excluded when filter active
// - No filter includes all endpoints
// - Non-existent type returns empty set
//
// ═══════════════════════════════════════════════════════════════════════════════

#pragma warning disable CA1819 // Properties should not return arrays
#pragma warning disable CA1725 // Parameter names should match base declaration
#pragma warning disable CA1849 // Call async methods when in async method
#pragma warning disable CA1062 // Validate arguments of public methods
#pragma warning disable RCS1248 // Use pattern matching to check for null

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.GroupFiltering
{
  /// <summary>
  /// Tests for group-based endpoint filtering (DiscoverEndpoints(typeof(...)) feature).
  /// </summary>
  [TestTag("Generator")]
  [TestTag("GroupFiltering")]
  public class GroupFilteringTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<GroupFilteringTests>();

    /// <summary>
    /// Test that filtering by a single group type includes all descendant commands.
    /// </summary>
    public static async Task FilterBySingleType_IncludesDescendants()
    {
      // Arrange - filter by TestKanbanGroup
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .DiscoverEndpoints(typeof(TestKanbanGroup))
        .Build();

      // Act - kanban commands should work
      int exitCode = await app.RunAsync(["kanban", "add", "Test Task"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Added: Test Task").ShouldBeTrue();

      // Act - kanban list should also work
      terminal.ClearOutput();
      exitCode = await app.RunAsync(["kanban", "list"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Kanban list").ShouldBeTrue();
    }

    /// <summary>
    /// Test that filtering by multiple group types includes commands from both groups.
    /// Uses OR logic - command matches if it inherits from ANY specified type.
    /// </summary>
    public static async Task FilterByMultipleTypes_IncludesBothGroups()
    {
      // Arrange - filter by both Kanban and Git groups
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .DiscoverEndpoints(typeof(TestKanbanGroup), typeof(TestGitGroup))
        .Build();

      // Act - kanban command should work
      int exitCode = await app.RunAsync(["kanban", "add", "Task"]);

      // Assert - kanban command works
      exitCode.ShouldBe(0);
      terminal.OutputContains("Added: Task").ShouldBeTrue();

      // Act - git command should also work
      terminal.ClearOutput();
      exitCode = await app.RunAsync(["git", "commit", "-m", "test message"]);

      // Assert - git command works
      exitCode.ShouldBe(0);
      terminal.OutputContains("Committed: test message").ShouldBeTrue();
    }

    /// <summary>
    /// Test that parent group prefixes are stripped when filtering.
    /// When filtering by TestKanbanGroup (inherits TestTopGroup with "testapp" prefix),
    /// the "testapp" prefix is removed, leaving just "kanban add".
    /// </summary>
    public static async Task FilterByType_StripsParentPrefix()
    {
      // Arrange
      using TestTerminal terminal = new();

      // Full version would be: "testapp kanban add" (no filter)
      // Filtered version should be: "kanban add"
      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .DiscoverEndpoints(typeof(TestKanbanGroup))
        .Build();

      // Act - without "testapp" prefix - should work because it's stripped
      int exitCode = await app.RunAsync(["kanban", "add", "Stripped Prefix"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Added: Stripped Prefix").ShouldBeTrue();

      // Act - verify the old full path does NOT work
      terminal.ClearOutput();
      exitCode = await app.RunAsync(["testapp", "kanban", "add", "Wont Work"]);

      // Assert - unknown command because prefix is stripped
      exitCode.ShouldBe(1);
    }

    /// <summary>
    /// Test that filtering by ROOT group strips the root prefix.
    /// When filtering by TestTopGroup (root with "testapp" prefix),
    /// the "testapp" prefix is removed, leaving just "kanban add".
    /// </summary>
    public static async Task FilterByRootType_StripsRootPrefix()
    {
      // Arrange
      using TestTerminal terminal = new();

      // Full version would be: "testapp kanban add" (no filter)
      // Filtered by ROOT should be: "kanban add" (root prefix stripped)
      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .DiscoverEndpoints(typeof(TestTopGroup))
        .Build();

      // Act - without "testapp" prefix - should work because root prefix is stripped
      int exitCode = await app.RunAsync(["kanban", "add", "Root Filter Test"]);

      // Assert
      exitCode.ShouldBe(0);
      terminal.OutputContains("Added: Root Filter Test").ShouldBeTrue();

      // Act - verify the old full path does NOT work
      terminal.ClearOutput();
      exitCode = await app.RunAsync(["testapp", "kanban", "add", "Wont Work"]);

      // Assert - unknown command because root prefix is stripped
      exitCode.ShouldBe(1);
    }

    /// <summary>
    /// Test that ungrouped commands are excluded when a filter is active.
    /// Commands without [NuruRouteGroup] base are only available when no filter is set.
    /// </summary>
    public static async Task FilterActive_ExcludesUngrouped()
    {
      // Arrange - filter active
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .DiscoverEndpoints(typeof(TestKanbanGroup))
        .Build();

      // Act - try to run ungrouped command
      int exitCode = await app.RunAsync(["ungrouped"]);

      // Assert - ungrouped command not available with filter active (exit code 1 = not found)
      exitCode.ShouldBe(1);
    }

    /// <summary>
    /// Test that without any filter, all endpoints are included.
    /// Includes both grouped and ungrouped commands with full prefixes.
    /// </summary>
    public static async Task NoFilter_IncludesAll()
    {
      // Arrange - no filter
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .DiscoverEndpoints()
        .Build();

      // Act - full path including "testapp" prefix
      int exitCode = await app.RunAsync(["testapp", "kanban", "add", "No Filter"]);

      // Assert - full path works
      exitCode.ShouldBe(0);
      terminal.OutputContains("Added: No Filter").ShouldBeTrue();

      // Act - ungrouped command should also work
      terminal.ClearOutput();
      exitCode = await app.RunAsync(["ungrouped"]);

      // Assert - ungrouped command available without filter
      exitCode.ShouldBe(0);
      terminal.OutputContains("Ungrouped command executed").ShouldBeTrue();

      // Act - git commands work too
      terminal.ClearOutput();
      exitCode = await app.RunAsync(["testapp", "git", "commit", "-m", "all"]); exitCode.ShouldBe(0);
      terminal.OutputContains("Committed: all").ShouldBeTrue();
    }

    /// <summary>
    /// Test that filtering by a non-existent type results in no commands.
    /// The CLI should be valid but empty (help shows nothing).
    /// </summary>
    public static async Task FilterByNonExistentType_ReturnsEmpty()
    {
      // Arrange - filter by type that doesn't match any endpoint
      using TestTerminal terminal = new();

      NuruApp app = NuruApp.CreateBuilder()
        .UseTerminal(terminal)
        .DiscoverEndpoints(typeof(NonExistentGroup))
        .Build();

      // Act - try any command
      int exitCode = await app.RunAsync(["testapp", "kanban", "add", "test"]);

      // Assert - no commands available (exit code 1 = not found)
      exitCode.ShouldBe(1);

      // Act - even ungrouped should fail
      exitCode = await app.RunAsync(["ungrouped"]);

      // Assert - still not found
      exitCode.ShouldBe(1);
    }
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// GROUP HIERARCHY DEFINITIONS
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Top-level group with "testapp" prefix.
/// All other test groups inherit from this.
/// </summary>
[NuruRouteGroup("testapp")]
public abstract class TestTopGroup;

/// <summary>
/// Kanban group inheriting from TestTopGroup.
/// Full prefix: "testapp kanban"
/// </summary>
[NuruRouteGroup("kanban")]
public abstract class TestKanbanGroup : TestTopGroup;

/// <summary>
/// Git group inheriting from TestTopGroup.
/// Full prefix: "testapp git"
/// </summary>
[NuruRouteGroup("git")]
public abstract class TestGitGroup : TestTopGroup;

/// <summary>
/// Non-existent group for testing empty filter results.
/// No commands inherit from this.
/// </summary>
[NuruRouteGroup("nonexistent")]
public abstract class NonExistentGroup;

// ═══════════════════════════════════════════════════════════════════════════════
// COMMAND DEFINITIONS - Kanban Commands
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Kanban add command - inherits from TestKanbanGroup.
/// Route: "testapp kanban add" (no filter) or "kanban add" (filtered)
/// </summary>
[NuruRoute("add", Description = "Add a kanban task")]
public sealed class KanbanAddCommand : TestKanbanGroup, ICommand<Unit>
{
  [Parameter(Description = "Task name")]
  public string Name { get; set; } = string.Empty;

  public sealed class Handler(ITerminal terminal) : ICommandHandler<KanbanAddCommand, Unit>
  {
    public ValueTask<Unit> Handle(KanbanAddCommand command, CancellationToken ct)
    {
      terminal.WriteLine($"Added: {command.Name}");
      return default;
    }
  }
}

/// <summary>
/// Kanban list command - inherits from TestKanbanGroup.
/// Route: "testapp kanban list" (no filter) or "kanban list" (filtered)
/// </summary>
[NuruRoute("list", Description = "List kanban tasks")]
public sealed class KanbanListCommand : TestKanbanGroup, ICommand<Unit>
{
  public sealed class Handler(ITerminal terminal) : ICommandHandler<KanbanListCommand, Unit>
  {
    public ValueTask<Unit> Handle(KanbanListCommand command, CancellationToken ct)
    {
      terminal.WriteLine("Kanban list");
      return default;
    }
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// COMMAND DEFINITIONS - Git Commands
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Git commit command - inherits from TestGitGroup.
/// Route: "testapp git commit" (no filter) or "git commit" (filtered)
/// </summary>
[NuruRoute("commit", Description = "Commit changes")]
public sealed class GitCommitCommand : TestGitGroup, ICommand<Unit>
{
  [Option("message", "m", Description = "Commit message")]
  public string Message { get; set; } = string.Empty;

  public sealed class Handler(ITerminal terminal) : ICommandHandler<GitCommitCommand, Unit>
  {
    public ValueTask<Unit> Handle(GitCommitCommand command, CancellationToken ct)
    {
      terminal.WriteLine($"Committed: {command.Message}");
      return default;
    }
  }
}

// ═══════════════════════════════════════════════════════════════════════════════
// COMMAND DEFINITIONS - Ungrouped Command
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Ungrouped command - no [NuruRouteGroup] base.
/// Only available when no filter is active.
/// Route: "ungrouped"
/// </summary>
[NuruRoute("ungrouped", Description = "Ungrouped test command")]
public sealed class UngroupedCommand : ICommand<Unit>
{
  public sealed class Handler(ITerminal terminal) : ICommandHandler<UngroupedCommand, Unit>
  {
    public ValueTask<Unit> Handle(UngroupedCommand command, CancellationToken ct)
    {
      terminal.WriteLine("Ungrouped command executed");
      return default;
    }
  }
}
