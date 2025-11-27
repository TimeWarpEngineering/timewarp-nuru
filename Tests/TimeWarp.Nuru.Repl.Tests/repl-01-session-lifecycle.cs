#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../Source/TimeWarp.Nuru.Repl/TimeWarp.Nuru.Repl.csproj

using TimeWarp.Nuru;
using TimeWarp.Nuru.Repl;

return await RunTests<SessionLifecycleTests>(clearCache: true);

[TestTag("REPL")]
[ClearRunfileCache]
public class SessionLifecycleTests
{
  public static async Task Should_start_session_and_display_welcome_message()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddReplSupport()
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
    using var terminal = new TestTerminal();
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddReplSupport(options => options.WelcomeMessage = "Custom Welcome!")
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Custom Welcome!")
      .ShouldBeTrue("Custom welcome message should be displayed");
  }

  public static async Task Should_exit_cleanly_via_exit_command()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddReplSupport()
      .Build();

    // Act
    int exitCode = await app.RunReplAsync();

    // Assert
    exitCode.ShouldBe(0, "Exit code should be 0 for clean exit");
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Goodbye message should be displayed on exit");
  }

  public static async Task Should_exit_via_quit_command()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueLine("quit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddReplSupport()
      .Build();

    // Act
    int exitCode = await app.RunReplAsync();

    // Assert
    exitCode.ShouldBe(0, "Exit code should be 0 for quit command");
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Goodbye message should be displayed on quit");
  }

  public static async Task Should_exit_via_q_shortcut()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueLine("q");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddReplSupport()
      .Build();

    // Act
    int exitCode = await app.RunReplAsync();

    // Assert
    exitCode.ShouldBe(0, "Exit code should be 0 for q shortcut");
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Goodbye message should be displayed on q");
  }

  public static async Task Should_display_custom_goodbye_message()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddReplSupport(options => options.GoodbyeMessage = "See you later!")
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("See you later!")
      .ShouldBeTrue("Custom goodbye message should be displayed");
  }

  public static async Task Should_execute_command_and_continue()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueLine("greet Alice");
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("greet {name}", (string name) => $"Hello, {name}!")
      .AddReplSupport()
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - Command output goes through IConsole (not ITerminal), but session control uses ITerminal
    // We verify session continued by checking goodbye message was displayed via terminal
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Session should continue after command and exit cleanly");
  }

  public static async Task Should_execute_multiple_commands()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueLine("greet Alice");
    terminal.QueueLine("greet Bob");
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("greet {name}", (string name) => $"Hello, {name}!")
      .AddReplSupport()
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - Verify session processed both commands and exited cleanly
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Session should execute multiple commands and exit cleanly");
  }

  public static async Task Should_show_timing_when_enabled()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueLine("greet World");
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("greet {name}", (string name) => $"Hello, {name}!")
      .AddReplSupport(options => options.ShowTiming = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("ms)")
      .ShouldBeTrue("Timing information should be displayed when enabled");
  }

  public static async Task Should_not_show_timing_when_disabled()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueLine("greet World");
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("greet {name}", (string name) => $"Hello, {name}!")
      .AddReplSupport(options => options.ShowTiming = false)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("ms)")
      .ShouldBeFalse("Timing information should NOT be displayed when disabled");
  }

  public static async Task Should_show_help_command()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueLine("help");
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("greet {name}", (string name) => $"Hello, {name}!")
      .AddReplSupport()
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - help command output goes through app routing (IConsole)
    // Verify session processed the help command and exited cleanly via terminal
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Session should process help and exit cleanly");
  }
}
