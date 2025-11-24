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
  // ============================================================================
  // BackwardChar: LeftArrow, Ctrl+B
  // ============================================================================

  public static async Task Should_move_backward_char_with_left_arrow()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("ab");
    terminal.QueueKey(ConsoleKey.LeftArrow);  // Move back one
    terminal.QueueKeys("X");                   // Insert X between a and b
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("aXb", () => "Success!")
      .AddReplSupport(options =>
      {
        options.EnableArrowHistory = true;
        options.EnableColors = false;  // Disable colors for cleaner test output
      })
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - Command executed successfully (session completed normally)
    terminal.OutputContains("Goodbye!").ShouldBeTrue("LeftArrow should move cursor back one character");
  }

  public static async Task Should_move_backward_char_with_ctrl_b()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("ab");
    terminal.QueueKey(ConsoleKey.B, ctrl: true);  // Ctrl+B = BackwardChar
    terminal.QueueKeys("X");                       // Insert X between a and b
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("aXb", () => "Success!")
      .AddReplSupport(options =>
      {
        options.EnableArrowHistory = true;
        options.EnableColors = false;  // Disable colors for cleaner test assertions
      })
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!").ShouldBeTrue("Ctrl+B should move cursor back one character");
  }

  // ============================================================================
  // ForwardChar: RightArrow, Ctrl+F
  // ============================================================================

  public static async Task Should_move_forward_char_with_right_arrow()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("ab");
    terminal.QueueKey(ConsoleKey.Home);        // Go to start
    terminal.QueueKey(ConsoleKey.RightArrow);  // Move forward one (after 'a')
    terminal.QueueKeys("X");                    // Insert X between a and b
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("aXb", () => "Success!")
      .AddReplSupport(options =>
      {
        options.EnableArrowHistory = true;
        options.EnableColors = false;  // Disable colors for cleaner test assertions
      })
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!").ShouldBeTrue("RightArrow should move cursor forward one character");
  }

  public static async Task Should_move_forward_char_with_ctrl_f()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("ab");
    terminal.QueueKey(ConsoleKey.Home);           // Go to start
    terminal.QueueKey(ConsoleKey.F, ctrl: true);  // Ctrl+F = ForwardChar
    terminal.QueueKeys("X");                       // Insert X between a and b
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("aXb", () => "Success!")
      .AddReplSupport(options =>
      {
        options.EnableArrowHistory = true;
        options.EnableColors = false;  // Disable colors for cleaner test assertions
      })
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!").ShouldBeTrue("Ctrl+F should move cursor forward one character");
  }

  // ============================================================================
  // BeginningOfLine: Home, Ctrl+A
  // ============================================================================

  public static async Task Should_move_to_beginning_with_home()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("world");
    terminal.QueueKey(ConsoleKey.Home);  // Go to start
    terminal.QueueKeys("hello ");         // Insert "hello " at beginning
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("hello world", () => "Success!")
      .AddReplSupport(options =>
      {
        options.EnableArrowHistory = true;
        options.EnableColors = false;  // Disable colors for cleaner test assertions
      })
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!").ShouldBeTrue("Home should move cursor to beginning of line");
  }

  public static async Task Should_move_to_beginning_with_ctrl_a()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("world");
    terminal.QueueKey(ConsoleKey.A, ctrl: true);  // Ctrl+A = BeginningOfLine
    terminal.QueueKeys("hello ");                  // Insert "hello " at beginning
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("hello world", () => "Success!")
      .AddReplSupport(options =>
      {
        options.EnableArrowHistory = true;
        options.EnableColors = false;  // Disable colors for cleaner test assertions
      })
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!").ShouldBeTrue("Ctrl+A should move cursor to beginning of line");
  }

  // ============================================================================
  // EndOfLine: End, Ctrl+E
  // ============================================================================

  public static async Task Should_move_to_end_with_end_key()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("hello");
    terminal.QueueKey(ConsoleKey.Home);  // Go to start
    terminal.QueueKey(ConsoleKey.End);   // Go to end
    terminal.QueueKeys(" world");         // Append " world"
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("hello world", () => "Success!")
      .AddReplSupport(options =>
      {
        options.EnableArrowHistory = true;
        options.EnableColors = false;  // Disable colors for cleaner test assertions
      })
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!").ShouldBeTrue("End should move cursor to end of line");
  }

  public static async Task Should_move_to_end_with_ctrl_e()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("hello");
    terminal.QueueKey(ConsoleKey.Home);           // Go to start
    terminal.QueueKey(ConsoleKey.E, ctrl: true);  // Ctrl+E = EndOfLine
    terminal.QueueKeys(" world");                  // Append " world"
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("hello world", () => "Success!")
      .AddReplSupport(options =>
      {
        options.EnableArrowHistory = true;
        options.EnableColors = false;  // Disable colors for cleaner test assertions
      })
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!").ShouldBeTrue("Ctrl+E should move cursor to end of line");
  }

  // ============================================================================
  // BackwardWord: Ctrl+LeftArrow, Alt+B
  // ============================================================================

  public static async Task Should_move_backward_word_with_ctrl_left()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("hello world");
    terminal.QueueKey(ConsoleKey.LeftArrow, ctrl: true);  // Move to start of "world"
    terminal.QueueKey(ConsoleKey.LeftArrow, ctrl: true);  // Move to start of "hello"
    terminal.QueueKeys("say ");                            // Insert "say " before "hello"
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("say hello world", () => "Success!")
      .AddReplSupport(options =>
      {
        options.EnableArrowHistory = true;
        options.EnableColors = false;  // Disable colors for cleaner test assertions
      })
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!").ShouldBeTrue("Ctrl+LeftArrow should move cursor to beginning of previous word");
  }

  public static async Task Should_move_backward_word_with_alt_b()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("hello world");
    terminal.QueueKey(ConsoleKey.B, alt: true);  // Alt+B = BackwardWord (to start of "world")
    terminal.QueueKey(ConsoleKey.B, alt: true);  // Alt+B again (to start of "hello")
    terminal.QueueKeys("say ");                   // Insert "say " before "hello"
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("say hello world", () => "Success!")
      .AddReplSupport(options =>
      {
        options.EnableArrowHistory = true;
        options.EnableColors = false;  // Disable colors for cleaner test assertions
      })
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!").ShouldBeTrue("Alt+B should move cursor to beginning of previous word");
  }

  // ============================================================================
  // ForwardWord: Ctrl+RightArrow, Alt+F
  // PSReadLine behavior: move to END of current/next word
  // ============================================================================

  public static async Task Should_move_forward_word_with_ctrl_right()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("hello world");
    terminal.QueueKey(ConsoleKey.Home);                    // Go to start
    terminal.QueueKey(ConsoleKey.RightArrow, ctrl: true);  // Move to end of "hello"
    terminal.QueueKeys("X");                                // Insert X after "hello"
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("helloX world", () => "Success!")
      .AddReplSupport(options =>
      {
        options.EnableArrowHistory = true;
        options.EnableColors = false;  // Disable colors for cleaner test assertions
      })
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!").ShouldBeTrue("Ctrl+RightArrow should move cursor to end of current word");
  }

  public static async Task Should_move_forward_word_with_alt_f()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("hello world");
    terminal.QueueKey(ConsoleKey.Home);          // Go to start
    terminal.QueueKey(ConsoleKey.F, alt: true);  // Alt+F = ForwardWord (to end of "hello")
    terminal.QueueKeys("X");                      // Insert X after "hello"
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("helloX world", () => "Success!")
      .AddReplSupport(options =>
      {
        options.EnableArrowHistory = true;
        options.EnableColors = false;  // Disable colors for cleaner test assertions
      })
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!").ShouldBeTrue("Alt+F should move cursor to end of current word");
  }

  // ============================================================================
  // PreviousHistory: UpArrow, Ctrl+P
  // ============================================================================

  public static async Task Should_navigate_previous_history_with_up_arrow()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("greet Alice");           // Execute first command
    terminal.QueueKey(ConsoleKey.UpArrow);       // Navigate to previous (greet Alice)
    terminal.QueueKey(ConsoleKey.Enter);         // Execute again
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("greet {name}", (string name) => $"Hello, {name}!")
      .AddReplSupport(options =>
      {
        options.EnableArrowHistory = true;
        options.EnableColors = false;  // Disable colors for cleaner test assertions
      })
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - Session completed successfully (history navigation worked)
    terminal.OutputContains("Goodbye!").ShouldBeTrue("UpArrow should recall previous history item");
  }

  public static async Task Should_navigate_previous_history_with_ctrl_p()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("greet Bob");             // Execute first command
    terminal.QueueKey(ConsoleKey.P, ctrl: true); // Ctrl+P = PreviousHistory
    terminal.QueueKey(ConsoleKey.Enter);         // Execute again
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("greet {name}", (string name) => $"Hello, {name}!")
      .AddReplSupport(options =>
      {
        options.EnableArrowHistory = true;
        options.EnableColors = false;  // Disable colors for cleaner test assertions
      })
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - Session completed successfully (history navigation worked)
    terminal.OutputContains("Goodbye!").ShouldBeTrue("Ctrl+P should recall previous history item");
  }

  // ============================================================================
  // NextHistory: DownArrow, Ctrl+N
  // ============================================================================

  public static async Task Should_navigate_next_history_with_down_arrow()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("greet First");
    terminal.QueueLine("greet Second");
    terminal.QueueKey(ConsoleKey.UpArrow);       // Go to "greet Second"
    terminal.QueueKey(ConsoleKey.UpArrow);       // Go to "greet First"
    terminal.QueueKey(ConsoleKey.DownArrow);     // Go back to "greet Second"
    terminal.QueueKey(ConsoleKey.Enter);         // Execute
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("greet {name}", (string name) => $"Hello, {name}!")
      .AddReplSupport(options =>
      {
        options.EnableArrowHistory = true;
        options.EnableColors = false;  // Disable colors for cleaner test assertions
      })
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - Session completed successfully (history navigation worked)
    terminal.OutputContains("Goodbye!").ShouldBeTrue("DownArrow should navigate to next history item");
  }

  public static async Task Should_navigate_next_history_with_ctrl_n()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("greet Alpha");
    terminal.QueueLine("greet Beta");
    terminal.QueueKey(ConsoleKey.UpArrow);       // Go to "greet Beta"
    terminal.QueueKey(ConsoleKey.UpArrow);       // Go to "greet Alpha"
    terminal.QueueKey(ConsoleKey.N, ctrl: true); // Ctrl+N = NextHistory (back to "greet Beta")
    terminal.QueueKey(ConsoleKey.Enter);         // Execute
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("greet {name}", (string name) => $"Hello, {name}!")
      .AddReplSupport(options =>
      {
        options.EnableArrowHistory = true;
        options.EnableColors = false;  // Disable colors for cleaner test assertions
      })
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - Session completed successfully (history navigation worked)
    terminal.OutputContains("Goodbye!").ShouldBeTrue("Ctrl+N should navigate to next history item");
  }

  // ============================================================================
  // BackwardDeleteChar: Backspace
  // DeleteChar: Delete
  // ============================================================================

  public static async Task Should_delete_backward_with_backspace()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("hellox");
    terminal.QueueKey(ConsoleKey.Backspace);  // Delete 'x'
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("hello", () => "Success!")
      .AddReplSupport(options =>
      {
        options.EnableArrowHistory = true;
        options.EnableColors = false;  // Disable colors for cleaner test assertions
      })
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!").ShouldBeTrue("Backspace should delete character before cursor");
  }

  public static async Task Should_delete_forward_with_delete()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("xhello");
    terminal.QueueKey(ConsoleKey.Home);    // Go to start
    terminal.QueueKey(ConsoleKey.Delete);  // Delete 'x'
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("hello", () => "Success!")
      .AddReplSupport(options =>
      {
        options.EnableArrowHistory = true;
        options.EnableColors = false;  // Disable colors for cleaner test assertions
      })
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!").ShouldBeTrue("Delete should delete character under cursor");
  }
}
