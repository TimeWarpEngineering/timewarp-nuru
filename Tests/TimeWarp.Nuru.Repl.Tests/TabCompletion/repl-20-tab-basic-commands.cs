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
  private static TestTerminal? Terminal;
  private static NuruApp? App;

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
    Terminal = null;
    App = null;
    await Task.CompletedTask;
  }

  // ============================================================================
  // EMPTY INPUT COMPLETION
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_show_all_commands_on_empty_tab()
  {
    // Arrange: Empty input, press Tab
    Terminal!.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App!.RunReplAsync();

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
    KeySequenceHelpers.TypeAndTab(Terminal!, "st");
    KeySequenceHelpers.CleanupAndExit(Terminal!);

    // Act
    await App!.RunReplAsync();

    // Assert: Should auto-complete to "status" without showing list
    CompletionAssertions.ShouldAutoComplete(Terminal!, "status");
  }

  [Timeout(5000)]
  public static async Task Should_auto_complete_unique_match_ti_to_time()
  {
    // Arrange: Type "ti" then Tab - only "time" starts with "ti"
    KeySequenceHelpers.TypeAndTab(Terminal!, "ti");
    KeySequenceHelpers.CleanupAndExit(Terminal!);

    // Act
    await App!.RunReplAsync();

    // Assert: Should auto-complete to "time" without showing list
    CompletionAssertions.ShouldAutoComplete(Terminal!, "time");
  }

  [Timeout(5000)]
  public static async Task Should_auto_complete_unique_match_gr_to_greet()
  {
    // Arrange: Type "gr" then Tab - only "greet" starts with "gr"
    KeySequenceHelpers.TypeAndTab(Terminal!, "gr");
    KeySequenceHelpers.CleanupAndExit(Terminal!);

    // Act
    await App!.RunReplAsync();

    // Assert: Should auto-complete to "greet" without showing list
    CompletionAssertions.ShouldAutoComplete(Terminal!, "greet");
  }

  // ============================================================================
  // MULTIPLE MATCHES SHOW LIST
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_show_multiple_commands_starting_with_s()
  {
    // Arrange: Type "s" then Tab - "status" and "search" both start with "s"
    KeySequenceHelpers.TypeAndTab(Terminal!, "s");
    KeySequenceHelpers.CleanupAndExit(Terminal!);

    // Act
    await App!.RunReplAsync();

    // Assert: Should show both commands in list
    CompletionAssertions.ShouldShowCompletions(Terminal!, "status", "search");
    CompletionAssertions.ShouldShowCompletionList(Terminal!);
  }

  [Timeout(5000)]
  public static async Task Should_show_multiple_commands_starting_with_b()
  {
    // Arrange: Type "b" then Tab - "build" and "backup" both start with "b"
    KeySequenceHelpers.TypeAndTab(Terminal!, "b");
    KeySequenceHelpers.CleanupAndExit(Terminal!);

    // Act
    await App!.RunReplAsync();

    // Assert: Should show both commands in list
    CompletionAssertions.ShouldShowCompletions(Terminal!, "build", "backup");
    CompletionAssertions.ShouldShowCompletionList(Terminal!);
  }

  // ============================================================================
  // NOTE: Help option tests moved to repl-27-tab-help-option.cs
  // The --help option behavior after complete commands needs further investigation
  // ============================================================================
}
