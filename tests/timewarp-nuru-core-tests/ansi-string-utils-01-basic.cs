#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

// Test AnsiStringUtils functionality
return await RunTests<AnsiStringUtilsTests>(clearCache: true);

[TestTag("Widgets")]
[ClearRunfileCache]
public class AnsiStringUtilsTests
{
  public static async Task Should_strip_basic_ansi_codes()
  {
    // Arrange
    string input = "\x1b[31mError\x1b[0m";

    // Act
    string result = AnsiStringUtils.StripAnsiCodes(input);

    // Assert
    result.ShouldBe("Error");

    await Task.CompletedTask;
  }

  public static async Task Should_strip_256_color_codes()
  {
    // Arrange
    string input = "\x1b[38;5;214mOrange\x1b[0m";

    // Act
    string result = AnsiStringUtils.StripAnsiCodes(input);

    // Assert
    result.ShouldBe("Orange");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_null_input()
  {
    // Act
    string result = AnsiStringUtils.StripAnsiCodes(null);

    // Assert
    result.ShouldBe(string.Empty);

    await Task.CompletedTask;
  }

  public static async Task Should_handle_empty_input()
  {
    // Act
    string result = AnsiStringUtils.StripAnsiCodes("");

    // Assert
    result.ShouldBe(string.Empty);

    await Task.CompletedTask;
  }

  public static async Task Should_get_visible_length_with_ansi_codes()
  {
    // Arrange
    string styled = "\x1b[31mError\x1b[0m"; // "Error" = 5 chars

    // Act
    int length = AnsiStringUtils.GetVisibleLength(styled);

    // Assert
    length.ShouldBe(5);

    await Task.CompletedTask;
  }

  public static async Task Should_get_visible_length_with_chained_styles()
  {
    // Arrange
    string styled = "Warning".Yellow().Bold(); // Uses chained ANSI codes

    // Act
    int length = AnsiStringUtils.GetVisibleLength(styled);

    // Assert
    length.ShouldBe(7); // "Warning" = 7 chars

    await Task.CompletedTask;
  }

  public static async Task Should_get_visible_length_without_ansi_codes()
  {
    // Arrange
    string plain = "Hello World";

    // Act
    int length = AnsiStringUtils.GetVisibleLength(plain);

    // Assert
    length.ShouldBe(11);

    await Task.CompletedTask;
  }

  public static async Task Should_pad_right_accounting_for_ansi()
  {
    // Arrange
    string styled = "Hi".Red(); // 2 visible chars

    // Act
    string padded = AnsiStringUtils.PadRightVisible(styled, 10);

    // Assert
    int visibleLength = AnsiStringUtils.GetVisibleLength(padded);
    visibleLength.ShouldBe(10);
    padded.ShouldContain(AnsiColors.Red);
    padded.ShouldEndWith("        "); // 8 spaces

    await Task.CompletedTask;
  }

  public static async Task Should_pad_left_accounting_for_ansi()
  {
    // Arrange
    string styled = "Hi".Blue(); // 2 visible chars

    // Act
    string padded = AnsiStringUtils.PadLeftVisible(styled, 10);

    // Assert
    int visibleLength = AnsiStringUtils.GetVisibleLength(padded);
    visibleLength.ShouldBe(10);
    padded.ShouldContain(AnsiColors.Blue);
    padded.ShouldStartWith("        "); // 8 spaces

    await Task.CompletedTask;
  }

  public static async Task Should_center_accounting_for_ansi()
  {
    // Arrange
    string styled = "Hi".Green(); // 2 visible chars

    // Act
    string centered = AnsiStringUtils.CenterVisible(styled, 10);

    // Assert
    int visibleLength = AnsiStringUtils.GetVisibleLength(centered);
    visibleLength.ShouldBe(10);
    // Should have 4 spaces on each side
    centered.ShouldStartWith("    "); // 4 spaces

    await Task.CompletedTask;
  }

  public static async Task Should_not_pad_if_already_at_width()
  {
    // Arrange
    string text = "Hello"; // 5 chars

    // Act
    string padded = AnsiStringUtils.PadRightVisible(text, 5);

    // Assert
    padded.ShouldBe("Hello");

    await Task.CompletedTask;
  }

  public static async Task Should_not_truncate_if_exceeds_width()
  {
    // Arrange
    string text = "Hello World"; // 11 chars

    // Act
    string padded = AnsiStringUtils.PadRightVisible(text, 5);

    // Assert - Should not truncate
    padded.ShouldBe("Hello World");

    await Task.CompletedTask;
  }
}
