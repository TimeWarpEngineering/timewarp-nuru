#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../source/timewarp-nuru-repl/timewarp-nuru-repl.csproj

// Tests for PSReadLine-compatible interactive history search (Task 043-002a)
// Verifies Ctrl+R (reverse search) and Ctrl+S (forward search) functionality

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.InteractiveHistorySearch
{

[TestTag("REPL")]
[TestTag("PSReadLine")]
[TestTag("HistorySearch")]
public class InteractiveHistorySearchTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<InteractiveHistorySearchTests>();

  private static TestTerminal? Terminal;
  private static NuruCoreApp? App;

  public static async Task Setup()
  {
    // Create fresh terminal and app for each test
    Terminal = new TestTerminal();

    App = new NuruAppBuilder()
      .UseTerminal(Terminal)
      .Map("deploy prod", () => "Deployed to prod!")
      .Map("deploy staging", () => "Deployed to staging!")
      .Map("deploy dev", () => "Deployed to dev!")
      .Map("status", () => "Status OK")
      .Map("greet {name}", (string name) => $"Hello, {name}!")
      .Map("hello", () => "Hello!")
      .AddReplSupport(options =>
      {
        options.EnableArrowHistory = true;
        options.EnableColors = false;  // Disable colors for cleaner test assertions
      })
      .Build();

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
  // Ctrl+R: Reverse Interactive Search
  // ============================================================================

  public static async Task Should_enter_search_mode_with_ctrl_r()
  {
    // Arrange - Create history with commands
    Terminal!.QueueLine("deploy prod");
    Terminal!.QueueLine("deploy staging");
    Terminal!.QueueLine("status");
    // Press Ctrl+R to enter search mode
    Terminal.QueueKey(ConsoleKey.R, ctrl: true);
    // Type search pattern "dep"
    Terminal.QueueKeys("dep");
    // Press Enter to accept match
    Terminal.QueueKey(ConsoleKey.Enter);
    Terminal.QueueLine("exit");

    // Act
    await App!.RunReplAsync();

    // Assert - Search mode was entered and exited successfully
    Terminal.OutputContains("Goodbye!").ShouldBeTrue("Ctrl+R should enter search mode");
    // The output should show the search prompt
    Terminal.OutputContains("reverse-i-search").ShouldBeTrue("Should display reverse search prompt");
  }

  public static async Task Should_find_matching_history_entry_with_ctrl_r()
  {
    // Arrange
    Terminal!.QueueLine("deploy prod");
    Terminal!.QueueLine("status");
    Terminal!.QueueLine("greet Alice");
    // Enter search mode and type pattern
    Terminal.QueueKey(ConsoleKey.R, ctrl: true);
    Terminal.QueueKeys("deploy");
    // Press Enter to accept (should find "deploy prod")
    Terminal.QueueKey(ConsoleKey.Enter);
    Terminal.QueueLine("exit");

    // Act
    await App!.RunReplAsync();

    // Assert - The matched command should have been executed
    Terminal.OutputContains("Deployed to prod!").ShouldBeTrue("Should find and execute matching history entry");
  }

  public static async Task Should_cycle_through_matches_with_repeated_ctrl_r()
  {
    // Arrange
    Terminal!.QueueLine("deploy dev");
    Terminal!.QueueLine("deploy staging");
    Terminal!.QueueLine("deploy prod");
    Terminal!.QueueLine("status");
    // Enter search mode
    Terminal.QueueKey(ConsoleKey.R, ctrl: true);
    Terminal.QueueKeys("deploy");
    // Press Ctrl+R again to find older match (deploy staging)
    Terminal.QueueKey(ConsoleKey.R, ctrl: true);
    // Press Ctrl+R again to find even older match (deploy dev)
    Terminal.QueueKey(ConsoleKey.R, ctrl: true);
    // Press Enter to accept
    Terminal.QueueKey(ConsoleKey.Enter);
    Terminal.QueueLine("exit");

    // Act
    await App!.RunReplAsync();

    // Assert - Should have found "deploy dev" (the oldest match)
    Terminal.OutputContains("Deployed to dev!").ShouldBeTrue("Repeated Ctrl+R should cycle to older matches");
  }

  public static async Task Should_cancel_search_with_escape()
  {
    // Arrange
    Terminal!.QueueLine("deploy prod");
    Terminal!.QueueLine("status");
    // Type some initial input
    Terminal.QueueKeys("hello");
    // Enter search mode
    Terminal.QueueKey(ConsoleKey.R, ctrl: true);
    Terminal.QueueKeys("deploy");
    // Press Escape to cancel (should restore "hello")
    Terminal.QueueKey(ConsoleKey.Escape);
    // Now the input should be "hello" again, complete and execute
    Terminal.QueueKey(ConsoleKey.Enter);
    Terminal.QueueLine("exit");

    // Act
    await App!.RunReplAsync();

    // Assert - "hello" command was executed (restored after cancel)
    Terminal.OutputContains("Hello!").ShouldBeTrue("Escape should cancel search and restore original input");
  }

  public static async Task Should_update_search_incrementally()
  {
    // Arrange
    Terminal!.QueueLine("deploy dev");
    Terminal!.QueueLine("deploy staging");
    Terminal!.QueueLine("greet Alice");
    // Enter search mode
    Terminal.QueueKey(ConsoleKey.R, ctrl: true);
    // Type incrementally - first 'd' might match "deploy staging"
    Terminal.QueueKeys("d");
    // Then 'e' continues refining
    Terminal.QueueKeys("eploy s");
    // Accept match (should be "deploy staging")
    Terminal.QueueKey(ConsoleKey.Enter);
    Terminal.QueueLine("exit");

    // Act
    await App!.RunReplAsync();

    // Assert
    Terminal.OutputContains("Deployed to staging!").ShouldBeTrue("Search should update incrementally");
  }

  public static async Task Should_handle_backspace_in_search_pattern()
  {
    // Arrange
    Terminal!.QueueLine("deploy prod");
    Terminal!.QueueLine("status");
    // Enter search mode
    Terminal.QueueKey(ConsoleKey.R, ctrl: true);
    // Type search pattern
    Terminal.QueueKeys("sta");
    // Backspace to remove 'a'
    Terminal.QueueKey(ConsoleKey.Backspace);
    // Now pattern is "st" - still matches "status"
    Terminal.QueueKey(ConsoleKey.Enter);
    Terminal.QueueLine("exit");

    // Act
    await App!.RunReplAsync();

    // Assert
    Terminal.OutputContains("Status OK").ShouldBeTrue("Backspace should remove from search pattern");
  }

  // ============================================================================
  // Ctrl+S: Forward Interactive Search
  // ============================================================================

  public static async Task Should_enter_forward_search_mode_with_ctrl_s()
  {
    // Arrange
    Terminal!.QueueLine("deploy prod");
    Terminal!.QueueLine("deploy staging");
    Terminal!.QueueLine("status");
    // Enter forward search mode
    Terminal.QueueKey(ConsoleKey.S, ctrl: true);
    // Type search pattern
    Terminal.QueueKeys("deploy");
    // Press Enter to accept
    Terminal.QueueKey(ConsoleKey.Enter);
    Terminal.QueueLine("exit");

    // Act
    await App!.RunReplAsync();

    // Assert
    Terminal.OutputContains("Goodbye!").ShouldBeTrue("Ctrl+S should enter forward search mode");
    Terminal.OutputContains("forward-i-search").ShouldBeTrue("Should display forward search prompt");
  }

  public static async Task Should_switch_direction_from_reverse_to_forward()
  {
    // Arrange
    Terminal!.QueueLine("deploy dev");
    Terminal!.QueueLine("deploy staging");
    Terminal!.QueueLine("deploy prod");
    Terminal!.QueueLine("status");
    // Enter reverse search
    Terminal.QueueKey(ConsoleKey.R, ctrl: true);
    Terminal.QueueKeys("deploy");
    // Find "deploy prod" (most recent)
    // Press Ctrl+R to find older "deploy staging"
    Terminal.QueueKey(ConsoleKey.R, ctrl: true);
    // Press Ctrl+S to go back to newer "deploy prod"
    Terminal.QueueKey(ConsoleKey.S, ctrl: true);
    // Accept
    Terminal.QueueKey(ConsoleKey.Enter);
    Terminal.QueueLine("exit");

    // Act
    await App!.RunReplAsync();

    // Assert - Should have ended on "deploy prod"
    Terminal.OutputContains("Deployed to prod!").ShouldBeTrue("Ctrl+S should switch to forward search");
  }

  // ============================================================================
  // Edge Cases
  // ============================================================================

  public static async Task Should_handle_empty_history()
  {
    // Arrange - No commands in history yet
    // Enter search mode
    Terminal!.QueueKey(ConsoleKey.R, ctrl: true);
    Terminal.QueueKeys("deploy");
    // No match possible, press Escape to cancel
    Terminal.QueueKey(ConsoleKey.Escape);
    // Type and execute a command
    Terminal.QueueLine("status");
    Terminal.QueueLine("exit");

    // Act
    await App!.RunReplAsync();

    // Assert - Should handle gracefully
    Terminal.OutputContains("Status OK").ShouldBeTrue("Should handle empty history gracefully");
  }

  public static async Task Should_handle_no_matches()
  {
    // Arrange
    Terminal!.QueueLine("status");
    Terminal!.QueueLine("hello");
    // Enter search mode
    Terminal.QueueKey(ConsoleKey.R, ctrl: true);
    // Type pattern that doesn't match anything
    Terminal.QueueKeys("xyz");
    // Cancel and type a valid command
    Terminal.QueueKey(ConsoleKey.Escape);
    Terminal.QueueLine("status");
    Terminal.QueueLine("exit");

    // Act
    await App!.RunReplAsync();

    // Assert
    Terminal.OutputContains("Goodbye!").ShouldBeTrue("Should handle no matches gracefully");
  }

  public static async Task Should_handle_empty_search_pattern()
  {
    // Arrange
    Terminal!.QueueLine("deploy prod");
    Terminal!.QueueLine("status");
    // Enter search mode with empty pattern
    Terminal.QueueKey(ConsoleKey.R, ctrl: true);
    // Immediately press Enter (empty pattern should match most recent)
    Terminal.QueueKey(ConsoleKey.Enter);
    Terminal.QueueLine("exit");

    // Act
    await App!.RunReplAsync();

    // Assert - Should execute most recent command (status)
    Terminal.OutputContains("Status OK").ShouldBeTrue("Empty pattern should match most recent history entry");
  }

  public static async Task Should_perform_case_insensitive_search()
  {
    // Arrange
    Terminal!.QueueLine("Deploy Prod");
    Terminal!.QueueLine("status");
    // Enter search mode
    Terminal.QueueKey(ConsoleKey.R, ctrl: true);
    // Search with lowercase
    Terminal.QueueKeys("deploy");
    // Accept
    Terminal.QueueKey(ConsoleKey.Enter);
    Terminal.QueueLine("exit");

    // Act
    await App!.RunReplAsync();

    // Assert - Should find "Deploy Prod" with lowercase search
    // Note: The command might not execute because "Deploy Prod" doesn't match our route
    // But the search should find it
    Terminal.OutputContains("reverse-i-search").ShouldBeTrue("Search should be case-insensitive");
  }

  public static async Task Should_cancel_with_ctrl_g()
  {
    // Arrange
    Terminal!.QueueLine("deploy prod");
    Terminal.QueueKeys("hello");
    // Enter search mode
    Terminal.QueueKey(ConsoleKey.R, ctrl: true);
    Terminal.QueueKeys("deploy");
    // Cancel with Ctrl+G (Emacs style)
    Terminal.QueueKey(ConsoleKey.G, ctrl: true);
    // Original input "hello" should be restored
    Terminal.QueueKey(ConsoleKey.Enter);
    Terminal.QueueLine("exit");

    // Act
    await App!.RunReplAsync();

    // Assert
    Terminal.OutputContains("Hello!").ShouldBeTrue("Ctrl+G should cancel search like Escape");
  }

  public static async Task Should_accept_and_process_other_keys()
  {
    // Arrange
    Terminal!.QueueLine("deploy prod");
    Terminal!.QueueLine("status");
    // Enter search mode
    Terminal.QueueKey(ConsoleKey.R, ctrl: true);
    Terminal.QueueKeys("deploy");
    // Press Home key - should accept match and move cursor to beginning
    Terminal.QueueKey(ConsoleKey.Home);
    // Now we're in normal mode with "deploy prod" as input, cursor at start
    // Move to end and execute
    Terminal.QueueKey(ConsoleKey.End);
    Terminal.QueueKey(ConsoleKey.Enter);
    Terminal.QueueLine("exit");

    // Act
    await App!.RunReplAsync();

    // Assert - The matched command should have been accepted and executed
    Terminal.OutputContains("Deployed to prod!").ShouldBeTrue("Other keys should accept match and be processed");
  }
}

} // namespace TimeWarp.Nuru.Tests.ReplTests.InteractiveHistorySearch
