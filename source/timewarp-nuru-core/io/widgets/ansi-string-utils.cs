namespace TimeWarp.Nuru;

/// <summary>
/// Utility methods for working with strings containing ANSI escape codes.
/// </summary>
public static partial class AnsiStringUtils
{
  /// <summary>
  /// Compiled regex for stripping ANSI escape sequences.
  /// Matches:
  /// - CSI sequences like \x1b[0m, \x1b[31m, \x1b[38;5;214m, etc.
  /// - OSC 8 hyperlink sequences: \x1b]8;;URL\x1b\ or \x1b]8;;URL\a (BEL terminator)
  /// </summary>
  private static readonly Regex AnsiRegexInstance = new(
    @"\x1b\[[0-9;]*m|\x1b]8;;[^\x07\x1b]*(?:\x1b\\|\x07)",
    RegexOptions.Compiled);

  /// <summary>
  /// Gets the compiled regex for stripping ANSI codes.
  /// </summary>
  private static Regex AnsiRegex() => AnsiRegexInstance;

  /// <summary>
  /// Removes all ANSI escape codes from a string.
  /// </summary>
  /// <param name="text">The text potentially containing ANSI escape codes.</param>
  /// <returns>The text with all ANSI escape codes removed.</returns>
  /// <example>
  /// <code>
  /// string styled = "\x1b[31mError\x1b[0m";
  /// string plain = AnsiStringUtils.StripAnsiCodes(styled);
  /// // plain == "Error"
  /// </code>
  /// </example>
  public static string StripAnsiCodes(string? text)
  {
    if (string.IsNullOrEmpty(text))
      return string.Empty;

    return AnsiRegex().Replace(text, string.Empty);
  }

  /// <summary>
  /// Gets the visible length of a string, excluding ANSI escape codes.
  /// </summary>
  /// <param name="text">The text potentially containing ANSI escape codes.</param>
  /// <returns>The number of visible characters (excluding ANSI codes).</returns>
  /// <example>
  /// <code>
  /// string styled = "\x1b[31mError\x1b[0m";
  /// int length = AnsiStringUtils.GetVisibleLength(styled);
  /// // length == 5 (for "Error")
  /// </code>
  /// </example>
  public static int GetVisibleLength(string? text)
  {
    if (string.IsNullOrEmpty(text))
      return 0;

    return StripAnsiCodes(text).Length;
  }

  /// <summary>
  /// Pads a string to a specified length, accounting for ANSI codes.
  /// </summary>
  /// <param name="text">The text to pad.</param>
  /// <param name="totalWidth">The desired total visible width.</param>
  /// <param name="paddingChar">The character to use for padding (default is space).</param>
  /// <returns>The padded string with original ANSI codes preserved.</returns>
  public static string PadRightVisible(string? text, int totalWidth, char paddingChar = ' ')
  {
    if (string.IsNullOrEmpty(text))
      return new string(paddingChar, totalWidth);

    int visibleLength = GetVisibleLength(text);
    if (visibleLength >= totalWidth)
      return text;

    return text + new string(paddingChar, totalWidth - visibleLength);
  }

  /// <summary>
  /// Pads a string on the left to a specified length, accounting for ANSI codes.
  /// </summary>
  /// <param name="text">The text to pad.</param>
  /// <param name="totalWidth">The desired total visible width.</param>
  /// <param name="paddingChar">The character to use for padding (default is space).</param>
  /// <returns>The padded string with original ANSI codes preserved.</returns>
  public static string PadLeftVisible(string? text, int totalWidth, char paddingChar = ' ')
  {
    if (string.IsNullOrEmpty(text))
      return new string(paddingChar, totalWidth);

    int visibleLength = GetVisibleLength(text);
    if (visibleLength >= totalWidth)
      return text;

    return new string(paddingChar, totalWidth - visibleLength) + text;
  }

  /// <summary>
  /// Centers a string within a specified width, accounting for ANSI codes.
  /// </summary>
  /// <param name="text">The text to center.</param>
  /// <param name="totalWidth">The desired total visible width.</param>
  /// <param name="paddingChar">The character to use for padding (default is space).</param>
  /// <returns>The centered string with original ANSI codes preserved.</returns>
  public static string CenterVisible(string? text, int totalWidth, char paddingChar = ' ')
  {
    if (string.IsNullOrEmpty(text))
      return new string(paddingChar, totalWidth);

    int visibleLength = GetVisibleLength(text);
    if (visibleLength >= totalWidth)
      return text;

    int totalPadding = totalWidth - visibleLength;
    int leftPadding = totalPadding / 2;
    int rightPadding = totalPadding - leftPadding;

    return new string(paddingChar, leftPadding) + text + new string(paddingChar, rightPadding);
  }
}
