#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../Source/TimeWarp.Nuru.Repl/TimeWarp.Nuru.Repl.csproj

using TimeWarp.Nuru;
using TimeWarp.Nuru.Repl;

// Test display and formatting (Section 11 of REPL Test Plan)
return await RunTests<DisplayFormattingTests>();

[TestTag("REPL")]
public class DisplayFormattingTests
{
  public static async Task Should_display_welcome_message()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddReplSupport(options => options.WelcomeMessage = "Welcome to My App!")
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Welcome to My App!")
      .ShouldBeTrue("Welcome message should be displayed");
  }

  public static async Task Should_display_goodbye_message()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddReplSupport(options => options.GoodbyeMessage = "Farewell!")
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Farewell!")
      .ShouldBeTrue("Goodbye message should be displayed");
  }

  public static async Task Should_use_custom_prompt()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddReplSupport(options => options.Prompt = ">>> ")
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains(">>>")
      .ShouldBeTrue("Custom prompt should be used");
  }

  public static async Task Should_show_exit_code_when_enabled()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueLine("status");
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("status", () => "OK")
      .AddReplSupport(options => options.ShowExitCode = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Exit code:")
      .ShouldBeTrue("Exit code should be shown when enabled");
  }

  public static async Task Should_hide_exit_code_when_disabled()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueLine("status");
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("status", () => "OK")
      .AddReplSupport(options => options.ShowExitCode = false)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Exit code:")
      .ShouldBeFalse("Exit code should NOT be shown when disabled");
  }

  public static async Task Should_show_timing_when_enabled()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueLine("status");
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("status", () => "OK")
      .AddReplSupport(options => options.ShowTiming = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("ms)")
      .ShouldBeTrue("Timing should be shown when enabled");
  }

  public static async Task Should_hide_timing_when_disabled()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueLine("status");
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("status", () => "OK")
      .AddReplSupport(options => options.ShowTiming = false)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("ms)")
      .ShouldBeFalse("Timing should NOT be shown when disabled");
  }

  public static async Task Should_use_colors_when_enabled()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddReplSupport(options => options.EnableColors = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - ANSI escape codes should be present
    terminal.OutputContains("\x1b[")
      .ShouldBeTrue("Colors should be enabled");
  }

  public static async Task Should_not_use_colors_when_disabled()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddReplSupport(options => options.EnableColors = false)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - ANSI escape codes should NOT be present
    terminal.OutputContains("\x1b[")
      .ShouldBeFalse("Colors should NOT be used when disabled");
  }
}
