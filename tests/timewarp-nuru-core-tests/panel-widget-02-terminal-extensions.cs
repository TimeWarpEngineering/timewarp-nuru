#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

// Test Panel widget terminal extension methods

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Core.PanelWidgetTerminal
{

[TestTag("Widgets")]
public class PanelTerminalExtensionTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<PanelTerminalExtensionTests>();

  public static async Task Should_write_simple_panel_to_terminal()
  {
    // Arrange
    using TestTerminal terminal = new() { WindowWidth = 40 };

    // Act
    terminal.WritePanel("Hello World");

    // Assert
    terminal.Output.ShouldContain("Hello World");
    terminal.Output.ShouldContain("╭"); // rounded corners by default
    terminal.Output.ShouldContain("╯");

    await Task.CompletedTask;
  }

  public static async Task Should_write_panel_with_header()
  {
    // Arrange
    using TestTerminal terminal = new() { WindowWidth = 40 };

    // Act
    terminal.WritePanel("Content here", "Notice");

    // Assert
    terminal.Output.ShouldContain("Notice");
    terminal.Output.ShouldContain("Content here");

    await Task.CompletedTask;
  }

  public static async Task Should_write_panel_with_border_style()
  {
    // Arrange
    using TestTerminal terminal = new() { WindowWidth = 40 };

    // Act
    terminal.WritePanel("Test", BorderStyle.Doubled);

    // Assert
    terminal.Output.ShouldContain("╔"); // double border
    terminal.Output.ShouldContain("║");
    terminal.Output.ShouldContain("╝");

    await Task.CompletedTask;
  }

  public static async Task Should_write_panel_with_fluent_builder()
  {
    // Arrange
    using TestTerminal terminal = new() { WindowWidth = 40 };

    // Act
    terminal.WritePanel(panel => panel
        .Header("Configuration")
        .Content("Setting: value")
        .Border(BorderStyle.Rounded)
        .Padding(2, 1));

    // Assert
    terminal.Output.ShouldContain("Configuration");
    terminal.Output.ShouldContain("Setting: value");
    terminal.Output.ShouldContain("╭");

    await Task.CompletedTask;
  }

  public static async Task Should_write_preconfigured_panel()
  {
    // Arrange
    using TestTerminal terminal = new() { WindowWidth = 40 };
    Panel panel = new()
    {
      Header = "Test",
      Content = "Content",
      Border = BorderStyle.Heavy,
      BorderColor = AnsiColors.Yellow
    };

    // Act
    terminal.WritePanel(panel);

    // Assert
    terminal.Output.ShouldContain("Test");
    terminal.Output.ShouldContain("Content");
    terminal.Output.ShouldContain("┏"); // heavy border
    terminal.Output.ShouldContain(AnsiColors.Yellow);

    await Task.CompletedTask;
  }

  public static async Task Should_use_terminal_width()
  {
    // Arrange
    using TestTerminal terminal = new() { WindowWidth = 60 };

    // Act
    terminal.WritePanel("Test content");

    // Assert
    string[] lines = terminal.Output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
    TimeWarp.Nuru.AnsiStringUtils.GetVisibleLength(lines[0]).ShouldBe(60);

    await Task.CompletedTask;
  }

  public static async Task Should_handle_multiline_content()
  {
    // Arrange
    using TestTerminal terminal = new() { WindowWidth = 40 };

    // Act
    terminal.WritePanel("Line 1\nLine 2\nLine 3");

    // Assert
    terminal.Output.ShouldContain("Line 1");
    terminal.Output.ShouldContain("Line 2");
    terminal.Output.ShouldContain("Line 3");

    await Task.CompletedTask;
  }

  public static async Task Should_write_panel_with_styled_header()
  {
    // Arrange
    using TestTerminal terminal = new() { WindowWidth = 40 };

    // Act
    terminal.WritePanel(panel => panel
        .Header("Results".Cyan().Bold())
        .Content("Success"));

    // Assert
    terminal.Output.ShouldContain("Results");
    terminal.Output.ShouldContain(AnsiColors.Cyan);
    terminal.Output.ShouldContain("Success");

    await Task.CompletedTask;
  }

  public static async Task Should_include_newlines_between_panel_lines()
  {
    // Arrange
    using TestTerminal terminal = new() { WindowWidth = 40 };

    // Act
    terminal.WritePanel("Content");

    // Assert
    // Should have 3 lines: top, content, bottom
    string[] lines = terminal.Output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
    lines.Length.ShouldBe(3);

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Core.PanelWidgetTerminal
