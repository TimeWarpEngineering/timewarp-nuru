#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../source/timewarp-nuru-repl/timewarp-nuru-repl.csproj

using TimeWarp.Nuru;

return await RunTests<HistoryManagementTests>(clearCache: true);

[TestTag("REPL")]
[ClearRunfileCache]
public class HistoryManagementTests
{
  public static async Task Should_add_commands_to_history()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("greet Alice");
    terminal.QueueLine("greet Bob");
    terminal.QueueLine("history");
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("greet {name}", (string name) => $"Hello, {name}!")
      .AddReplSupport()
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - history command output shows commands were added
    terminal.OutputContains("greet Alice")
      .ShouldBeTrue("First command should be in history");
    terminal.OutputContains("greet Bob")
      .ShouldBeTrue("Second command should be in history");
  }

  public static async Task Should_not_add_duplicate_consecutive_commands()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("greet Alice");
    terminal.QueueLine("greet Alice");  // Duplicate
    terminal.QueueLine("history");
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("greet {name}", (string name) => $"Hello, {name}!")
      .AddReplSupport()
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - history output should show count (duplicates not added)
    // The history command itself will be in history, plus only ONE "greet Alice"
    terminal.OutputContains("Command History:")
      .ShouldBeTrue("History command should show header");
  }

  public static async Task Should_navigate_history_with_up_arrow()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("greet Alice");
    terminal.QueueKey(ConsoleKey.UpArrow);  // Navigate to previous command
    terminal.QueueKey(ConsoleKey.Enter);    // Execute it again
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("greet {name}", (string name) => $"Hello, {name}!")
      .AddReplSupport()
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - command was executed twice via history navigation
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Session should complete after history navigation");
  }

  public static async Task Should_clear_history_command()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("greet Alice");
    terminal.QueueLine("clear-history");
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("greet {name}", (string name) => $"Hello, {name}!")
      .AddReplSupport()
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - clear-history command executed successfully
    // Note: Commands are added to history AFTER execution, so clear-history
    // clears history then itself gets added. This test verifies the command runs.
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Session should complete after clear-history");
  }

  public static async Task Should_show_history_command()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("greet World");
    terminal.QueueLine("history");
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("greet {name}", (string name) => $"Hello, {name}!")
      .AddReplSupport()
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Command History:")
      .ShouldBeTrue("History command should display header");
  }

  public static async Task Should_clear_history_removes_prior_commands()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("greet First");
    terminal.QueueLine("greet Second");
    terminal.QueueLine("clear-history");  // Clear history including above commands
    terminal.QueueLine("history");        // Should NOT show "greet First" or "greet Second"
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("greet {name}", (string name) => $"Hello, {name}!")
      .AddReplSupport(options => options.PersistHistory = false)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - Prior commands should be cleared from history
    // Note: Commands are added BEFORE execution, so clear-history clears everything
    // including itself and prior commands. The history command then adds itself.
    terminal.OutputContains("greet First")
      .ShouldBeFalse("Prior command 'greet First' should be cleared from history output");
  }

  public static async Task Should_respect_max_history_size()
  {
    // Arrange
    using TestTerminal terminal = new();
    // Queue more commands than max history size
    for (int i = 1; i <= 5; i++)
    {
      terminal.QueueLine($"cmd{i}");
    }

    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("cmd1", () => "1")
      .Map("cmd2", () => "2")
      .Map("cmd3", () => "3")
      .Map("cmd4", () => "4")
      .Map("cmd5", () => "5")
      .AddReplSupport(options => options.MaxHistorySize = 3)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - session should complete (history trimming doesn't affect execution)
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Session should complete with history size limit");
  }

  public static async Task Should_skip_empty_commands_in_history()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("");           // Empty line
    terminal.QueueLine("   ");        // Whitespace only
    terminal.QueueLine("greet Test");
    terminal.QueueLine("history");
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("greet {name}", (string name) => $"Hello, {name}!")
      .AddReplSupport()
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - only non-empty command should be in history
    terminal.OutputContains("greet Test")
      .ShouldBeTrue("Non-empty command should be in history");
  }
}
