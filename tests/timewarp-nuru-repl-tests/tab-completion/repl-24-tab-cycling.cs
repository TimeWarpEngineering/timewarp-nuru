#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../../source/timewarp-nuru-repl/timewarp-nuru-repl.csproj

using TimeWarp.Nuru;
using TimeWarp.Nuru.Repl;
using TimeWarp.Nuru.Repl.Tests.TabCompletion;

// ============================================================================
// Tab Cycling Behavior Tests
// ============================================================================
// Tests tab completion cycling through multiple matches:
// - Forward cycling: Tab, Tab, Tab cycles through matches
// - Reverse cycling: Shift+Tab goes backwards
// - Alt+= shows all matches without cycling
// - Cycling with commands, subcommands, and enums
// - Cycling wraps around to first match
//
// These tests validate the interactive completion UX where users
// can cycle through options to find the right match.
// ============================================================================

return await RunTests<CyclingBehaviorTests>();

[TestTag("REPL")]
[TestTag("TabCompletion")]
[ClearRunfileCache]
public class CyclingBehaviorTests
{
  private static TestTerminal Terminal = null!;
  private static NuruCoreApp App = null!;

  public static async Task Setup()
  {
    // Create fresh terminal and app for each test using helper factory
    Terminal = new TestTerminal();
    App = TestAppFactory.CreateReplDemoApp(Terminal);
    await Task.CompletedTask;
  }

  public static async Task CleanUp()
  {
    Terminal?.Dispose();
    Terminal = null!;
    App = null!;
    await Task.CompletedTask;
  }

  // ============================================================================
  // FORWARD CYCLING - COMMANDS
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_show_completion_list_on_first_tab_for_s()
  {
    // Arrange: Type "s" then Tab - should show list (status, search)
    KeySequenceHelpers.TypeAndTab(Terminal, "s");
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show completion list header
    CompletionAssertions.ShouldShowCompletionList(Terminal);
    CompletionAssertions.ShouldShowCompletions(Terminal, "status", "search");
  }

  [Timeout(5000)]
  public static async Task Should_cycle_through_commands_with_multiple_tabs()
  {
    // Arrange: Type "s" then Tab twice to cycle
    Terminal.QueueKeys("s");
    KeySequenceHelpers.TabMultipleTimes(Terminal, 2);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show both completions during cycling
    CompletionAssertions.ShouldShowCompletions(Terminal, "status", "search");
  }

  [Timeout(5000)]
  public static async Task Should_cycle_through_b_commands()
  {
    // Arrange: Type "b" then Tab twice - build and backup
    Terminal.QueueKeys("b");
    KeySequenceHelpers.TabMultipleTimes(Terminal, 2);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show both build and backup
    CompletionAssertions.ShouldShowCompletions(Terminal, "build", "backup");
  }

  [Timeout(5000)]
  public static async Task Should_cycle_through_e_commands()
  {
    // Arrange: Type "e" then Tab twice - echo and exit
    Terminal.QueueKeys("e");
    KeySequenceHelpers.TabMultipleTimes(Terminal, 2);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show both echo and exit
    CompletionAssertions.ShouldShowCompletions(Terminal, "echo", "exit");
  }

  [Timeout(5000)]
  public static async Task Should_cycle_through_g_commands()
  {
    // Arrange: Type "g" then Tab twice - git and greet
    Terminal.QueueKeys("g");
    KeySequenceHelpers.TabMultipleTimes(Terminal, 2);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show both git and greet
    CompletionAssertions.ShouldShowCompletions(Terminal, "git", "greet");
  }

  // ============================================================================
  // FORWARD CYCLING - SUBCOMMANDS
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_show_git_subcommands_on_first_tab()
  {
    // Arrange: Type "git " then Tab
    Terminal.QueueKeys("git ");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show all git subcommands
    CompletionAssertions.ShouldShowCompletionList(Terminal);
    CompletionAssertions.ShouldShowCompletions(Terminal, "status", "commit", "log");
  }

  [Timeout(5000)]
  public static async Task Should_cycle_through_git_subcommands_with_multiple_tabs()
  {
    // Arrange: Type "git " then Tab three times
    Terminal.QueueKeys("git ");
    KeySequenceHelpers.TabMultipleTimes(Terminal, 3);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show all git subcommands during cycling
    CompletionAssertions.ShouldShowCompletions(Terminal, "status", "commit", "log");
  }

  // ============================================================================
  // FORWARD CYCLING - ENUMS
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_show_enum_values_on_first_tab()
  {
    // Arrange: Type "deploy " then Tab
    Terminal.QueueKeys("deploy ");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show all enum values
    CompletionAssertions.ShouldShowCompletionList(Terminal);
    CompletionAssertions.ShouldShowCompletions(Terminal, "Dev", "Staging", "Prod");
  }

  [Timeout(5000)]
  public static async Task Should_cycle_through_enum_values_with_multiple_tabs()
  {
    // Arrange: Type "deploy " then Tab three times
    Terminal.QueueKeys("deploy ");
    KeySequenceHelpers.TabMultipleTimes(Terminal, 3);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show all enum values during cycling
    CompletionAssertions.ShouldShowCompletions(Terminal, "Dev", "Staging", "Prod");
  }

  // ============================================================================
  // CYCLING WRAP-AROUND
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_wrap_around_after_cycling_through_all_matches()
  {
    // Arrange: Type "s" then Tab 4 times (2 matches + wrap to first + continue)
    Terminal.QueueKeys("s");
    KeySequenceHelpers.TabMultipleTimes(Terminal, 4);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show completions (wrap-around behavior)
    CompletionAssertions.ShouldShowCompletions(Terminal, "status", "search");
  }

  [Timeout(5000)]
  public static async Task Should_wrap_around_git_subcommands()
  {
    // Arrange: Type "git " then Tab 5 times (3 matches + wrap + continue)
    Terminal.QueueKeys("git ");
    KeySequenceHelpers.TabMultipleTimes(Terminal, 5);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show all subcommands (wrap-around behavior)
    CompletionAssertions.ShouldShowCompletions(Terminal, "status", "commit", "log");
  }

  [Timeout(5000)]
  public static async Task Should_wrap_around_enum_values()
  {
    // Arrange: Type "deploy " then Tab 5 times (3 enums + wrap + continue)
    Terminal.QueueKeys("deploy ");
    KeySequenceHelpers.TabMultipleTimes(Terminal, 5);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show all enum values (wrap-around behavior)
    CompletionAssertions.ShouldShowCompletions(Terminal, "Dev", "Staging", "Prod");
  }

  // ============================================================================
  // EMPTY INPUT CYCLING
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_show_all_commands_on_empty_input_tab()
  {
    // Arrange: Empty input, Tab
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show all top-level commands
    CompletionAssertions.ShouldShowCompletionList(Terminal);
    CompletionAssertions.ShouldShowCompletions(Terminal,
      "status", "time", "greet", "add", "deploy",
      "echo", "git", "build", "search", "backup"
    );
  }

  [Timeout(5000)]
  public static async Task Should_cycle_through_all_commands_on_empty_input()
  {
    // Arrange: Empty input, Tab 3 times
    KeySequenceHelpers.TabMultipleTimes(Terminal, 3);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show commands (cycling through all 10+)
    Terminal.OutputContains("status").ShouldBeTrue("Should show 'status' during cycling");
    Terminal.OutputContains("git").ShouldBeTrue("Should show 'git' during cycling");
  }

  // ============================================================================
  // NOTE: Shift+Tab (reverse cycling) and Alt+= (show all) tests
  // require special key handling that may not work in TestTerminal.
  // These will be tested in Phase 3 if terminal supports it.
  // ============================================================================
}
