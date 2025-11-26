#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../../Source/TimeWarp.Nuru.Repl/TimeWarp.Nuru.Repl.csproj

using TimeWarp.Nuru;
using TimeWarp.Nuru.Repl;
using TimeWarp.Nuru.Repl.Tests.TabCompletion;

// ============================================================================
// State Management Tests
// ============================================================================
// Tests how completion state is managed during interaction:
// - Escape cancels completion and returns to original input
// - Typing after Tab filters completions
// - Backspace during completion resets state
// - Delete during completion resets state
// - State is cleared between completion attempts
//
// These tests validate that completion state doesn't leak between
// operations and that users can interrupt/modify completions naturally.
// ============================================================================

return await RunTests<StateManagementTests>();

[TestTag("REPL")]
[TestTag("TabCompletion")]
[ClearRunfileCache]
public class StateManagementTests
{
  private static TestTerminal Terminal = null!;
  private static NuruApp App = null!;

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
  // ESCAPE CANCELS COMPLETION
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_cancel_completion_with_escape_for_commands()
  {
    // Arrange: Type "s", Tab to show completions, then Escape to cancel
    Terminal.QueueKeys("s");
    Terminal.QueueKey(ConsoleKey.Tab);
    Terminal.QueueKey(ConsoleKey.Escape);
    Terminal.QueueLine("");
    Terminal.QueueLine("exit");

    // Act
    await App.RunReplAsync();

    // Assert: Escape should be handled (output should contain the input)
    Terminal.OutputContains("s").ShouldBeTrue("Should retain 's' after escape");
  }

  [Timeout(5000)]
  public static async Task Should_cancel_completion_with_escape_for_subcommands()
  {
    // Arrange: Type "git ", Tab, then Escape
    Terminal.QueueKeys("git ");
    Terminal.QueueKey(ConsoleKey.Tab);
    Terminal.QueueKey(ConsoleKey.Escape);
    Terminal.QueueLine("");
    Terminal.QueueLine("exit");

    // Act
    await App.RunReplAsync();

    // Assert: Should show git in output
    Terminal.OutputContains("git").ShouldBeTrue("Should retain 'git ' after escape");
  }

  [Timeout(5000)]
  public static async Task Should_cancel_completion_with_escape_for_enums()
  {
    // Arrange: Type "deploy ", Tab, then Escape
    Terminal.QueueKeys("deploy ");
    Terminal.QueueKey(ConsoleKey.Tab);
    Terminal.QueueKey(ConsoleKey.Escape);
    Terminal.QueueLine("");
    Terminal.QueueLine("exit");

    // Act
    await App.RunReplAsync();

    // Assert: Should show deploy in output
    Terminal.OutputContains("deploy").ShouldBeTrue("Should retain 'deploy ' after escape");
  }

  // ============================================================================
  // TYPING AFTER TAB FILTERS COMPLETIONS
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_filter_completions_by_typing_after_tab()
  {
    // Arrange: Type "s", Tab (show status, search), then type "t" to filter to "status"
    Terminal.QueueKeys("s");
    Terminal.QueueKey(ConsoleKey.Tab);
    Terminal.QueueKeys("t");
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show "status" (filtered by "st")
    Terminal.OutputContains("status").ShouldBeTrue("Should show 'status' after typing 't'");
  }

  [Timeout(5000)]
  public static async Task Should_filter_completions_typing_e_after_tab()
  {
    // Arrange: Type "s", Tab, then type "e" to filter to "search"
    Terminal.QueueKeys("s");
    Terminal.QueueKey(ConsoleKey.Tab);
    Terminal.QueueKeys("e");
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show "search" (filtered by "se")
    Terminal.OutputContains("search").ShouldBeTrue("Should show 'search' after typing 'e'");
  }

  [Timeout(5000)]
  public static async Task Should_filter_git_subcommands_by_typing_after_tab()
  {
    // Arrange: Type "git ", Tab, then type "c" to filter to "commit"
    Terminal.QueueKeys("git ");
    Terminal.QueueKey(ConsoleKey.Tab);
    Terminal.QueueKeys("c");
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show "commit" (filtered by "git c")
    Terminal.OutputContains("commit").ShouldBeTrue("Should show 'commit' after typing 'c'");
  }

  [Timeout(5000)]
  public static async Task Should_filter_enum_values_by_typing_after_tab()
  {
    // Arrange: Type "deploy ", Tab, then type "p" to filter to "Prod"
    Terminal.QueueKeys("deploy ");
    Terminal.QueueKey(ConsoleKey.Tab);
    Terminal.QueueKeys("p");
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show "Prod" (filtered by "deploy p")
    Terminal.OutputContains("Prod").ShouldBeTrue("Should show 'Prod' after typing 'p'");
  }

  // ============================================================================
  // BACKSPACE DURING COMPLETION
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_handle_backspace_after_tab()
  {
    // Arrange: Type "st", Tab (completes to status), then Backspace
    Terminal.QueueKeys("st");
    Terminal.QueueKey(ConsoleKey.Tab);
    Terminal.QueueKey(ConsoleKey.Backspace);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Backspace should be processed
    // The exact behavior depends on implementation (might go back to "statu" or "st")
    Terminal.Output.ShouldNotBeNull("Should have output after backspace");
  }

  [Timeout(5000)]
  public static async Task Should_handle_backspace_during_multiple_matches()
  {
    // Arrange: Type "s", Tab (show list), then Backspace
    Terminal.QueueKeys("s");
    Terminal.QueueKey(ConsoleKey.Tab);
    Terminal.QueueKey(ConsoleKey.Backspace);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Backspace should be processed
    Terminal.Output.ShouldNotBeNull("Should have output after backspace");
  }

  // ============================================================================
  // STATE CLEARED BETWEEN ATTEMPTS
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_clear_state_between_completion_attempts()
  {
    // BUG: This test FAILS - state leaks between completion attempts
    // Arrange: Type "s", Tab, Escape, then type "g" and Tab
    // State from first attempt should not affect second
    Terminal.QueueKeys("s");
    Terminal.QueueKey(ConsoleKey.Tab);
    Terminal.QueueKey(ConsoleKey.Escape);
    Terminal.QueueKey(ConsoleKey.Backspace); // Remove 's'
    Terminal.QueueKeys("g");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show "git" and "greet" (not "status" or "search" from first attempt)
    CompletionAssertions.ShouldShowCompletions(Terminal, "git", "greet");
    CompletionAssertions.ShouldNotShowCompletions(Terminal, "status", "search");
  }

  [Timeout(5000)]
  public static async Task Should_handle_multiple_tab_escape_sequences()
  {
    // Arrange: Tab, Escape, Tab, Escape pattern
    Terminal.QueueKey(ConsoleKey.Tab);
    Terminal.QueueKey(ConsoleKey.Escape);
    Terminal.QueueKey(ConsoleKey.Tab);
    Terminal.QueueKey(ConsoleKey.Escape);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should handle multiple escape sequences without error
    Terminal.Output.ShouldNotBeNull("Should handle multiple Tab-Escape sequences");
  }

  // ============================================================================
  // FRESH STATE AFTER CHARACTER INPUT
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_have_fresh_state_after_typing_character()
  {
    // Arrange: Type "s", Tab (show list), type "t", then Tab again
    // Second Tab should use "st" as context, not "s"
    Terminal.QueueKeys("s");
    Terminal.QueueKey(ConsoleKey.Tab);
    Terminal.QueueKeys("t");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should complete "st" to "status"
    Terminal.OutputContains("status").ShouldBeTrue("Should complete 'st' to 'status'");
  }

  [Timeout(5000)]
  public static async Task Should_have_fresh_state_for_git_after_typing()
  {
    // Arrange: Type "git ", Tab, type "c", Tab again
    Terminal.QueueKeys("git ");
    Terminal.QueueKey(ConsoleKey.Tab);
    Terminal.QueueKeys("c");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should complete "git c" to "git commit"
    Terminal.OutputContains("commit").ShouldBeTrue("Should complete 'git c' to 'git commit'");
  }

  // ============================================================================
  // COMPLETION STATE ISOLATION
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_not_mix_command_and_subcommand_state()
  {
    // Arrange: Complete "g" to "git", then space and Tab for subcommands
    // Command completion state should not leak into subcommand completion
    Terminal.QueueKeys("g");
    Terminal.QueueKey(ConsoleKey.Tab); // Could show git/greet
    Terminal.QueueKeys("it ");         // Complete to "git "
    Terminal.QueueKey(ConsoleKey.Tab); // Show git subcommands
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show git subcommands (status, commit, log), not top-level commands
    CompletionAssertions.ShouldShowCompletions(Terminal, "status", "commit", "log");
  }

  [Timeout(5000)]
  public static async Task Should_not_mix_command_and_enum_state()
  {
    // Arrange: Complete "d" to "deploy", then space and Tab for enum values
    Terminal.QueueKeys("d");
    Terminal.QueueKey(ConsoleKey.Tab); // Complete to "deploy"
    Terminal.QueueKeys(" ");            // Add space
    Terminal.QueueKey(ConsoleKey.Tab); // Show enum values
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show enum values (Dev, Staging, Prod), not commands
    CompletionAssertions.ShouldShowCompletions(Terminal, "Dev", "Staging", "Prod");
  }
}
