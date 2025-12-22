// CA1822: Analyzer bug - doesn't recognize C# 14 extension block receiver 'text' as instance data
#pragma warning disable CA1822
// CA1054: OSC 8 hyperlinks use raw URL strings by design for ergonomic fluent API
#pragma warning disable CA1054

namespace TimeWarp.Terminal;

/// <summary>
/// Constants for OSC 8 hyperlink escape sequences.
/// </summary>
public static class AnsiHyperlinks
{
  /// <summary>
  /// OSC 8 escape sequence to start a hyperlink.
  /// Format: \e]8;;{URL}\e\
  /// </summary>
  internal const string LinkStart = "\x1b]8;;";

  /// <summary>
  /// OSC 8 escape sequence to end a hyperlink.
  /// </summary>
  internal const string LinkEnd = "\x1b\\";

  /// <summary>
  /// Creates an OSC 8 hyperlink string.
  /// </summary>
  /// <param name="displayText">The text to display.</param>
  /// <param name="url">The URL to link to.</param>
  /// <returns>An OSC 8 formatted hyperlink string.</returns>
  public static string CreateLink(string displayText, string url)
    => $"{LinkStart}{url}{LinkEnd}{displayText}{LinkStart}{LinkEnd}";
}

/// <summary>
/// Fluent extension methods for creating OSC 8 hyperlinks in terminal output.
/// OSC 8 hyperlinks are supported by Windows Terminal, iTerm2, VS Code terminal,
/// Hyper, Konsole, and GNOME Terminal 3.26+.
/// </summary>
/// <example>
/// <code>
/// // Create a clickable link
/// "Click here".Link("https://example.com")
///
/// // Chain with color extensions
/// "Visit Ardalis.com".Link("https://ardalis.com").Cyan().Bold()
/// </code>
/// </example>
public static class AnsiHyperlinkExtensions
{
  extension(string text)
  {
    /// <summary>
    /// Wraps the text in an OSC 8 hyperlink sequence.
    /// In supported terminals, the text becomes a clickable link.
    /// In unsupported terminals, only the display text is shown.
    /// </summary>
    /// <param name="url">The URL to link to.</param>
    /// <returns>The text wrapped in OSC 8 hyperlink escape sequences.</returns>
    /// <example>
    /// <code>
    /// // Simple hyperlink
    /// "GitHub".Link("https://github.com")
    ///
    /// // With styling
    /// "Download".Link("https://example.com/file.zip").Blue().Underline()
    /// </code>
    /// </example>
    public string Link(string url) => AnsiHyperlinks.CreateLink(text, url);
  }
}
