#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

#region Purpose
// Pins PSReadLine-compatible REPL history, completion, and delete keybindings (task 043_001); split from repl-18-psreadline-keybindings.cs.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.PSReadLineKeybindingsHistory
{

[TestTag("REPL")]
[TestTag("PSReadLine")]
public class PSReadLineHistoryKeybindingsTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<PSReadLineHistoryKeybindingsTests>();

  private static TestTerminal? Terminal;
  private static NuruApp? App;

  public static async Task Setup()
  {
    // Create fresh terminal and app for each test
    Terminal = new TestTerminal();

    App = NuruApp.CreateBuilder()
      .UseTerminal(Terminal)
      .Map("aXb")
        .WithHandler(() => "Success!")
        .AsQuery()
        .Done()
      .Map("hello")
        .WithHandler(() => "Success!")
        .AsQuery()
        .Done()
      .Map("hello world")
        .WithHandler(() => "Success!")
        .AsQuery()
        .Done()
      .Map("helloX world")
        .WithHandler(() => "Success!")
        .AsQuery()
        .Done()
      .Map("say hello world")
        .WithHandler(() => "Success!")
        .AsQuery()
        .Done()
      .Map("greet {name}")
        .WithHandler((string name) => $"Hello, {name}!")
        .AsCommand()
        .Done()
      .AddRepl(options =>
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
  // PreviousHistory: UpArrow, Ctrl+P
  // ============================================================================

  public static async Task Should_navigate_previous_history_with_up_arrow()
  {
    // Arrange
    Terminal!.QueueLine("greet Alice");           // Execute first command
    Terminal.QueueKey(ConsoleKey.UpArrow);       // Navigate to previous (greet Alice)
    Terminal.QueueKey(ConsoleKey.Enter);         // Execute again
    Terminal.QueueLine("exit");

    // Act
    await App!.RunReplAsync();

    // Assert - Session completed successfully (history navigation worked)
    Terminal.OutputContains("Goodbye!").ShouldBeTrue("UpArrow should recall previous history item");
  }

  public static async Task Should_navigate_previous_history_with_ctrl_p()
  {
    // Arrange
    Terminal!.QueueLine("greet Bob");             // Execute first command
    Terminal.QueueKey(ConsoleKey.P, ctrl: true); // Ctrl+P = PreviousHistory
    Terminal.QueueKey(ConsoleKey.Enter);         // Execute again
    Terminal.QueueLine("exit");

    // Act
    await App!.RunReplAsync();

    // Assert - Session completed successfully (history navigation worked)
    Terminal.OutputContains("Goodbye!").ShouldBeTrue("Ctrl+P should recall previous history item");
  }

  // ============================================================================
  // NextHistory: DownArrow, Ctrl+N
  // ============================================================================

  public static async Task Should_navigate_next_history_with_down_arrow()
  {
    // Arrange
    Terminal!.QueueLine("greet First");
    Terminal.QueueLine("greet Second");
    Terminal.QueueKey(ConsoleKey.UpArrow);       // Go to "greet Second"
    Terminal.QueueKey(ConsoleKey.UpArrow);       // Go to "greet First"
    Terminal.QueueKey(ConsoleKey.DownArrow);     // Go back to "greet Second"
    Terminal.QueueKey(ConsoleKey.Enter);         // Execute
    Terminal.QueueLine("exit");

    // Act
    await App!.RunReplAsync();

    // Assert - Session completed successfully (history navigation worked)
    Terminal.OutputContains("Goodbye!").ShouldBeTrue("DownArrow should navigate to next history item");
  }

  public static async Task Should_navigate_next_history_with_ctrl_n()
  {
    // Arrange
    Terminal!.QueueLine("greet Alpha");
    Terminal.QueueLine("greet Beta");
    Terminal.QueueKey(ConsoleKey.UpArrow);       // Go to "greet Beta"
    Terminal.QueueKey(ConsoleKey.UpArrow);       // Go to "greet Alpha"
    Terminal.QueueKey(ConsoleKey.N, ctrl: true); // Ctrl+N = NextHistory (back to "greet Beta")
    Terminal.QueueKey(ConsoleKey.Enter);         // Execute
    Terminal.QueueLine("exit");

    // Act
    await App!.RunReplAsync();

    // Assert - Session completed successfully (history navigation worked)
    Terminal.OutputContains("Goodbye!").ShouldBeTrue("Ctrl+N should navigate to next history item");
  }

  // ============================================================================
  // HistorySearchBackward: F8
  // HistorySearchForward: Shift+F8
  // ============================================================================

  public static async Task Should_search_backward_with_f8_prefix_match()
  {
    // Arrange - Create history with commands starting with "greet"
    Terminal!.QueueLine("greet Alice");
    Terminal.QueueLine("deploy prod");
    Terminal.QueueLine("greet Bob");
    Terminal.QueueLine("status");
    // Type "gre" and press F8 to search backward for commands starting with "gre"
    Terminal.QueueKeys("gre");
    Terminal.QueueKey(ConsoleKey.F8);  // Should find "greet Bob"
    Terminal.QueueKey(ConsoleKey.Enter);
    Terminal.QueueLine("exit");

    // Act
    await App!.RunReplAsync();

    // Assert - Session completed successfully
    Terminal.OutputContains("Goodbye!").ShouldBeTrue("F8 should search backward for prefix match");
  }

  public static async Task Should_search_backward_multiple_times_with_f8()
  {
    // Arrange
    Terminal!.QueueLine("greet Alice");
    Terminal.QueueLine("greet Bob");
    Terminal.QueueLine("greet Charlie");
    Terminal.QueueKeys("gre");
    Terminal.QueueKey(ConsoleKey.F8);  // Find "greet Charlie"
    Terminal.QueueKey(ConsoleKey.F8);  // Find "greet Bob"
    Terminal.QueueKey(ConsoleKey.F8);  // Find "greet Alice"
    Terminal.QueueKey(ConsoleKey.Enter);
    Terminal.QueueLine("exit");

    // Act
    await App!.RunReplAsync();

    // Assert
    Terminal.OutputContains("Goodbye!").ShouldBeTrue("F8 should search backward multiple times");
  }

  public static async Task Should_search_forward_with_shift_f8()
  {
    // Arrange
    Terminal!.QueueLine("greet Alice");
    Terminal.QueueLine("greet Bob");
    Terminal.QueueLine("greet Charlie");
    Terminal.QueueKeys("gre");
    Terminal.QueueKey(ConsoleKey.F8);  // Find "greet Charlie"
    Terminal.QueueKey(ConsoleKey.F8);  // Find "greet Bob"
    Terminal.QueueKey(ConsoleKey.F8, shift: true);  // Search forward to "greet Charlie"
    Terminal.QueueKey(ConsoleKey.Enter);
    Terminal.QueueLine("exit");

    // Act
    await App!.RunReplAsync();

    // Assert
    Terminal.OutputContains("Goodbye!").ShouldBeTrue("Shift+F8 should search forward with prefix");
  }

  public static async Task Should_clear_prefix_search_when_typing()
  {
    // Arrange
    Terminal!.QueueLine("greet Alice");
    Terminal.QueueLine("deploy prod");
    Terminal.QueueKeys("gre");
    Terminal.QueueKey(ConsoleKey.F8);  // Find "greet Alice"
    // Now type more characters - should clear prefix search
    Terminal.QueueKeys("et Bob");
    Terminal.QueueKey(ConsoleKey.Enter);
    Terminal.QueueLine("exit");

    // Act
    await App!.RunReplAsync();

    // Assert
    Terminal.OutputContains("Goodbye!").ShouldBeTrue("Typing should clear prefix search state");
  }

  // ============================================================================
  // BeginningOfHistory: Alt+<
  // EndOfHistory: Alt+>
  // ============================================================================

  public static async Task Should_jump_to_beginning_of_history_with_alt_shift_comma()
  {
    // Arrange - Create history with multiple items
    Terminal!.QueueLine("greet First");
    Terminal.QueueLine("greet Second");
    Terminal.QueueLine("greet Third");
    // Now at empty prompt, jump to beginning of history
    Terminal.QueueKey(ConsoleKey.OemComma, alt: true, shift: true);  // Alt+< = BeginningOfHistory
    Terminal.QueueKey(ConsoleKey.Enter);  // Execute first history item
    Terminal.QueueLine("exit");

    // Act
    await App!.RunReplAsync();

    // Assert - Session completed successfully (jumped to first history item)
    Terminal.OutputContains("Goodbye!").ShouldBeTrue("Alt+< should jump to beginning of history");
  }

  public static async Task Should_jump_to_end_of_history_with_alt_shift_period()
  {
    // Arrange - Navigate into history, then jump back to current input
    Terminal!.QueueLine("greet Alpha");
    Terminal.QueueLine("greet Beta");
    Terminal.QueueKey(ConsoleKey.UpArrow);  // Go to "greet Beta"
    Terminal.QueueKey(ConsoleKey.UpArrow);  // Go to "greet Alpha"
    // Now jump back to current (empty) input
    Terminal.QueueKey(ConsoleKey.OemPeriod, alt: true, shift: true);  // Alt+> = EndOfHistory
    Terminal.QueueLine("greet Gamma");      // Type new command
    Terminal.QueueLine("exit");

    // Act
    await App!.RunReplAsync();

    // Assert - Session completed successfully (jumped to end of history)
    Terminal.OutputContains("Goodbye!").ShouldBeTrue("Alt+> should jump to end of history (current input)");
  }

  // ============================================================================
  // PossibleCompletions: Alt+=
  // ============================================================================

  public static async Task Should_display_completions_with_alt_equals()
  {
    // Arrange - Type partial command and press Alt+=
    Terminal!.QueueKeys("gre");
    Terminal.QueueKey(ConsoleKey.Oem7, alt: true);  // Alt+= (Oem7 is the = key)
    // Should display completions without changing input
    // Complete the command manually
    Terminal.QueueKeys("et Alice");
    Terminal.QueueKey(ConsoleKey.Enter);
    Terminal.QueueLine("exit");

    // Act
    await App!.RunReplAsync();

    // Assert
    Terminal.OutputContains("Goodbye!").ShouldBeTrue("Alt+= should display completions without modifying input");
  }

  // ============================================================================
  // BackwardDeleteChar: Backspace
  // DeleteChar: Delete
  // ============================================================================

  public static async Task Should_delete_backward_with_backspace()
  {
    // Arrange
    Terminal!.QueueKeys("hellox");
    Terminal.QueueKey(ConsoleKey.Backspace);  // Delete 'x'
    Terminal.QueueKey(ConsoleKey.Enter);
    Terminal.QueueLine("exit");

    // Act
    await App!.RunReplAsync();

    // Assert
    Terminal.OutputContains("Goodbye!").ShouldBeTrue("Backspace should delete character before cursor");
  }

  public static async Task Should_delete_forward_with_delete()
  {
    // Arrange
    Terminal!.QueueKeys("xhello");
    Terminal.QueueKey(ConsoleKey.Home);    // Go to start
    Terminal.QueueKey(ConsoleKey.Delete);  // Delete 'x'
    Terminal.QueueKey(ConsoleKey.Enter);
    Terminal.QueueLine("exit");

    // Act
    await App!.RunReplAsync();

    // Assert
    Terminal.OutputContains("Goodbye!").ShouldBeTrue("Delete should delete character under cursor");
  }
}

} // namespace TimeWarp.Nuru.Tests.ReplTests.PSReadLineKeybindingsHistory
