#!/usr/bin/dotnet --
#:project ../../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../../Source/TimeWarp.Nuru.Repl/TimeWarp.Nuru.Repl.csproj

using TimeWarp.Nuru;
using TimeWarp.Nuru.Repl;
using TimeWarp.Nuru.Repl.Tests.TabCompletion;

// ============================================================================
// Help Option Availability Tests
// ============================================================================
// Tests that --help option appears in completions where expected:
// - After complete commands
// - In option lists
// - Partial completion of --help
// - Help mixed with other options
//
// NOTE: These tests document current behavior. Many tests may fail
// if --help completion is not fully implemented yet.
// ============================================================================

return await RunTests<HelpOptionTests>();

[TestTag("REPL")]
[TestTag("TabCompletion")]
[ClearRunfileCache]
public class HelpOptionTests
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
  // HELP IN COMMAND LISTS
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_include_help_in_empty_tab_completions()
  {
    // Arrange: Press Tab on empty input
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should include "help" command in the list
    Terminal.OutputContains("help").ShouldBeTrue("Should show 'help' command in completions");
  }

  // ============================================================================
  // PARTIAL HELP COMPLETION
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_complete_he_to_help()
  {
    // Arrange: Type "he" then Tab
    KeySequenceHelpers.TypeAndTab(Terminal, "he");
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should complete to "help"
    Terminal.OutputContains("help").ShouldBeTrue("Should complete 'he' to 'help'");
  }

  [Timeout(5000)]
  public static async Task Should_complete_h_to_help_or_history()
  {
    // Arrange: Type "h" then Tab - might show help, history, or both
    KeySequenceHelpers.TypeAndTab(Terminal, "h");
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show help in completions
    Terminal.OutputContains("help").ShouldBeTrue("Should show 'help' for 'h' prefix");
  }

  // ============================================================================
  // HELP OPTION (--help) AFTER COMMANDS
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_show_help_option_after_status_command()
  {
    // BUG: This test may FAIL - --help not shown after simple commands
    // Arrange: Type "status " then Tab
    Terminal.QueueKeys("status ");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show --help option
    Terminal.OutputContains("--help").ShouldBeTrue("Should show '--help' after 'status '");
  }

  [Timeout(5000)]
  public static async Task Should_show_help_option_after_time_command()
  {
    // BUG: This test may FAIL - --help not shown after simple commands
    // Arrange: Type "time " then Tab
    Terminal.QueueKeys("time ");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show --help option
    Terminal.OutputContains("--help").ShouldBeTrue("Should show '--help' after 'time '");
  }

  [Timeout(5000)]
  public static async Task Should_show_help_option_after_greet_command()
  {
    // BUG: This test may FAIL - --help not shown for commands with parameters
    // Arrange: Type "greet " then Tab
    Terminal.QueueKeys("greet ");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show --help option
    Terminal.OutputContains("--help").ShouldBeTrue("Should show '--help' after 'greet '");
  }

  [Timeout(5000)]
  public static async Task Should_show_help_option_with_deploy_enum_values()
  {
    // BUG: This test may FAIL - documented in existing test file
    // Arrange: Type "deploy " then Tab
    Terminal.QueueKeys("deploy ");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show --help along with enum values
    Terminal.OutputContains("--help").ShouldBeTrue("Should show '--help' with enum values");
  }

  // ============================================================================
  // HELP OPTION PARTIAL COMPLETION
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_complete_dash_dash_he_to_help()
  {
    // Arrange: Type "status --he" then Tab
    Terminal.QueueKeys("status --he");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should complete to --help
    Terminal.OutputContains("--help").ShouldBeTrue("Should complete '--he' to '--help'");
  }

  [Timeout(5000)]
  public static async Task Should_complete_dash_dash_h_to_help()
  {
    // Arrange: Type "status --h" then Tab
    Terminal.QueueKeys("status --h");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should complete to --help
    Terminal.OutputContains("--help").ShouldBeTrue("Should complete '--h' to '--help'");
  }

  // ============================================================================
  // HELP WITH OTHER OPTIONS
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_show_help_with_build_options()
  {
    // BUG: This test may FAIL - --help not shown with other options
    // Arrange: Type "build " then Tab
    Terminal.QueueKeys("build ");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show --help along with --verbose, -v
    Terminal.OutputContains("--help").ShouldBeTrue("Should show '--help' with build options");
  }

  [Timeout(5000)]
  public static async Task Should_show_help_with_search_options()
  {
    // BUG: This test may FAIL - --help not shown with other options
    // Arrange: Type "search foo " then Tab
    Terminal.QueueKeys("search foo ");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show --help along with --limit, -l
    Terminal.OutputContains("--help").ShouldBeTrue("Should show '--help' with search options");
  }

  // ============================================================================
  // CASE SENSITIVITY
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_complete_uppercase_HE_to_help()
  {
    // Arrange: Type "HE" then Tab - case insensitive
    KeySequenceHelpers.TypeAndTab(Terminal, "HE");
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should match case-insensitively
    Terminal.OutputContains("help").ShouldBeTrue("Should match 'HE' to 'help' case-insensitively");
  }

  [Timeout(5000)]
  public static async Task Should_complete_mixed_case_HeLp()
  {
    // Arrange: Type "HeLp" then Tab
    KeySequenceHelpers.TypeAndTab(Terminal, "HeLp");
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should match case-insensitively
    Terminal.OutputContains("help").ShouldBeTrue("Should match 'HeLp' case-insensitively");
  }
}
