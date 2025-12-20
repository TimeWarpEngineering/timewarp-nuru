// CA1054: OSC 8 hyperlinks use raw URL strings by design for ergonomic API
#pragma warning disable CA1054

namespace TimeWarp.Terminal;

/// <summary>
/// Extension methods for writing hyperlinks to an <see cref="ITerminal"/>.
/// </summary>
/// <example>
/// <code>
/// // Write a clickable link
/// terminal.WriteLink("https://github.com", "GitHub");
///
/// // Write a link with newline
/// terminal.WriteLinkLine("https://example.com", "Click here");
///
/// // Conditionally use hyperlinks based on terminal support
/// if (terminal.SupportsHyperlinks)
///     terminal.WriteLinkLine("https://docs.com", "View documentation");
/// else
///     terminal.WriteLine("View documentation at https://docs.com");
/// </code>
/// </example>
public static class TerminalHyperlinkExtensions
{
  /// <summary>
  /// Writes a hyperlink to the terminal without a trailing newline.
  /// </summary>
  /// <param name="terminal">The terminal to write to.</param>
  /// <param name="url">The URL to link to.</param>
  /// <param name="displayText">The text to display. If null, the URL is used as display text.</param>
  /// <remarks>
  /// If the terminal does not support hyperlinks (<see cref="ITerminal.SupportsHyperlinks"/> is false),
  /// only the display text is written without the hyperlink escape sequences.
  /// </remarks>
  public static void WriteLink(this ITerminal terminal, string url, string? displayText = null)
  {
    ArgumentNullException.ThrowIfNull(terminal);
    ArgumentNullException.ThrowIfNull(url);

    string text = displayText ?? url;

    if (terminal.SupportsHyperlinks)
    {
      terminal.Write(AnsiHyperlinks.CreateLink(text, url));
    }
    else
    {
      terminal.Write(text);
    }
  }

  /// <summary>
  /// Writes a hyperlink to the terminal followed by a newline.
  /// </summary>
  /// <param name="terminal">The terminal to write to.</param>
  /// <param name="url">The URL to link to.</param>
  /// <param name="displayText">The text to display. If null, the URL is used as display text.</param>
  /// <remarks>
  /// If the terminal does not support hyperlinks (<see cref="ITerminal.SupportsHyperlinks"/> is false),
  /// only the display text is written without the hyperlink escape sequences.
  /// </remarks>
  public static void WriteLinkLine(this ITerminal terminal, string url, string? displayText = null)
  {
    ArgumentNullException.ThrowIfNull(terminal);
    ArgumentNullException.ThrowIfNull(url);

    string text = displayText ?? url;

    if (terminal.SupportsHyperlinks)
    {
      terminal.WriteLine(AnsiHyperlinks.CreateLink(text, url));
    }
    else
    {
      terminal.WriteLine(text);
    }
  }
}
