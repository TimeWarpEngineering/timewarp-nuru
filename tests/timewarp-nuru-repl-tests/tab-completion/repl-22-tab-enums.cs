#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../../source/timewarp-nuru-repl/timewarp-nuru-repl.csproj

using TimeWarp.Nuru;
using TimeWarp.Nuru.Tests.TabCompletion;

// ============================================================================
// Enum Parameter Completion Tests
// ============================================================================
// Tests tab completion behavior for enum-typed parameters:
// - deploy {env:environment} where environment = Dev|Staging|Prod
// - Showing all enum values after command
// - Filtering enum values with partial input
// - Case sensitivity handling for enum values
// - Optional parameters after enum selection
//
// These tests validate that enum type converters work correctly with
// tab completion and provide proper IntelliSense-like experience.
// ============================================================================

return await RunTests<EnumCompletionTests>();

[TestTag("REPL")]
[TestTag("TabCompletion")]
[ClearRunfileCache]
public class EnumCompletionTests
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
  // SHOW ALL ENUM VALUES
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_show_all_enum_values_after_deploy_space()
  {
    // Arrange: Type "deploy " then Tab
    Terminal.QueueKeys("deploy ");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should show all Environment enum values
    CompletionAssertions.ShouldShowCompletions(Terminal, "Dev", "Staging", "Prod");
    CompletionAssertions.ShouldShowCompletionList(Terminal);
  }

  // ============================================================================
  // PARTIAL ENUM VALUE MATCHING
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_auto_complete_deploy_d_to_Dev()
  {
    // Arrange: Type "deploy d" then Tab - only "Dev" starts with "d"
    Terminal.QueueKeys("deploy d");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should auto-complete to "Dev"
    Terminal.OutputContains("Dev").ShouldBeTrue("Should complete 'deploy d' to 'deploy Dev'");
  }

  [Timeout(5000)]
  public static async Task Should_auto_complete_deploy_s_to_Staging()
  {
    // Arrange: Type "deploy s" then Tab - only "Staging" starts with "s"
    Terminal.QueueKeys("deploy s");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should auto-complete to "Staging"
    Terminal.OutputContains("Staging").ShouldBeTrue("Should complete 'deploy s' to 'deploy Staging'");
  }

  [Timeout(5000)]
  public static async Task Should_auto_complete_deploy_p_to_Prod()
  {
    // Arrange: Type "deploy p" then Tab - only "Prod" starts with "p"
    Terminal.QueueKeys("deploy p");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should auto-complete to "Prod"
    Terminal.OutputContains("Prod").ShouldBeTrue("Should complete 'deploy p' to 'deploy Prod'");
  }

  [Timeout(5000)]
  public static async Task Should_auto_complete_deploy_sta_to_Staging()
  {
    // Arrange: Type "deploy sta" then Tab - unique match
    Terminal.QueueKeys("deploy sta");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should auto-complete to "Staging"
    Terminal.OutputContains("Staging").ShouldBeTrue("Should complete 'deploy sta' to 'deploy Staging'");
  }

  [Timeout(5000)]
  public static async Task Should_auto_complete_deploy_pr_to_Prod()
  {
    // Arrange: Type "deploy pr" then Tab - unique match
    Terminal.QueueKeys("deploy pr");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should auto-complete to "Prod"
    Terminal.OutputContains("Prod").ShouldBeTrue("Should complete 'deploy pr' to 'deploy Prod'");
  }

  // ============================================================================
  // CASE SENSITIVITY
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_complete_deploy_D_to_Dev_case_insensitive()
  {
    // Arrange: Type "deploy D" then Tab - case insensitive
    Terminal.QueueKeys("deploy D");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should match case-insensitively
    Terminal.OutputContains("Dev").ShouldBeTrue("Should match 'Dev' case-insensitively");
  }

  [Timeout(5000)]
  public static async Task Should_complete_deploy_S_to_Staging_case_insensitive()
  {
    // Arrange: Type "deploy S" then Tab - case insensitive
    Terminal.QueueKeys("deploy S");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should match case-insensitively
    Terminal.OutputContains("Staging").ShouldBeTrue("Should match 'Staging' case-insensitively");
  }

  [Timeout(5000)]
  public static async Task Should_complete_deploy_P_to_Prod_case_insensitive()
  {
    // Arrange: Type "deploy P" then Tab - case insensitive
    Terminal.QueueKeys("deploy P");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should match case-insensitively
    Terminal.OutputContains("Prod").ShouldBeTrue("Should match 'Prod' case-insensitively");
  }

  [Timeout(5000)]
  public static async Task Should_complete_deploy_lowercase_dev()
  {
    // Arrange: Type "deploy dev" then Tab - all lowercase
    Terminal.QueueKeys("deploy dev");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should match case-insensitively
    Terminal.OutputContains("Dev").ShouldBeTrue("Should match 'dev' to 'Dev' case-insensitively");
  }

  [Timeout(5000)]
  public static async Task Should_complete_deploy_uppercase_DEV()
  {
    // Arrange: Type "deploy DEV" then Tab - all uppercase
    Terminal.QueueKeys("deploy DEV");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should match case-insensitively
    Terminal.OutputContains("Dev").ShouldBeTrue("Should match 'DEV' to 'Dev' case-insensitively");
  }

  // ============================================================================
  // NO MATCHES FOR INVALID ENUM VALUES
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_show_no_completions_for_deploy_z()
  {
    // Arrange: Type "deploy z" then Tab - no enum values start with "z"
    Terminal.QueueKeys("deploy z");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should not show any enum values
    CompletionAssertions.ShouldNotShowCompletions(Terminal, "Dev", "Staging", "Prod");
  }

  [Timeout(5000)]
  public static async Task Should_show_no_completions_for_deploy_x()
  {
    // Arrange: Type "deploy x" then Tab - no enum values start with "x"
    Terminal.QueueKeys("deploy x");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should not show any enum values
    CompletionAssertions.ShouldNotShowCompletions(Terminal, "Dev", "Staging", "Prod");
  }

  // ============================================================================
  // OPTIONAL PARAMETER AFTER ENUM
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_allow_tab_after_deploy_dev_space()
  {
    // Arrange: Type "deploy dev " then Tab - optional tag parameter
    Terminal.QueueKeys("deploy dev ");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Should accept input (no error) - optional parameter position
    // The output should contain "deploy" indicating command was recognized
    Terminal.OutputContains("deploy").ShouldBeTrue("Should accept optional tag parameter position");
  }

  // ============================================================================
  // EXACT ENUM VALUE MATCHES
  // ============================================================================

  [Timeout(5000)]
  public static async Task Should_accept_exact_enum_value_Dev()
  {
    // Arrange: Type "deploy Dev" then Tab - exact match
    Terminal.QueueKeys("deploy Dev");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Exact match should be in output
    Terminal.OutputContains("Dev").ShouldBeTrue("Should accept exact 'Dev' enum value");
  }

  [Timeout(5000)]
  public static async Task Should_accept_exact_enum_value_Staging()
  {
    // Arrange: Type "deploy Staging" then Tab - exact match
    Terminal.QueueKeys("deploy Staging");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Exact match should be in output
    Terminal.OutputContains("Staging").ShouldBeTrue("Should accept exact 'Staging' enum value");
  }

  [Timeout(5000)]
  public static async Task Should_accept_exact_enum_value_Prod()
  {
    // Arrange: Type "deploy Prod" then Tab - exact match
    Terminal.QueueKeys("deploy Prod");
    Terminal.QueueKey(ConsoleKey.Tab);
    KeySequenceHelpers.CleanupAndExit(Terminal);

    // Act
    await App.RunReplAsync();

    // Assert: Exact match should be in output
    Terminal.OutputContains("Prod").ShouldBeTrue("Should accept exact 'Prod' enum value");
  }
}
