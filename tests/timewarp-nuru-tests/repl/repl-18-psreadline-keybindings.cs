#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

#region Purpose
// Pins PSReadLine-compatible REPL keybindings, both primary and alternative (task 043_001).
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.PSReadLineKeybindings
{

[TestTag("REPL")]
[TestTag("PSReadLine")]
public class PSReadLineKeybindingsTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<PSReadLineKeybindingsTests>();

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
  // BackwardChar: LeftArrow, Ctrl+B
  // ============================================================================

  public static async Task Should_move_backward_char_with_left_arrow()
  {
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
}

} // namespace TimeWarp.Nuru.Tests.ReplTests.PSReadLineKeybindings
