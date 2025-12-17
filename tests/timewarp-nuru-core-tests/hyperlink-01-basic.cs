#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

// Test OSC 8 hyperlink functionality

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Core.Hyperlink
{

[TestTag("Hyperlinks")]
public class HyperlinkTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<HyperlinkTests>();

  public static async Task Should_create_hyperlink_with_link_extension()
  {
    // Arrange
    string displayText = "Click here";
    string url = "https://example.com";

    // Act
    string result = displayText.Link(url);

    // Assert - Verify OSC 8 format: \e]8;;URL\e\TEXT\e]8;;\e\
    result.ShouldContain("\x1b]8;;"); // Start sequence
    result.ShouldContain(url);
    result.ShouldContain(displayText);
    result.ShouldEndWith("\x1b]8;;\x1b\\"); // End sequence (empty URL to close)

    await Task.CompletedTask;
  }

  public static async Task Should_create_hyperlink_with_create_link_method()
  {
    // Arrange
    string displayText = "GitHub";
    string url = "https://github.com";

    // Act
    string result = AnsiHyperlinks.CreateLink(displayText, url);

    // Assert
    string expected = $"\x1b]8;;{url}\x1b\\{displayText}\x1b]8;;\x1b\\";
    result.ShouldBe(expected);

    await Task.CompletedTask;
  }

  public static async Task Should_chain_hyperlink_with_color_extensions()
  {
    // Arrange
    string displayText = "Styled Link";
    string url = "https://example.com";

    // Act - Chain Link with color
    string result = displayText.Link(url).Cyan();

    // Assert - Should contain both hyperlink and color codes
    result.ShouldContain("\x1b]8;;"); // OSC 8 start
    result.ShouldContain(url);
    result.ShouldContain(AnsiColors.Cyan); // Color code
    result.ShouldContain(AnsiColors.Reset); // Reset code

    await Task.CompletedTask;
  }

  public static async Task Should_chain_color_then_hyperlink()
  {
    // Arrange
    string displayText = "Colored then linked";
    string url = "https://example.com";

    // Act - Apply color first, then link
    string result = displayText.Blue().Link(url);

    // Assert - The colored text gets wrapped in hyperlink
    result.ShouldContain("\x1b]8;;");
    result.ShouldContain(url);
    result.ShouldContain(AnsiColors.Blue);

    await Task.CompletedTask;
  }

  public static async Task Should_write_link_to_terminal_when_supported()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.SupportsHyperlinks = true;
    string url = "https://docs.microsoft.com";
    string displayText = "Microsoft Docs";

    // Act
    terminal.WriteLink(url, displayText);

    // Assert - Should contain OSC 8 sequences
    terminal.Output.ShouldContain("\x1b]8;;");
    terminal.Output.ShouldContain(url);
    terminal.Output.ShouldContain(displayText);

    await Task.CompletedTask;
  }

  public static async Task Should_write_plain_text_when_hyperlinks_not_supported()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.SupportsHyperlinks = false;
    string url = "https://docs.microsoft.com";
    string displayText = "Microsoft Docs";

    // Act
    terminal.WriteLink(url, displayText);

    // Assert - Should NOT contain OSC 8 sequences, just plain text
    terminal.Output.ShouldNotContain("\x1b]8;;");
    terminal.Output.ShouldBe(displayText);

    await Task.CompletedTask;
  }

  public static async Task Should_write_link_line_with_newline()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.SupportsHyperlinks = true;
    string url = "https://example.com";

    // Act
    terminal.WriteLinkLine(url, "Example");

    // Assert - Should end with newline
    terminal.Output.ShouldEndWith(Environment.NewLine);

    await Task.CompletedTask;
  }

  public static async Task Should_use_url_as_display_text_when_not_specified()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.SupportsHyperlinks = true;
    string url = "https://example.com";

    // Act
    terminal.WriteLink(url); // No display text

    // Assert - URL should appear as display text
    string output = terminal.Output;
    // The URL appears twice: once in the OSC sequence, once as display text
    int urlCount = (output.Length - output.Replace(url, "").Length) / url.Length;
    urlCount.ShouldBe(2);

    await Task.CompletedTask;
  }

  public static async Task Should_handle_empty_display_text()
  {
    // Arrange
    string displayText = "";
    string url = "https://example.com";

    // Act
    string result = displayText.Link(url);

    // Assert - Should still create valid OSC 8 sequence
    result.ShouldContain("\x1b]8;;");
    result.ShouldContain(url);
    // Display text portion is empty but sequence is valid
    string expected = $"\x1b]8;;{url}\x1b\\\x1b]8;;\x1b\\";
    result.ShouldBe(expected);

    await Task.CompletedTask;
  }

  public static async Task Should_test_terminal_default_hyperlink_support_is_false()
  {
    // Arrange
    using TestTerminal terminal = new();

    // Assert - TestTerminal defaults to false for SupportsHyperlinks
    terminal.SupportsHyperlinks.ShouldBeFalse();

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Core.Hyperlink
