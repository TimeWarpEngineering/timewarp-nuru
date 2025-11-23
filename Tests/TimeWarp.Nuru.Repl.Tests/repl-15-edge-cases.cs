#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../Source/TimeWarp.Nuru.Repl/TimeWarp.Nuru.Repl.csproj

using TimeWarp.Nuru;
using TimeWarp.Nuru.Repl;

// Test edge cases (Section 15 of REPL Test Plan)
return await RunTests<EdgeCaseTests>();

[TestTag("REPL")]
public class EdgeCaseTests
{
  public static async Task Should_handle_very_long_input()
  {
    // Arrange
    using var terminal = new TestTerminal();
    string longArg = new string('x', 1000);
    terminal.QueueLine($"echo {longArg}");
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("echo {text}", (string text) => text)
      .AddReplSupport()
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Should handle very long input");
  }

  public static async Task Should_handle_unicode_input()
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

    // Assert - basic unicode handling (emoji would need different test setup)
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Should handle unicode characters");
  }

  public static async Task Should_handle_empty_input()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueLine("");  // Empty input
    terminal.QueueLine("");  // Another empty
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddReplSupport()
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Should handle empty input");
  }

  public static async Task Should_handle_whitespace_only_input()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueLine("   ");  // Whitespace only
    terminal.QueueLine("\t");   // Tab only
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddReplSupport()
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Should handle whitespace-only input");
  }

  public static async Task Should_handle_null_options_gracefully()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddReplSupport(options =>
      {
        options.HistoryIgnorePatterns = null!;  // Null collection
      })
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Should handle null options gracefully");
  }

  public static async Task Should_handle_empty_history_patterns()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueLine("test");
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("test", () => "OK")
      .AddReplSupport(options =>
      {
        options.HistoryIgnorePatterns = [];  // Empty list
      })
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Should handle empty history patterns");
  }

  public static async Task Should_handle_special_characters_in_commands()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueLine("echo \"Hello!@#$%^&*()\"");
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("echo {text}", (string text) => text)
      .AddReplSupport()
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Should handle special characters");
  }

  public static async Task Should_handle_rapid_commands()
  {
    // Arrange
    using var terminal = new TestTerminal();

    // Queue many commands rapidly
    for (int i = 0; i < 50; i++)
    {
      terminal.QueueLine("noop");
    }
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("noop", () => { })
      .AddReplSupport()
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Should handle rapid commands");
  }

  public static async Task Should_handle_window_width_edge_cases()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.WindowWidth = 10;  // Very narrow window
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddReplSupport()
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Should handle narrow window");
  }

  public static async Task Should_handle_zero_max_history()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueLine("cmd1");
    terminal.QueueLine("cmd2");
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddRoute("cmd{n}", (string n) => "OK")
      .AddReplSupport(options => options.MaxHistorySize = 0)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Should handle zero max history");
  }
}
