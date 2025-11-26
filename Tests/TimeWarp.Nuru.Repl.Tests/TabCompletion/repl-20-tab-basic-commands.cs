#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../../Source/TimeWarp.Nuru.Repl/TimeWarp.Nuru.Repl.csproj

using TimeWarp.Nuru;
using TimeWarp.Nuru.Repl;
using TimeWarp.Nuru.Repl.Tests.TabCompletion;

// ============================================================================
// Basic Command Completion Tests
// ============================================================================
// Tests fundamental tab completion behavior for top-level commands:
// - Empty input completion (show all commands)
// - Partial matching and filtering
// - Unique match auto-completion
// - Multiple matches showing completion list
// - Help option availability after complete commands
//
// These tests validate the core completion engine works correctly for
// simple command-level completions before moving to more complex scenarios
// like subcommands, options, and enums.
// ============================================================================

return await RunTests<BasicCommandCompletionTests>();

[TestTag("REPL")]
[TestTag("TabCompletion")]
[ClearRunfileCache]
public class BasicCommandCompletionTests
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
  // EMPTY INPUT COMPLETION
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_show_all_commands_on_empty_tab()
  {
    // Arrange: Empty input, press Tab
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show all top-level commands
    CompletionAssertions.ShouldShowCompletions(Terminal,
      "status", "time", "greet", "add", "deploy",
      "echo", "git", "build", "search", "backup"
    );
    CompletionAssertions.ShouldShowCompletionList(Terminal);
  }

  // ============================================================================
  // UNIQUE MATCH AUTO-COMPLETION
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_auto_complete_unique_match_st_to_status()
  {
    // Arrange: Type "st" then Tab - only "status" starts with "st"
    KeySequenceHelpers.TypeAndTab(Terminal, "st");
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should auto-complete to "status" without showing list
    CompletionAssertions.ShouldAutoComplete(Terminal, "status");
  }

  [Timeout(5000)]
  public static async Task Should_auto_complete_unique_match_ti_to_time()
  {
    // Arrange: Type "ti" then Tab - only "time" starts with "ti"
    KeySequenceHelpers.TypeAndTab(Terminal, "ti");
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should auto-complete to "time" without showing list
    CompletionAssertions.ShouldAutoComplete(Terminal, "time");
  }

  [Timeout(5000)]
  public static async Task Should_auto_complete_unique_match_gr_to_greet()
  {
    // Arrange: Type "gr" then Tab - only "greet" starts with "gr"
    KeySequenceHelpers.TypeAndTab(Terminal, "gr");
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should auto-complete to "greet" without showing list
    CompletionAssertions.ShouldAutoComplete(Terminal, "greet");
  }

  // ============================================================================
  // MULTIPLE MATCHES SHOW LIST
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_show_multiple_commands_starting_with_s()
  {
    // Arrange: Type "s" then Tab - "status" and "search" both start with "s"
    KeySequenceHelpers.TypeAndTab(Terminal, "s");
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show both commands in list
    CompletionAssertions.ShouldShowCompletions(Terminal, "status", "search");
    CompletionAssertions.ShouldShowCompletionList(Terminal);
  }

  [Timeout(5000)]
  public static async Task Should_show_multiple_commands_starting_with_b()
  {
    // Arrange: Type "b" then Tab - "build" and "backup" both start with "b"
    KeySequenceHelpers.TypeAndTab(Terminal, "b");
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show both commands in list
    CompletionAssertions.ShouldShowCompletions(Terminal, "build", "backup");
    CompletionAssertions.ShouldShowCompletionList(Terminal);
  }

  // ============================================================================
  // MORE UNIQUE MATCHES - COMPREHENSIVE LETTER COVERAGE
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_auto_complete_unique_match_a_to_add()
  {
    // Arrange: Type "a" then Tab - only "add" starts with "a"
    KeySequenceHelpers.TypeAndTab(Terminal, "a");
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should auto-complete to "add" without showing list
    CompletionAssertions.ShouldAutoComplete(Terminal, "add");
  }

  [Timeout(5000)]
  public static async Task Should_auto_complete_unique_match_d_to_deploy()
  {
    // Arrange: Type "d" then Tab - only "deploy" starts with "d"
    KeySequenceHelpers.TypeAndTab(Terminal, "d");
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should auto-complete to "deploy" without showing list
    CompletionAssertions.ShouldAutoComplete(Terminal, "deploy");
  }

  [Timeout(5000)]
  public static async Task Should_show_multiple_commands_starting_with_e()
  {
    // Arrange: Type "e" then Tab - "echo" and "exit" both start with "e"
    KeySequenceHelpers.TypeAndTab(Terminal, "e");
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show both commands in list (echo and exit builtin)
    CompletionAssertions.ShouldShowCompletions(Terminal, "echo", "exit");
    CompletionAssertions.ShouldShowCompletionList(Terminal);
  }

  [Timeout(5000)]
  public static async Task Should_auto_complete_unique_match_g_to_git()
  {
    // Arrange: Type "g" then Tab - only "git" starts with "g" (greet also exists)
    // Actually both git and greet start with g, so this should show list
    KeySequenceHelpers.TypeAndTab(Terminal, "g");
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show both commands
    CompletionAssertions.ShouldShowCompletions(Terminal, "git", "greet");
    CompletionAssertions.ShouldShowCompletionList(Terminal);
  }

  // ============================================================================
  // NO MATCHES
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_show_no_completions_for_z()
  {
    // Arrange: Type "z" then Tab - no commands start with "z"
    KeySequenceHelpers.TypeAndTab(Terminal, "z");
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should not show any command completions (no status, git, etc.)
    // The output should not contain the completion list header
    CompletionAssertions.ShouldNotShowCompletions(Terminal,
      "status", "time", "greet", "add", "deploy",
      "echo", "git", "build", "search", "backup"
    );
  }

  [Timeout(5000)]
  public static async Task Should_show_no_completions_for_x()
  {
    // Arrange: Type "x" then Tab - no commands start with "x"
    KeySequenceHelpers.TypeAndTab(Terminal, "x");
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should not show any command completions
    CompletionAssertions.ShouldNotShowCompletions(Terminal,
      "status", "time", "greet", "add", "deploy"
    );
  }

  // ============================================================================
  // CASE SENSITIVITY
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_complete_uppercase_S_to_status_or_search()
  {
    // Arrange: Type "S" then Tab - case insensitive matching
    KeySequenceHelpers.TypeAndTab(Terminal, "S");
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show completions (case insensitive)
    Terminal.OutputContains("status").ShouldBeTrue("Should match 'status' case-insensitively");
  }

  [Timeout(5000)]
  public static async Task Should_complete_uppercase_G_to_git_or_greet()
  {
    // Arrange: Type "G" then Tab - case insensitive matching
    KeySequenceHelpers.TypeAndTab(Terminal, "G");
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show completions (case insensitive)
    Terminal.OutputContains("git").ShouldBeTrue("Should match 'git' case-insensitively");
  }

  // ============================================================================
  // PARTIAL COMPLETION WITH MORE LETTERS
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_auto_complete_sta_to_status()
  {
    // Arrange: Type "sta" then Tab - unique match
    KeySequenceHelpers.TypeAndTab(Terminal, "sta");
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should auto-complete to "status"
    CompletionAssertions.ShouldAutoComplete(Terminal, "status");
  }

  [Timeout(5000)]
  public static async Task Should_auto_complete_sea_to_search()
  {
    // Arrange: Type "sea" then Tab - unique match
    KeySequenceHelpers.TypeAndTab(Terminal, "sea");
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should auto-complete to "search"
    CompletionAssertions.ShouldAutoComplete(Terminal, "search");
  }

  [Timeout(5000)]
  public static async Task Should_auto_complete_bui_to_build()
  {
    // Arrange: Type "bui" then Tab - unique match (build vs backup)
    KeySequenceHelpers.TypeAndTab(Terminal, "bui");
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should auto-complete to "build"
    CompletionAssertions.ShouldAutoComplete(Terminal, "build");
  }

  [Timeout(5000)]
  public static async Task Should_auto_complete_bac_to_backup()
  {
    // Arrange: Type "bac" then Tab - unique match (backup vs build)
    KeySequenceHelpers.TypeAndTab(Terminal, "bac");
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should auto-complete to "backup"
    CompletionAssertions.ShouldAutoComplete(Terminal, "backup");
  }

  // ============================================================================
  // EXACT COMMAND NAMES
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_accept_exact_command_status_with_tab()
  {
    // Arrange: Type "status" then Tab - exact match
    KeySequenceHelpers.TypeAndTab(Terminal, "status");
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Exact match should be in output (no further completion needed)
    Terminal.OutputContains("status").ShouldBeTrue("Should accept exact 'status' command");
  }

  // ============================================================================
  // NOTE: Help option tests moved to repl-27-tab-help-option.cs
  // The --help option behavior after complete commands needs further investigation
  // ============================================================================
}
