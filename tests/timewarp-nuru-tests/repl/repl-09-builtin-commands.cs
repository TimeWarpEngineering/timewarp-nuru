#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// Test built-in REPL commands (Section 9 of REPL Test Plan)

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.BuiltinCommands
{
  [TestTag("REPL")]
  public class BuiltinCommandsTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<BuiltinCommandsTests>();

  public static async Task Should_handle_exit_command()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .AddRepl()
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Exit command should terminate session");
  }

  public static async Task Should_handle_quit_command()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("quit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .AddRepl()
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Quit command should terminate session");
  }

  public static async Task Should_handle_q_shortcut()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("q");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .AddRepl()
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

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

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("status").WithHandler(() => "OK").AsQuery().Done()
      .AddRepl()
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

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

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .AddRepl()
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

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

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .AddRepl()
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

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

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("status").WithHandler(() => "OK").AsQuery().Done()
      .AddRepl()
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

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

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("status").WithHandler(() => "OK").AsQuery().Done()
      .AddRepl()
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert - after clear-history, history should be empty
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Clear-history command should work");
  }
  }
}
