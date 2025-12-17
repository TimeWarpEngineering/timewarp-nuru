#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../../source/timewarp-nuru-repl/timewarp-nuru-repl.csproj

using TimeWarp.Nuru.Tests.TabCompletion;

// ============================================================================
// Subcommand Completion Tests
// ============================================================================
// Tests tab completion behavior for hierarchical commands (subcommands):
// - git status, git commit, git log
// - Showing all subcommands after parent command
// - Filtering subcommands with partial input
// - Completion after subcommand selection
// - Proper context awareness (only show git subcommands after "git")
//
// These tests validate that the completion engine correctly handles
// multi-level command structures and maintains proper context.
// ============================================================================

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.TabCompletion.Subcommands
{

[TestTag("REPL")]
[TestTag("TabCompletion")]
public class SubcommandCompletionTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<SubcommandCompletionTests>();

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
  // SHOW ALL SUBCOMMANDS
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_show_all_git_subcommands_on_tab()
  {
    // Arrange: Type "git " then Tab
    Terminal.QueueKeys("git ");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show all git subcommands
    CompletionAssertions.ShouldShowCompletions(Terminal, "status", "commit", "log");
    CompletionAssertions.ShouldShowCompletionList(Terminal);
  }

  // ============================================================================
  // PARTIAL SUBCOMMAND MATCHING
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_auto_complete_git_s_to_status()
  {
    // Arrange: Type "git s" then Tab - only "status" starts with "s"
    Terminal.QueueKeys("git s");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should auto-complete to "status"
    Terminal.OutputContains("status").ShouldBeTrue("Should complete 'git s' to 'git status'");
  }

  [Timeout(5000)]
  public static async Task Should_auto_complete_git_st_to_status()
  {
    // Arrange: Type "git st" then Tab - unique match
    Terminal.QueueKeys("git st");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should auto-complete to "status"
    Terminal.OutputContains("status").ShouldBeTrue("Should complete 'git st' to 'git status'");
  }

  [Timeout(5000)]
  public static async Task Should_auto_complete_git_com_to_commit()
  {
    // Arrange: Type "git com" then Tab - unique match
    Terminal.QueueKeys("git com");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should auto-complete to "commit"
    Terminal.OutputContains("commit").ShouldBeTrue("Should complete 'git com' to 'git commit'");
  }

  [Timeout(5000)]
  public static async Task Should_auto_complete_git_l_to_log()
  {
    // Arrange: Type "git l" then Tab - only "log" starts with "l"
    Terminal.QueueKeys("git l");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should auto-complete to "log"
    Terminal.OutputContains("log").ShouldBeTrue("Should complete 'git l' to 'git log'");
  }

  [Timeout(5000)]
  public static async Task Should_show_multiple_subcommands_starting_with_c()
  {
    // Arrange: Type "git c" then Tab - "commit" starts with "c"
    Terminal.QueueKeys("git c");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should complete to "commit" (only one match)
    Terminal.OutputContains("commit").ShouldBeTrue("Should show 'commit' subcommand");
  }

  // ============================================================================
  // CONTEXT AWARENESS - NO INAPPROPRIATE OPTIONS
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_not_show_route_specific_options_before_subcommand()
  {
    // Arrange: Type "git " then Tab
    Terminal.QueueKeys("git ");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should NOT show route-specific options before selecting subcommand
    CompletionAssertions.ShouldNotShowCompletions(Terminal,
      "--count",  // only for "git log"
      " -m"       // only for "git commit" (space before -m to avoid matching "commit")
    );

    // Should still show subcommands
    CompletionAssertions.ShouldShowCompletions(Terminal, "commit", "log", "status");
  }

  // ============================================================================
  // AFTER SUBCOMMAND COMPLETION
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_show_option_after_git_commit_space()
  {
    // Arrange: Type "git commit " then Tab - should show -m option
    Terminal.QueueKeys("git commit ");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show -m option
    Terminal.OutputContains("-m").ShouldBeTrue("Should show '-m' option after 'git commit '");

    // Should NOT duplicate the subcommand
    Terminal.OutputContains("git commit commit").ShouldBeFalse("Should NOT duplicate 'commit'");
  }

  [Timeout(5000)]
  public static async Task Should_show_option_after_git_log_space()
  {
    // Arrange: Type "git log " then Tab - should show --count option
    Terminal.QueueKeys("git log ");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show --count option
    Terminal.OutputContains("--count").ShouldBeTrue("Should show '--count' option after 'git log '");
  }

  // ============================================================================
  // NO MATCHES FOR INVALID SUBCOMMANDS
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_show_no_completions_for_git_z()
  {
    // Arrange: Type "git z" then Tab - no subcommands start with "z"
    Terminal.QueueKeys("git z");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should not show any git subcommands
    CompletionAssertions.ShouldNotShowCompletions(Terminal,
      "status", "commit", "log"
    );
  }

  // ============================================================================
  // CASE SENSITIVITY
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_complete_git_S_to_status_case_insensitive()
  {
    // Arrange: Type "git S" then Tab - case insensitive
    Terminal.QueueKeys("git S");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should match case-insensitively
    Terminal.OutputContains("status").ShouldBeTrue("Should match 'status' case-insensitively");
  }

  [Timeout(5000)]
  public static async Task Should_complete_git_C_to_commit_case_insensitive()
  {
    // Arrange: Type "git C" then Tab - case insensitive
    Terminal.QueueKeys("git C");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should match case-insensitively
    Terminal.OutputContains("commit").ShouldBeTrue("Should match 'commit' case-insensitively");
  }
}

} // namespace TimeWarp.Nuru.Tests.TabCompletion.Subcommands
