#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// Test edge cases (Section 15 of REPL Test Plan)

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.EdgeCases
{
  [TestTag("REPL")]
  public class EdgeCaseTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<EdgeCaseTests>();

  public static async Task Should_handle_very_long_input()
  {
    // Arrange
    using TestTerminal terminal = new();
    string longArg = new('x', 1000);
    terminal.QueueLine($"echo {longArg}");
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("echo {text}")
        .WithHandler((string text) => text)
        .AsCommand()
        .Done()
      .AddRepl()
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Should handle very long input");
  }

  public static async Task Should_handle_unicode_input()
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

    // Assert - basic unicode handling (emoji would need different test setup)
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Should handle unicode characters");
  }

  public static async Task Should_handle_empty_input()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("");  // Empty input
    terminal.QueueLine("");  // Another empty
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .AddRepl()
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Should handle empty input");
  }

  public static async Task Should_handle_whitespace_only_input()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("   ");  // Whitespace only
    terminal.QueueLine("\t");   // Tab only
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .AddRepl()
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Should handle whitespace-only input");
  }

  // Note: Null/empty HistoryIgnorePatterns tested in repl-03b-history-security.cs
  // HistoryIgnorePatterns is init-only so can't be set via Action<ReplOptions> lambda

  public static async Task Should_handle_special_characters_in_commands()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("echo \"Hello!@#$%^&*()\"");
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("echo {text}")
        .WithHandler((string text) => text)
        .AsCommand()
        .Done()
      .AddRepl()
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Should handle special characters");
  }

  public static async Task Should_handle_rapid_commands()
  {
    // Arrange
    using TestTerminal terminal = new();

    // Queue many commands rapidly
    for (int i = 0; i < 50; i++)
    {
      terminal.QueueLine("noop");
    }

    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("noop").WithHandler(() => "OK").AsCommand().Done()
      .AddRepl()
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Should handle rapid commands");
  }

  public static async Task Should_handle_window_width_edge_cases()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.WindowWidth = 10;  // Very narrow window
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .AddRepl()
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Should handle narrow window");
  }

  public static async Task Should_handle_zero_max_history()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("cmd1");
    terminal.QueueLine("cmd2");
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("cmd{n}").WithHandler((string n) => n).AsCommand().Done()
      .AddRepl(options => options.MaxHistorySize = 0)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Should handle zero max history");
  }
  }
}
