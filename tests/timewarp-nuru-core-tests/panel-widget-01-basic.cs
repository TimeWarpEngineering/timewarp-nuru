#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

// Test Panel widget basic functionality
return await RunTests<PanelWidgetBasicTests>(clearCache: true);

[TestTag("Widgets")]
[ClearRunfileCache]
public class PanelWidgetBasicTests
{
  public static async Task Should_render_simple_panel_with_content()
  {
    // Arrange
    Panel panel = new() { Content = "Hello World" };

    // Act
    string[] lines = panel.Render(40);

    // Assert
    lines.Length.ShouldBe(3); // top border + content + bottom border
    lines[0].ShouldContain("╭"); // rounded top-left
    lines[0].ShouldContain("╮"); // rounded top-right
    lines[1].ShouldContain("Hello World");
    lines[1].ShouldContain("│"); // vertical border
    lines[2].ShouldContain("╰"); // rounded bottom-left
    lines[2].ShouldContain("╯"); // rounded bottom-right

    await Task.CompletedTask;
  }

  public static async Task Should_render_panel_with_header()
  {
    // Arrange
    Panel panel = new() { Header = "Notice", Content = "Important info" };

    // Act
    string[] lines = panel.Render(40);

    // Assert
    lines[0].ShouldContain("Notice");
    lines[1].ShouldContain("Important info");

    await Task.CompletedTask;
  }

  public static async Task Should_render_panel_with_square_border()
  {
    // Arrange
    Panel panel = new() { Content = "Test", Border = BorderStyle.Square };

    // Act
    string[] lines = panel.Render(20);

    // Assert
    lines[0].ShouldContain("┌"); // square top-left
    lines[0].ShouldContain("┐"); // square top-right
    lines[2].ShouldContain("└"); // square bottom-left
    lines[2].ShouldContain("┘"); // square bottom-right

    await Task.CompletedTask;
  }

  public static async Task Should_render_panel_with_double_border()
  {
    // Arrange
    Panel panel = new() { Content = "Test", Border = BorderStyle.Doubled };

    // Act
    string[] lines = panel.Render(20);

    // Assert
    lines[0].ShouldContain("╔"); // double top-left
    lines[0].ShouldContain("╗"); // double top-right
    lines[0].ShouldContain("═"); // double horizontal
    lines[1].ShouldContain("║"); // double vertical
    lines[2].ShouldContain("╚"); // double bottom-left
    lines[2].ShouldContain("╝"); // double bottom-right

    await Task.CompletedTask;
  }

  public static async Task Should_render_panel_with_heavy_border()
  {
    // Arrange
    Panel panel = new() { Content = "Test", Border = BorderStyle.Heavy };

    // Act
    string[] lines = panel.Render(20);

    // Assert
    lines[0].ShouldContain("┏"); // heavy top-left
    lines[0].ShouldContain("┓"); // heavy top-right
    lines[0].ShouldContain("━"); // heavy horizontal
    lines[1].ShouldContain("┃"); // heavy vertical
    lines[2].ShouldContain("┗"); // heavy bottom-left
    lines[2].ShouldContain("┛"); // heavy bottom-right

    await Task.CompletedTask;
  }

  public static async Task Should_render_multiline_content()
  {
    // Arrange
    Panel panel = new() { Content = "Line 1\nLine 2\nLine 3" };

    // Act
    string[] lines = panel.Render(40);

    // Assert
    lines.Length.ShouldBe(5); // top + 3 content lines + bottom
    lines[1].ShouldContain("Line 1");
    lines[2].ShouldContain("Line 2");
    lines[3].ShouldContain("Line 3");

    await Task.CompletedTask;
  }

  public static async Task Should_render_panel_with_vertical_padding()
  {
    // Arrange
    Panel panel = new() { Content = "Content", PaddingVertical = 1 };

    // Act
    string[] lines = panel.Render(40);

    // Assert
    // top + 1 pad + content + 1 pad + bottom = 5 lines
    lines.Length.ShouldBe(5);
    lines[1].ShouldNotContain("Content"); // padding line
    lines[2].ShouldContain("Content");
    lines[3].ShouldNotContain("Content"); // padding line

    await Task.CompletedTask;
  }

  public static async Task Should_render_panel_with_horizontal_padding()
  {
    // Arrange
    Panel panel = new() { Content = "Hi", PaddingHorizontal = 3 };

    // Act
    string[] lines = panel.Render(40);

    // Assert
    // Content line should have 3 spaces before and after content
    lines[1].ShouldContain("│   Hi"); // 3 spaces of padding

    await Task.CompletedTask;
  }

  public static async Task Should_render_panel_with_fixed_width()
  {
    // Arrange
    Panel panel = new() { Content = "Test", Width = 20 };

    // Act
    string[] lines = panel.Render(80); // terminal width should be ignored

    // Assert
    AnsiStringUtils.GetVisibleLength(lines[0]).ShouldBe(20);
    AnsiStringUtils.GetVisibleLength(lines[2]).ShouldBe(20);

    await Task.CompletedTask;
  }

  public static async Task Should_render_panel_with_border_color()
  {
    // Arrange
    Panel panel = new() { Content = "Test", BorderColor = AnsiColors.Cyan };

    // Act
    string[] lines = panel.Render(30);

    // Assert
    lines[0].ShouldContain(AnsiColors.Cyan);
    lines[0].ShouldContain(AnsiColors.Reset);
    lines[1].ShouldContain(AnsiColors.Cyan);
    lines[2].ShouldContain(AnsiColors.Cyan);

    await Task.CompletedTask;
  }

  public static async Task Should_render_panel_without_border_when_style_is_none()
  {
    // Arrange
    Panel panel = new() { Content = "Line 1\nLine 2", Border = BorderStyle.None };

    // Act
    string[] lines = panel.Render(40);

    // Assert
    lines.Length.ShouldBe(2);
    lines[0].ShouldBe("Line 1");
    lines[1].ShouldBe("Line 2");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_empty_content()
  {
    // Arrange
    Panel panel = new() { Content = "" };

    // Act
    string[] lines = panel.Render(20);

    // Assert
    lines.Length.ShouldBe(3); // top + empty content row + bottom

    await Task.CompletedTask;
  }

  public static async Task Should_handle_null_content()
  {
    // Arrange
    Panel panel = new() { Content = null };

    // Act
    string[] lines = panel.Render(20);

    // Assert
    lines.Length.ShouldBe(3); // top + empty content row + bottom

    await Task.CompletedTask;
  }
}
