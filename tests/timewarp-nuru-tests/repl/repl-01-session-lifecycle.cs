#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.SessionLifecycle
{
  [TestTag("REPL")]
  public class SessionLifecycleTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<SessionLifecycleTests>();

  public static async Task Should_start_session_and_display_welcome_message()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .AddRepl()
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("TimeWarp.Nuru REPL Mode")
      .ShouldBeTrue("Welcome message should be displayed");
    terminal.OutputContains("Type 'help' for commands")
      .ShouldBeTrue("Help hint should be in welcome message");
  }

  public static async Task Should_display_custom_welcome_message()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .AddRepl(options => options.WelcomeMessage = "Custom Welcome!")
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Custom Welcome!")
      .ShouldBeTrue("Custom welcome message should be displayed");
  }

  public static async Task Should_exit_cleanly_via_exit_command()
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
      .ShouldBeTrue("Goodbye message should be displayed on exit");
  }

  public static async Task Should_exit_via_quit_command()
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
      .ShouldBeTrue("Goodbye message should be displayed on quit");
  }

  public static async Task Should_exit_via_q_shortcut()
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
      .ShouldBeTrue("Goodbye message should be displayed on q");
  }

  public static async Task Should_display_custom_goodbye_message()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .AddRepl(options => options.GoodbyeMessage = "See you later!")
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("See you later!")
      .ShouldBeTrue("Custom goodbye message should be displayed");
  }

  public static async Task Should_execute_command_and_continue()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("greet Alice");
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("greet {name}")
        .WithHandler((string name) => $"Hello, {name}!")
        .AsCommand()
        .Done()
      .AddRepl()
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert - Command output goes through IConsole (not ITerminal), but session control uses ITerminal
    // We verify session continued by checking goodbye message was displayed via terminal
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Session should continue after command and exit cleanly");
  }

  public static async Task Should_execute_multiple_commands()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("greet Alice");
    terminal.QueueLine("greet Bob");
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("greet {name}")
        .WithHandler((string name) => $"Hello, {name}!")
        .AsCommand()
        .Done()
      .AddRepl()
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert - Verify session processed both commands and exited cleanly
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Session should execute multiple commands and exit cleanly");
  }

  public static async Task Should_show_timing_when_enabled()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("greet World");
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("greet {name}")
        .WithHandler((string name) => $"Hello, {name}!")
        .AsCommand()
        .Done()
      .AddRepl(options => options.ShowTiming = true)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("ms)")
      .ShouldBeTrue("Timing information should be displayed when enabled");
  }

  public static async Task Should_not_show_timing_when_disabled()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("greet World");
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("greet {name}")
        .WithHandler((string name) => $"Hello, {name}!")
        .AsCommand()
        .Done()
      .AddRepl(options => options.ShowTiming = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("ms)")
      .ShouldBeFalse("Timing information should NOT be displayed when disabled");
  }

  public static async Task Should_show_help_command()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("help");
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("greet {name}")
        .WithHandler((string name) => $"Hello, {name}!")
        .AsCommand()
        .Done()
      .AddRepl()
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert - help command output goes through app routing (IConsole)
    // Verify session processed the help command and exited cleanly via terminal
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Session should process help and exit cleanly");
  }
  }
}
