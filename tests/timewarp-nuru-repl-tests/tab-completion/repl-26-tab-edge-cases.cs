#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../../source/timewarp-nuru-repl/timewarp-nuru-repl.csproj

using TimeWarp.Nuru;
using TimeWarp.Nuru.Tests.TabCompletion;

// ============================================================================
// Edge Cases Tests
// ============================================================================
// Tests unusual, boundary, and potentially problematic inputs:
// - Multiple consecutive spaces
// - Invalid contexts (too many arguments)
// - Mixed case variations
// - Special characters
// - Very long input
// - Repeated Tab presses
// - Completion after errors
//
// These tests validate that completion handles edge cases gracefully
// without crashing or producing unexpected behavior.
// ============================================================================

return await RunTests<EdgeCasesTests>();

[TestTag("REPL")]
[TestTag("TabCompletion")]
[ClearRunfileCache]
public class EdgeCasesTests
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
  // MULTIPLE SPACES
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_handle_double_space_before_tab()
  {
    // Arrange: Type "status  " (double space) then Tab
    Terminal.QueueKeys("status  ");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should handle gracefully without crash
    Terminal.Output.ShouldNotBeNull("Should handle double space gracefully");
  }

  [Timeout(5000)]
  public static async Task Should_handle_multiple_spaces_in_git_command()
  {
    // Arrange: Type "git   " (triple space) then Tab
    Terminal.QueueKeys("git   ");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should handle gracefully (might still show subcommands)
    Terminal.Output.ShouldNotBeNull("Should handle multiple spaces gracefully");
  }

  [Timeout(5000)]
  public static async Task Should_handle_spaces_in_middle_of_command()
  {
    // Arrange: Type "st atus" (space in middle) then Tab
    Terminal.QueueKeys("st atus");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should handle gracefully
    Terminal.Output.ShouldNotBeNull("Should handle space in middle of command");
  }

  // ============================================================================
  // INVALID CONTEXTS - TOO MANY ARGUMENTS
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_handle_too_many_arguments_for_status()
  {
    // Arrange: Type "status extra arg" then Tab
    // status takes no arguments, so "extra" is invalid
    Terminal.QueueKeys("status extra ");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should handle invalid context without crash
    Terminal.Output.ShouldNotBeNull("Should handle too many arguments gracefully");
  }

  [Timeout(5000)]
  public static async Task Should_handle_too_many_arguments_for_greet()
  {
    // Arrange: Type "greet Alice Bob" then Tab
    // greet takes one argument, so "Bob" is extra
    Terminal.QueueKeys("greet Alice Bob ");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should handle invalid context without crash
    Terminal.Output.ShouldNotBeNull("Should handle extra arguments gracefully");
  }

  [Timeout(5000)]
  public static async Task Should_handle_too_many_arguments_for_git_status()
  {
    // Arrange: Type "git status extra" then Tab
    // git status takes no arguments
    Terminal.QueueKeys("git status extra ");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should handle gracefully
    Terminal.Output.ShouldNotBeNull("Should handle extra git status arguments");
  }

  // ============================================================================
  // CASE VARIATIONS
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_handle_all_uppercase_STATUS()
  {
    // Arrange: Type "STATUS" then Tab
    Terminal.QueueKeys("STATUS");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should match case-insensitively
    Terminal.OutputContains("status").ShouldBeTrue("Should match 'STATUS' to 'status'");
  }

  [Timeout(5000)]
  public static async Task Should_handle_all_uppercase_GIT()
  {
    // Arrange: Type "GIT " then Tab
    Terminal.QueueKeys("GIT ");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show git subcommands
    Terminal.OutputContains("status").ShouldBeTrue("Should show git subcommands for 'GIT'");
  }

  [Timeout(5000)]
  public static async Task Should_handle_mixed_case_DePloy()
  {
    // Arrange: Type "DePloy " then Tab
    Terminal.QueueKeys("DePloy ");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show enum values
    Terminal.OutputContains("Dev").ShouldBeTrue("Should show enum values for 'DePloy'");
  }

  [Timeout(5000)]
  public static async Task Should_handle_camelCase_searchQuery()
  {
    // Arrange: Type "sEaRcH foo" then Tab
    Terminal.QueueKeys("sEaRcH foo ");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should handle mixed case
    Terminal.Output.ShouldNotBeNull("Should handle 'sEaRcH' mixed case");
  }

  // ============================================================================
  // EMPTY AND WHITESPACE
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_handle_tab_on_empty_input()
  {
    // Arrange: Just Tab (no input)
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show all commands
    CompletionAssertions.ShouldShowCompletionList(Terminal);
  }

  [Timeout(5000)]
  public static async Task Should_handle_tab_on_spaces_only()
  {
    // Arrange: Type "   " (only spaces) then Tab
    Terminal.QueueKeys("   ");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should handle gracefully
    Terminal.Output.ShouldNotBeNull("Should handle spaces-only input");
  }

  // ============================================================================
  // REPEATED TAB PRESSES
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_handle_many_tab_presses()
  {
    // Arrange: Press Tab 20 times on empty input
    for (int i = 0; i < 20; i++)
    {
      Terminal.QueueKey(ConsoleKey.Tab);
    }

    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should handle many tabs without crash or hang
    Terminal.Output.ShouldNotBeNull("Should handle 20 Tab presses gracefully");
  }

  [Timeout(5000)]
  public static async Task Should_handle_many_tabs_with_input()
  {
    // Arrange: Type "s" then press Tab 10 times
    Terminal.QueueKeys("s");
    for (int i = 0; i < 10; i++)
    {
      Terminal.QueueKey(ConsoleKey.Tab);
    }

    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should cycle through without crash
    Terminal.Output.ShouldNotBeNull("Should handle 10 Tab presses on 's'");
  }

  // ============================================================================
  // SPECIAL CHARACTERS (if applicable)
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_handle_dash_at_start()
  {
    // Arrange: Type "-" then Tab
    Terminal.QueueKeys("-");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should handle gracefully (no commands start with -)
    Terminal.Output.ShouldNotBeNull("Should handle '-' at start");
  }

  [Timeout(5000)]
  public static async Task Should_handle_double_dash_at_start()
  {
    // Arrange: Type "--" then Tab
    Terminal.QueueKeys("--");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should handle gracefully
    Terminal.Output.ShouldNotBeNull("Should handle '--' at start");
  }

  // ============================================================================
  // LONG INPUT
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_handle_very_long_command()
  {
    // Arrange: Type very long string then Tab
    string longInput = new('a', 200);
    Terminal.QueueKeys(longInput);
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should handle long input without crash
    Terminal.Output.ShouldNotBeNull("Should handle 200 character input");
  }

  [Timeout(5000)]
  public static async Task Should_handle_very_long_partial_command()
  {
    // Arrange: Type "status" followed by many characters
    Terminal.QueueKeys("status" + new string('x', 100));
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should handle gracefully
    Terminal.Output.ShouldNotBeNull("Should handle 'status' + 100 chars");
  }

  // ============================================================================
  // COMPLETION AFTER COMMAND EXECUTION
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_handle_tab_after_successful_command()
  {
    // Arrange: Execute "status" command, then type "t" and Tab
    Terminal.QueueLine("status");
    Terminal.QueueKeys("t");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should complete "t" to "time" after previous command
    Terminal.OutputContains("time").ShouldBeTrue("Should complete 't' to 'time' after status command");
  }

  // ============================================================================
  // NO MATCHES SCENARIOS
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_handle_no_matches_for_single_char()
  {
    // Arrange: Type "z" then Tab (no commands start with z)
    Terminal.QueueKeys("z");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should not show any completions
    CompletionAssertions.ShouldNotShowCompletions(Terminal,
      "status", "time", "git", "deploy"
    );
  }

  [Timeout(5000)]
  public static async Task Should_handle_no_matches_for_multi_char()
  {
    // Arrange: Type "xyz" then Tab
    Terminal.QueueKeys("xyz");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should not show any completions
    Terminal.OutputContains("Available completions").ShouldBeFalse(
      "Should not show completion list for no matches"
    );
  }
}
