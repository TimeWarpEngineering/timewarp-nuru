#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

// Test AnsiStringUtils.WrapText functionality

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Core.AnsiStringUtilsWrap
{

[TestTag("Widgets")]
public class AnsiStringUtilsWrapTextTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<AnsiStringUtilsWrapTextTests>();

  public static async Task Should_wrap_plain_text_at_word_boundaries()
  {
    // Arrange
    string text = "Hello World Test";

    // Act
    IReadOnlyList<string> lines = TimeWarp.Terminal.AnsiStringUtils.WrapText(text, 10);

    // Assert
    lines.Count.ShouldBe(2);
    lines[0].ShouldBe("Hello ");
    lines[1].ShouldBe("World Test");

    await Task.CompletedTask;
  }

  public static async Task Should_return_empty_line_for_null_input()
  {
    // Act
    IReadOnlyList<string> lines = TimeWarp.Terminal.AnsiStringUtils.WrapText(null, 20);

    // Assert
    lines.Count.ShouldBe(1);
    lines[0].ShouldBe("");

    await Task.CompletedTask;
  }

  public static async Task Should_return_empty_line_for_empty_input()
  {
    // Act
    IReadOnlyList<string> lines = TimeWarp.Terminal.AnsiStringUtils.WrapText("", 20);

    // Assert
    lines.Count.ShouldBe(1);
    lines[0].ShouldBe("");

    await Task.CompletedTask;
  }

  public static async Task Should_break_long_words()
  {
    // Arrange
    string text = "Supercalifragilisticexpialidocious";

    // Act
    IReadOnlyList<string> lines = TimeWarp.Terminal.AnsiStringUtils.WrapText(text, 10);

    // Assert
    lines.Count.ShouldBeGreaterThan(1);

    foreach (string line in lines)
    {
      TimeWarp.Terminal.AnsiStringUtils.GetVisibleLength(line).ShouldBeLessThanOrEqualTo(10);
    }

    await Task.CompletedTask;
  }

  public static async Task Should_preserve_ansi_color_codes_across_lines()
  {
    // Arrange
    string text = $"{AnsiColors.Red}This is red text that wraps{AnsiColors.Reset}";

    // Act
    IReadOnlyList<string> lines = TimeWarp.Terminal.AnsiStringUtils.WrapText(text, 15);

    // Assert
    lines.Count.ShouldBeGreaterThan(1);

    // First line should start with the red color
    lines[0].ShouldContain(AnsiColors.Red);

    // Subsequent lines should also have the red color restored
    if (lines.Count > 1)
    {
      lines[1].ShouldContain(AnsiColors.Red);
    }

    await Task.CompletedTask;
  }

  public static async Task Should_handle_text_shorter_than_width()
  {
    // Arrange
    string text = "Short";

    // Act
    IReadOnlyList<string> lines = TimeWarp.Terminal.AnsiStringUtils.WrapText(text, 20);

    // Assert
    lines.Count.ShouldBe(1);
    lines[0].ShouldBe("Short");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_text_exactly_at_width()
  {
    // Arrange
    string text = "1234567890";

    // Act
    IReadOnlyList<string> lines = TimeWarp.Terminal.AnsiStringUtils.WrapText(text, 10);

    // Assert
    lines.Count.ShouldBe(1);
    lines[0].ShouldBe("1234567890");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_minimum_width_of_one()
  {
    // Arrange
    string text = "ABC";

    // Act
    IReadOnlyList<string> lines = TimeWarp.Terminal.AnsiStringUtils.WrapText(text, 0);

    // Assert - Width should be treated as 1
    lines.Count.ShouldBe(3);
    lines[0].ShouldBe("A");
    lines[1].ShouldBe("B");
    lines[2].ShouldBe("C");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_multiple_spaces()
  {
    // Arrange
    string text = "Hello   World";

    // Act
    IReadOnlyList<string> lines = TimeWarp.Terminal.AnsiStringUtils.WrapText(text, 20);

    // Assert - Spaces are preserved
    lines.Count.ShouldBe(1);

    await Task.CompletedTask;
  }

  public static async Task Should_handle_osc8_hyperlinks()
  {
    // Arrange - OSC 8 hyperlink
    string text = "\x1b]8;;https://example.com\x1b\\Click Here\x1b]8;;\x1b\\";

    // Act
    IReadOnlyList<string> lines = TimeWarp.Terminal.AnsiStringUtils.WrapText(text, 20);

    // Assert
    lines.Count.ShouldBe(1);

    // Visible length should only be "Click Here" (10 chars)
    TimeWarp.Terminal.AnsiStringUtils.GetVisibleLength(lines[0]).ShouldBe(10);

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Core.AnsiStringUtilsWrap
