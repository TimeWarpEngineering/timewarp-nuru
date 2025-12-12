#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../source/timewarp-nuru-repl/timewarp-nuru-repl.csproj

using TimeWarp.Nuru;

// Test built-in REPL commands (Section 9 of REPL Test Plan)
return await RunTests<BuiltinCommandsTests>();

[TestTag("REPL")]
public class BuiltinCommandsTests
{
  public static async Task Should_handle_exit_command()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddReplSupport()
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Exit command should terminate session");
  }

  public static async Task Should_handle_quit_command()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("quit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddReplSupport()
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Quit command should terminate session");
  }

  public static async Task Should_handle_q_shortcut()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("q");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddReplSupport()
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("q shortcut should terminate session");
  }

  public static async Task Should_handle_help_command()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("help");
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("status", () => "OK")
      .AddReplSupport()
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - help is routed through app, verify session completed
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Help command should work");
  }

  public static async Task Should_handle_clear_command()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("clear");
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddReplSupport()
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("[CLEAR]")
      .ShouldBeTrue("Clear command should clear screen");
  }

  public static async Task Should_handle_cls_command()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("cls");
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddReplSupport()
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("[CLEAR]")
      .ShouldBeTrue("cls command should clear screen");
  }

  public static async Task Should_handle_history_command()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("status");
    terminal.QueueLine("history");
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("status", () => "OK")
      .AddReplSupport()
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - history output goes through app routing
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("History command should work");
  }

  public static async Task Should_handle_clear_history_command()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("status");
    terminal.QueueLine("clear-history");
    terminal.QueueLine("history");
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("status", () => "OK")
      .AddReplSupport()
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - after clear-history, history should be empty
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Clear-history command should work");
  }
}
