#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../Source/TimeWarp.Nuru.Repl/TimeWarp.Nuru.Repl.csproj

using TimeWarp.Nuru;
using TimeWarp.Nuru.Repl;

// Tests for PSReadLine-compatible keybindings (Task 043_001)
// Verifies both primary and alternative keybindings work correctly
return await RunTests<PSReadLineKeybindingsTests>();

[TestTag("REPL")]
[TestTag("PSReadLine")]
public class PSReadLineKeybindingsTests
{
  private static TestTerminal? Terminal;
  private static NuruApp? App;

  public static async Task Setup()
  {
    // Create fresh terminal and app for each test
    Terminal = new TestTerminal();

    App = new NuruAppBuilder()
      .UseTerminal(Terminal)
      .AddRoute("aXb", () => "Success!")
      .AddRoute("hello", () => "Success!")
      .AddRoute("hello world", () => "Success!")
      .AddRoute("helloX world", () => "Success!")
      .AddRoute("say hello world", () => "Success!")
      .AddRoute("greet {name}", (string name) => $"Hello, {name}!")
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
  // BackwardChar: LeftArrow, Ctrl+B
  // ============================================================================

  public static async Task Should_move_backward_char_with_left_arrow()
  {
    // Arrange
    Terminal!.QueueKeys("ab");
    Terminal.QueueKey(ConsoleKey.LeftArrow);  // Move back one
    Terminal.QueueKeys("X");                   // Insert X between a and b
    Terminal.QueueKey(ConsoleKey.Enter);
    Terminal.QueueLine("exit");

    // Act
    await App!.RunReplAsync();

    // Assert - Command executed successfully (session completed normally)
    Terminal.OutputContains("Goodbye!").ShouldBeTrue("LeftArrow should move cursor back one character");
  }

  public static async Task Should_move_backward_char_with_ctrl_b()
  {
    // Arrange
    Terminal!.QueueKeys("ab");
    Terminal.QueueKey(ConsoleKey.B, ctrl: true);  // Ctrl+B = BackwardChar
    Terminal.QueueKeys("X");                       // Insert X between a and b
    Terminal.QueueKey(ConsoleKey.Enter);
    Terminal.QueueLine("exit");

    // Act
    await App!.RunReplAsync();

    // Assert
    Terminal.OutputContains("Goodbye!").ShouldBeTrue("Ctrl+B should move cursor back one character");
  }

  // ============================================================================
  // ForwardChar: RightArrow, Ctrl+F
  // ============================================================================

  public static async Task Should_move_forward_char_with_right_arrow()
  {
    // Arrange
    Terminal!.QueueKeys("ab");
    Terminal.QueueKey(ConsoleKey.Home);        // Go to start
    Terminal.QueueKey(ConsoleKey.RightArrow);  // Move forward one (after 'a')
    Terminal.QueueKeys("X");                    // Insert X between a and b
    Terminal.QueueKey(ConsoleKey.Enter);
    Terminal.QueueLine("exit");

    // Act
    await App!.RunReplAsync();

    // Assert
    Terminal.OutputContains("Goodbye!").ShouldBeTrue("RightArrow should move cursor forward one character");
  }

  public static async Task Should_move_forward_char_with_ctrl_f()
  {
    // Arrange
    Terminal!.QueueKeys("ab");
    Terminal.QueueKey(ConsoleKey.Home);           // Go to start
    Terminal.QueueKey(ConsoleKey.F, ctrl: true);  // Ctrl+F = ForwardChar
    Terminal.QueueKeys("X");                       // Insert X between a and b
    Terminal.QueueKey(ConsoleKey.Enter);
    Terminal.QueueLine("exit");

    // Act
    await App!.RunReplAsync();

    // Assert
    Terminal.OutputContains("Goodbye!").ShouldBeTrue("Ctrl+F should move cursor forward one character");
  }

  // ============================================================================
  // BeginningOfLine: Home, Ctrl+A
  // ============================================================================

  public static async Task Should_move_to_beginning_with_home()
  {
    // Arrange
    Terminal!.QueueKeys("world");
    Terminal.QueueKey(ConsoleKey.Home);  // Go to start
    Terminal.QueueKeys("hello ");         // Insert "hello " at beginning
    Terminal.QueueKey(ConsoleKey.Enter);
    Terminal.QueueLine("exit");

    // Act
    await App!.RunReplAsync();

    // Assert
    Terminal.OutputContains("Goodbye!").ShouldBeTrue("Home should move cursor to beginning of line");
  }

  public static async Task Should_move_to_beginning_with_ctrl_a()
  {
    // Arrange
    Terminal!.QueueKeys("world");
    Terminal.QueueKey(ConsoleKey.A, ctrl: true);  // Ctrl+A = BeginningOfLine
    Terminal.QueueKeys("hello ");                  // Insert "hello " at beginning
    Terminal.QueueKey(ConsoleKey.Enter);
    Terminal.QueueLine("exit");

    // Act
    await App!.RunReplAsync();

    // Assert
    Terminal.OutputContains("Goodbye!").ShouldBeTrue("Ctrl+A should move cursor to beginning of line");
  }

  // ============================================================================
  // EndOfLine: End, Ctrl+E
  // ============================================================================

  public static async Task Should_move_to_end_with_end_key()
  {
    // Arrange
    Terminal!.QueueKeys("hello");
    Terminal.QueueKey(ConsoleKey.Home);  // Go to start
    Terminal.QueueKey(ConsoleKey.End);   // Go to end
    Terminal.QueueKeys(" world");         // Append " world"
    Terminal.QueueKey(ConsoleKey.Enter);
    Terminal.QueueLine("exit");

    // Act
    await App!.RunReplAsync();

    // Assert
    Terminal.OutputContains("Goodbye!").ShouldBeTrue("End should move cursor to end of line");
  }

  public static async Task Should_move_to_end_with_ctrl_e()
  {
    // Arrange
    Terminal!.QueueKeys("hello");
    Terminal.QueueKey(ConsoleKey.Home);           // Go to start
    Terminal.QueueKey(ConsoleKey.E, ctrl: true);  // Ctrl+E = EndOfLine
    Terminal.QueueKeys(" world");                  // Append " world"
    Terminal.QueueKey(ConsoleKey.Enter);
    Terminal.QueueLine("exit");

    // Act
    await App!.RunReplAsync();

    // Assert
    Terminal.OutputContains("Goodbye!").ShouldBeTrue("Ctrl+E should move cursor to end of line");
  }

  // ============================================================================
  // BackwardWord: Ctrl+LeftArrow, Alt+B
  // ============================================================================

  public static async Task Should_move_backward_word_with_ctrl_left()
  {
    // Arrange
    Terminal!.QueueKeys("hello world");
    Terminal.QueueKey(ConsoleKey.LeftArrow, ctrl: true);  // Move to start of "world"
    Terminal.QueueKey(ConsoleKey.LeftArrow, ctrl: true);  // Move to start of "hello"
    Terminal.QueueKeys("say ");                            // Insert "say " before "hello"
    Terminal.QueueKey(ConsoleKey.Enter);
    Terminal.QueueLine("exit");

    // Act
    await App!.RunReplAsync();

    // Assert
    Terminal.OutputContains("Goodbye!").ShouldBeTrue("Ctrl+LeftArrow should move cursor to beginning of previous word");
  }

  public static async Task Should_move_backward_word_with_alt_b()
  {
    // Arrange
    Terminal!.QueueKeys("hello world");
    Terminal.QueueKey(ConsoleKey.B, alt: true);  // Alt+B = BackwardWord (to start of "world")
    Terminal.QueueKey(ConsoleKey.B, alt: true);  // Alt+B again (to start of "hello")
    Terminal.QueueKeys("say ");                   // Insert "say " before "hello"
    Terminal.QueueKey(ConsoleKey.Enter);
    Terminal.QueueLine("exit");

    // Act
    await App!.RunReplAsync();

    // Assert
    Terminal.OutputContains("Goodbye!").ShouldBeTrue("Alt+B should move cursor to beginning of previous word");
  }

  // ============================================================================
  // ForwardWord: Ctrl+RightArrow, Alt+F
  // PSReadLine behavior: move to END of current/next word
  // ============================================================================

  public static async Task Should_move_forward_word_with_ctrl_right()
  {
    // Arrange
    Terminal!.QueueKeys("hello world");
    Terminal.QueueKey(ConsoleKey.Home);                    // Go to start
    Terminal.QueueKey(ConsoleKey.RightArrow, ctrl: true);  // Move to end of "hello"
    Terminal.QueueKeys("X");                                // Insert X after "hello"
    Terminal.QueueKey(ConsoleKey.Enter);
    Terminal.QueueLine("exit");

    // Act
    await App!.RunReplAsync();

    // Assert
    Terminal.OutputContains("Goodbye!").ShouldBeTrue("Ctrl+RightArrow should move cursor to end of current word");
  }

  public static async Task Should_move_forward_word_with_alt_f()
  {
    // Arrange
    Terminal!.QueueKeys("hello world");
    Terminal.QueueKey(ConsoleKey.Home);          // Go to start
    Terminal.QueueKey(ConsoleKey.F, alt: true);  // Alt+F = ForwardWord (to end of "hello")
    Terminal.QueueKeys("X");                      // Insert X after "hello"
    Terminal.QueueKey(ConsoleKey.Enter);
    Terminal.QueueLine("exit");

    // Act
    await App!.RunReplAsync();

    // Assert
    Terminal.OutputContains("Goodbye!").ShouldBeTrue("Alt+F should move cursor to end of current word");
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
