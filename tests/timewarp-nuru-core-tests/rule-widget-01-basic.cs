#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

// Test Rule widget basic functionality
return await RunTests<RuleWidgetBasicTests>(clearCache: true);

[TestTag("Widgets")]
[ClearRunfileCache]
public class RuleWidgetBasicTests
{
  public static async Task Should_render_simple_rule_at_specified_width()
  {
    // Arrange
    Rule rule = new();

    // Act
    string rendered = rule.Render(40);

    // Assert
    rendered.Length.ShouldBe(40);
    rendered.ShouldBe(new string('─', 40));

    await Task.CompletedTask;
  }

  public static async Task Should_render_rule_with_centered_title()
  {
    // Arrange
    Rule rule = new() { Title = "Test" };

    // Act
    string rendered = rule.Render(40);

    // Assert
    rendered.ShouldContain("Test");
    rendered.ShouldContain("─");

    // Title should be roughly centered
    int testIndex = rendered.IndexOf("Test", StringComparison.Ordinal);
    testIndex.ShouldBeGreaterThan(10);
    testIndex.ShouldBeLessThan(25);

    await Task.CompletedTask;
  }

  public static async Task Should_render_rule_with_doubled_style()
  {
    // Arrange
    Rule rule = new() { Style = LineStyle.Doubled };

    // Act
    string rendered = rule.Render(20);

    // Assert
    rendered.ShouldBe(new string('═', 20));

    await Task.CompletedTask;
  }

  public static async Task Should_render_rule_with_heavy_style()
  {
    // Arrange
    Rule rule = new() { Style = LineStyle.Heavy };

    // Act
    string rendered = rule.Render(20);

    // Assert
    rendered.ShouldBe(new string('━', 20));

    await Task.CompletedTask;
  }

  public static async Task Should_render_rule_with_color()
  {
    // Arrange
    Rule rule = new() { Color = AnsiColors.Cyan };

    // Act
    string rendered = rule.Render(20);

    // Assert
    rendered.ShouldContain(AnsiColors.Cyan);
    rendered.ShouldContain(AnsiColors.Reset);

    await Task.CompletedTask;
  }

  public static async Task Should_render_rule_with_fixed_width()
  {
    // Arrange
    Rule rule = new() { Width = 30 };

    // Act
    string rendered = rule.Render(80); // Terminal width should be ignored

    // Assert
    rendered.Length.ShouldBe(30);

    await Task.CompletedTask;
  }

  public static async Task Should_handle_title_longer_than_width()
  {
    // Arrange
    Rule rule = new() { Title = "This is a very long title" };

    // Act
    string rendered = rule.Render(20);

    // Assert - Should just show the title if not enough space
    rendered.ShouldContain("This is a very long title");

    await Task.CompletedTask;
  }

  public static async Task Should_preserve_title_ansi_codes_when_colored()
  {
    // Arrange
    string styledTitle = "Test".Cyan();
    Rule rule = new() { Title = styledTitle, Color = AnsiColors.Yellow };

    // Act
    string rendered = rule.Render(40);

    // Assert - Title should keep its color
    rendered.ShouldContain(AnsiColors.Cyan);
    // Line color should be applied
    rendered.ShouldContain(AnsiColors.Yellow);

    await Task.CompletedTask;
  }
}
