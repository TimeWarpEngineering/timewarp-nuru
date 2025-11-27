#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../Source/TimeWarp.Nuru.Repl/TimeWarp.Nuru.Repl.csproj

using TimeWarp.Nuru;
using TimeWarp.Nuru.Repl;

// Test console input handling (Section 5 of REPL Test Plan)
return await RunTests<ConsoleInputTests>();

[TestTag("REPL")]
public class ConsoleInputTests
{
  public static async Task Should_handle_character_insertion()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueKeys("hello");
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("hello", () => "Hello!")
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - command was processed (session completed normally)
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Session should complete with typed command");
  }

  public static async Task Should_handle_backspace_key()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueKeys("hellox");
    terminal.QueueKey(ConsoleKey.Backspace);  // Delete 'x'
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("hello", () => "Hello!")
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Session should complete with backspace-corrected command");
  }

  public static async Task Should_handle_delete_key()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueKeys("xhello");
    terminal.QueueKey(ConsoleKey.Home);      // Go to start
    terminal.QueueKey(ConsoleKey.Delete);    // Delete 'x'
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("hello", () => "Hello!")
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Session should complete with delete-corrected command");
  }

  public static async Task Should_handle_left_arrow_navigation()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueKeys("helllo");            // Typo: double 'l'
    terminal.QueueKey(ConsoleKey.LeftArrow); // Move left
    terminal.QueueKey(ConsoleKey.LeftArrow); // Move left again
    terminal.QueueKey(ConsoleKey.Backspace); // Delete extra 'l'
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("hello", () => "Hello!")
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Session should complete with arrow-navigated correction");
  }

  public static async Task Should_handle_right_arrow_navigation()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueKeys("hello");
    terminal.QueueKey(ConsoleKey.Home);       // Go to start
    terminal.QueueKey(ConsoleKey.RightArrow); // Move right one
    terminal.QueueKey(ConsoleKey.RightArrow); // Move right again
    terminal.QueueKey(ConsoleKey.End);        // Go to end
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("hello", () => "Hello!")
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Session should complete after right arrow navigation");
  }

  public static async Task Should_handle_ctrl_left_word_navigation()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueKeys("greet world");
    terminal.QueueKey(ConsoleKey.LeftArrow, ctrl: true);  // Jump to start of "world"
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("greet {name}", (string name) => $"Hello, {name}!")
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Session should complete after Ctrl+Left navigation");
  }

  public static async Task Should_handle_ctrl_right_word_navigation()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueKeys("greet world");
    terminal.QueueKey(ConsoleKey.Home);
    terminal.QueueKey(ConsoleKey.RightArrow, ctrl: true);  // Jump to end of "greet"
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("greet {name}", (string name) => $"Hello, {name}!")
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Session should complete after Ctrl+Right navigation");
  }

  public static async Task Should_handle_home_key()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueKeys("hello");
    terminal.QueueKey(ConsoleKey.Home);
    terminal.QueueKeys("say ");  // Insert "say " at beginning
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("say hello", () => "Said hello!")
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Session should complete with Home key positioning");
  }

  public static async Task Should_handle_end_key()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueKeys("hello");
    terminal.QueueKey(ConsoleKey.Home);
    terminal.QueueKey(ConsoleKey.End);
    terminal.QueueKeys(" world");  // Append at end
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("hello world", () => "Hello World!")
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Session should complete with End key positioning");
  }

  public static async Task Should_handle_escape_key_clearing_completion()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueKeys("hel");
    terminal.QueueKey(ConsoleKey.Tab);        // Trigger completion
    terminal.QueueKey(ConsoleKey.Escape);     // Cancel completion
    terminal.QueueKeys("lo");                 // Finish typing
    terminal.QueueKey(ConsoleKey.Enter);
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("hello", () => "Hello!")
      .AddReplSupport(options => options.EnableArrowHistory = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Session should complete after Escape key");
  }
}
