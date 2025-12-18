#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

// Test Panel widget word wrapping functionality

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Core.PanelWidgetWrap
{

[TestTag("Widgets")]
public class PanelWidgetWordWrapTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<PanelWidgetWordWrapTests>();

  public static async Task Should_wrap_long_content_within_panel_width()
  {
    // Arrange
    string longText = "Learn how to get started with building web products with ASP.NET Core. This course covers the fundamentals of ASP.NET Core web development.";
    Panel panel = new() { Content = longText, Width = 80 };

    // Act
    string[] lines = panel.Render(80);

    // Assert - All content lines should fit within the panel width
    foreach (string line in lines)
    {
      int visibleLength = TimeWarp.Nuru.AnsiStringUtils.GetVisibleLength(line);
      visibleLength.ShouldBeLessThanOrEqualTo(80);
    }

    // Should have multiple content lines due to wrapping (top + content lines + bottom)
    lines.Length.ShouldBeGreaterThan(3);

    await Task.CompletedTask;
  }

  public static async Task Should_wrap_content_preserving_ansi_codes()
  {
    // Arrange
    string styledText = $"{AnsiColors.Red}This is a very long red text that should wrap properly{AnsiColors.Reset} and continue with normal text that also needs wrapping.";
    Panel panel = new() { Content = styledText, Width = 40 };

    // Act
    string[] lines = panel.Render(40);

    // Assert - All content lines should fit within panel width
    foreach (string line in lines)
    {
      int visibleLength = TimeWarp.Nuru.AnsiStringUtils.GetVisibleLength(line);
      visibleLength.ShouldBeLessThanOrEqualTo(40);
    }

    // The styled text should be preserved (red color should appear)
    string allContent = string.Concat(lines);
    allContent.ShouldContain(AnsiColors.Red);

    await Task.CompletedTask;
  }

  public static async Task Should_not_wrap_when_WordWrap_is_false()
  {
    // Arrange
    string longText = "This is a very long line that would normally wrap but WordWrap is disabled.";
    Panel panel = new() { Content = longText, Width = 40, WordWrap = false };

    // Act
    string[] lines = panel.Render(40);

    // Assert - Should only have 3 lines: top border, content (not wrapped), bottom border
    lines.Length.ShouldBe(3);

    // Content line should contain the full text (even if it overflows)
    lines[1].ShouldContain("This is a very long line");

    await Task.CompletedTask;
  }

  public static async Task Should_wrap_content_with_hyperlinks()
  {
    // Arrange - OSC 8 hyperlink sequence
    string hyperlink = "\x1b]8;;https://example.com\x1b\\Click here for more details and information about this topic\x1b]8;;\x1b\\";
    Panel panel = new() { Content = hyperlink, Width = 50 };

    // Act
    string[] lines = panel.Render(50);

    // Assert - All lines should fit within width
    foreach (string line in lines)
    {
      int visibleLength = TimeWarp.Nuru.AnsiStringUtils.GetVisibleLength(line);
      visibleLength.ShouldBeLessThanOrEqualTo(50);
    }

    await Task.CompletedTask;
  }

  public static async Task Should_wrap_very_long_word()
  {
    // Arrange - A single word that's longer than the content area
    string longWord = "Supercalifragilisticexpialidocious";
    Panel panel = new() { Content = longWord, Width = 20 };

    // Act
    string[] lines = panel.Render(20);

    // Assert - All lines should fit within panel width
    foreach (string line in lines)
    {
      int visibleLength = TimeWarp.Nuru.AnsiStringUtils.GetVisibleLength(line);
      visibleLength.ShouldBeLessThanOrEqualTo(20);
    }

    // Should have multiple lines since the word is broken
    lines.Length.ShouldBeGreaterThan(3);

    await Task.CompletedTask;
  }

  public static async Task Should_preserve_explicit_newlines_while_wrapping()
  {
    // Arrange
    string multilineText = "First paragraph with some long text that needs wrapping.\nSecond paragraph that also contains long text for wrapping.";
    Panel panel = new() { Content = multilineText, Width = 40 };

    // Act
    string[] lines = panel.Render(40);

    // Assert - All lines should fit within panel width
    foreach (string line in lines)
    {
      int visibleLength = TimeWarp.Nuru.AnsiStringUtils.GetVisibleLength(line);
      visibleLength.ShouldBeLessThanOrEqualTo(40);
    }

    // Should have more than 4 lines (top + at least 2 content lines per paragraph + bottom)
    lines.Length.ShouldBeGreaterThan(4);

    await Task.CompletedTask;
  }

  public static async Task Should_handle_empty_content_with_wrapping()
  {
    // Arrange
    Panel panel = new() { Content = "", Width = 40, WordWrap = true };

    // Act
    string[] lines = panel.Render(40);

    // Assert - Should still render empty panel
    lines.Length.ShouldBe(3); // top + empty content row + bottom

    await Task.CompletedTask;
  }

  public static async Task Should_use_WordWrap_from_builder()
  {
    // Arrange
    string longText = "This is a long line that would wrap if WordWrap was enabled.";
    Panel panel = new PanelBuilder()
      .Content(longText)
      .Width(30)
      .WordWrap(false)
      .Build();

    // Act
    string[] lines = panel.Render(30);

    // Assert - Should not wrap (3 lines: top, content, bottom)
    lines.Length.ShouldBe(3);

    await Task.CompletedTask;
  }

  public static async Task Should_wrap_with_proper_visible_width()
  {
    // Arrange - Content that's exactly at the boundary
    string text = "Hello World! This is a test.";
    Panel panel = new() { Content = text, Width = 20, PaddingHorizontal = 1 };

    // Act
    string[] lines = panel.Render(20);

    // Assert - Content area is width(20) - 2 borders - 2 padding = 16 chars
    // "Hello World! This " = 18 chars, which is > 16, so should wrap
    lines.Length.ShouldBeGreaterThan(3);

    // Each line should be exactly 20 visible chars
    foreach (string line in lines)
    {
      int visibleLength = TimeWarp.Nuru.AnsiStringUtils.GetVisibleLength(line);
      visibleLength.ShouldBe(20);
    }

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Core.PanelWidgetWrap
