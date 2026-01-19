#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// Test display and formatting (Section 11 of REPL Test Plan)

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.DisplayFormatting
{
  [TestTag("REPL")]
  public class DisplayFormattingTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<DisplayFormattingTests>();

  public static async Task Should_display_welcome_message()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("exit");

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .AddRepl(options => options.WelcomeMessage = "Welcome to My App!")
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Welcome to My App!")
      .ShouldBeTrue("Welcome message should be displayed");
  }

  public static async Task Should_display_goodbye_message()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("exit");

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .AddRepl(options => options.GoodbyeMessage = "Farewell!")
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Farewell!")
      .ShouldBeTrue("Goodbye message should be displayed");
  }

  public static async Task Should_use_custom_prompt()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("exit");

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .AddRepl(options => options.Prompt = ">>> ")
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains(">>>")
      .ShouldBeTrue("Custom prompt should be used");
  }

  public static async Task Should_show_exit_code_when_enabled()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("status");
    terminal.QueueLine("exit");

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("status").WithHandler(() => "OK").AsQuery().Done()
      .AddRepl(options => options.ShowExitCode = true)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Exit code:")
      .ShouldBeTrue("Exit code should be shown when enabled");
  }

  public static async Task Should_hide_exit_code_when_disabled()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("status");
    terminal.QueueLine("exit");

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("status").WithHandler(() => "OK").AsQuery().Done()
      .AddRepl(options => options.ShowExitCode = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("Exit code:")
      .ShouldBeFalse("Exit code should NOT be shown when disabled");
  }

  public static async Task Should_show_timing_when_enabled()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("status");
    terminal.QueueLine("exit");

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("status").WithHandler(() => "OK").AsQuery().Done()
      .AddRepl(options => options.ShowTiming = true)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("ms)")
      .ShouldBeTrue("Timing should be shown when enabled");
  }

  public static async Task Should_hide_timing_when_disabled()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("status");
    terminal.QueueLine("exit");

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("status").WithHandler(() => "OK").AsQuery().Done()
      .AddRepl(options => options.ShowTiming = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert
    terminal.OutputContains("ms)")
      .ShouldBeFalse("Timing should NOT be shown when disabled");
  }

  public static async Task Should_use_colors_when_enabled()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("exit");

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .AddRepl(options => options.EnableColors = true)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert - ANSI escape codes should be present
    terminal.OutputContains("\x1b[")
      .ShouldBeTrue("Colors should be enabled");
  }

  public static async Task Should_not_use_colors_when_disabled()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("exit");

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .AddRepl(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert - ANSI escape codes should NOT be present
    terminal.OutputContains("\x1b[")
      .ShouldBeFalse("Colors should NOT be used when disabled");
  }
  }
}
