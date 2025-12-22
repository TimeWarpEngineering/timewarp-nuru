namespace TimeWarp.Terminal;

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

  /// <summary>
  /// Wraps text at word boundaries to fit within a specified width, preserving ANSI escape codes.
  /// </summary>
  /// <param name="text">The text to wrap, potentially containing ANSI escape codes.</param>
  /// <param name="maxWidth">The maximum visible width per line.</param>
  /// <returns>A list of wrapped lines with ANSI codes properly carried across line breaks.</returns>
  /// <example>
  /// <code>
  /// string longText = "This is a very long text that needs wrapping";
  /// var lines = AnsiStringUtils.WrapText(longText, 20);
  /// // Returns multiple lines, each with visible length <= 20
  /// </code>
  /// </example>
  public static IReadOnlyList<string> WrapText(string? text, int maxWidth)
  {
    if (string.IsNullOrEmpty(text))
      return [""];

    if (maxWidth < 1)
      maxWidth = 1;

    List<string> result = [];

    // Parse the text into segments (ANSI codes and visible text)
    List<TextSegment> segments = ParseTextSegments(text);

    // Track active ANSI state to carry across lines
    StringBuilder activeAnsiState = new();
    StringBuilder currentLine = new();
    int currentLineWidth = 0;

    foreach (TextSegment segment in segments)
    {
      if (segment.IsAnsiCode)
      {
        // Track ANSI state for carrying across lines
        UpdateAnsiState(activeAnsiState, segment.Text);
        currentLine.Append(segment.Text);
      }
      else
      {
        // Process visible text - split into words
        string[] words = SplitIntoWords(segment.Text);

        foreach (string word in words)
        {
          int wordLength = word.Length;

          if (wordLength == 0)
            continue;

          // Check if word fits on current line
          if (currentLineWidth + wordLength <= maxWidth)
          {
            currentLine.Append(word);
            currentLineWidth += wordLength;
          }
          else if (currentLineWidth == 0)
          {
            // Word is longer than maxWidth, need to break it
            foreach (char c in word)
            {
              if (currentLineWidth >= maxWidth)
              {
                // Close current line with reset if we have active state
                if (activeAnsiState.Length > 0)
                  currentLine.Append(AnsiColors.Reset);
                result.Add(currentLine.ToString());

                // Start new line with active state
                currentLine.Clear();
                currentLine.Append(activeAnsiState);
                currentLineWidth = 0;
              }

              currentLine.Append(c);
              currentLineWidth++;
            }
          }
          else
          {
            // Start a new line
            // Close current line with reset if we have active state
            if (activeAnsiState.Length > 0)
              currentLine.Append(AnsiColors.Reset);
            result.Add(currentLine.ToString());

            // Start new line with active state
            currentLine.Clear();
            currentLine.Append(activeAnsiState);
            currentLineWidth = 0;

            // Handle the word on the new line
            if (wordLength <= maxWidth)
            {
              // Trim leading space if word starts with space
              string trimmedWord = word.TrimStart();
              currentLine.Append(trimmedWord);
              currentLineWidth = trimmedWord.Length;
            }
            else
            {
              // Word is longer than maxWidth, break it
              foreach (char c in word)
              {
                if (c == ' ' && currentLineWidth == 0)
                  continue; // Skip leading spaces on new line

                if (currentLineWidth >= maxWidth)
                {
                  if (activeAnsiState.Length > 0)
                    currentLine.Append(AnsiColors.Reset);
                  result.Add(currentLine.ToString());

                  currentLine.Clear();
                  currentLine.Append(activeAnsiState);
                  currentLineWidth = 0;
                }

                currentLine.Append(c);
                currentLineWidth++;
              }
            }
          }
        }
      }
    }

    // Add the last line if it has content
    if (currentLine.Length > 0)
    {
      result.Add(currentLine.ToString());
    }

    // Ensure we return at least one empty line
    if (result.Count == 0)
      result.Add("");

    return result;
  }

  /// <summary>
  /// Represents a segment of text (either ANSI code or visible text).
  /// </summary>
  private readonly struct TextSegment(string text, bool isAnsiCode)
  {
    public string Text { get; } = text;
    public bool IsAnsiCode { get; } = isAnsiCode;
  }

  /// <summary>
  /// Parses text into segments of ANSI codes and visible text.
  /// </summary>
  private static List<TextSegment> ParseTextSegments(string text)
  {
    List<TextSegment> segments = [];
    int lastEnd = 0;

    foreach (Match match in AnsiRegex().Matches(text))
    {
      // Add any text before this ANSI code
      if (match.Index > lastEnd)
      {
        segments.Add(new TextSegment(text[lastEnd..match.Index], isAnsiCode: false));
      }

      // Add the ANSI code
      segments.Add(new TextSegment(match.Value, isAnsiCode: true));
      lastEnd = match.Index + match.Length;
    }

    // Add any remaining text after the last ANSI code
    if (lastEnd < text.Length)
    {
      segments.Add(new TextSegment(text[lastEnd..], isAnsiCode: false));
    }

    return segments;
  }

  /// <summary>
  /// Splits text into "words" where each word includes any trailing whitespace.
  /// This helps preserve spacing when wrapping.
  /// </summary>
  private static string[] SplitIntoWords(string text)
  {
    List<string> words = [];
    StringBuilder current = new();

    foreach (char c in text)
    {
      if (char.IsWhiteSpace(c))
      {
        current.Append(c);
        // End of word (including trailing space)
        if (current.Length > 0)
        {
          words.Add(current.ToString());
          current.Clear();
        }
      }
      else
      {
        current.Append(c);
      }
    }

    // Add remaining text
    if (current.Length > 0)
    {
      words.Add(current.ToString());
    }

    return [.. words];
  }

  /// <summary>
  /// Updates the active ANSI state based on an ANSI code.
  /// Reset codes clear the state, other codes are accumulated.
  /// </summary>
  private static void UpdateAnsiState(StringBuilder state, string ansiCode)
  {
    // Check if this is a reset code
    if (ansiCode is "\x1b[0m" or "\x1b[m")
    {
      state.Clear();
    }
    // Check if this is an OSC 8 end hyperlink sequence (reset hyperlink)
    else if (ansiCode.StartsWith("\x1b]8;;", StringComparison.Ordinal) &&
             (ansiCode.EndsWith("\x1b\\", StringComparison.Ordinal) || ansiCode.EndsWith('\x07')))
    {
      // Check if it's an empty URL (end of hyperlink)
      string url = ansiCode.StartsWith("\x1b]8;;", StringComparison.Ordinal)
        ? ansiCode[5..^(ansiCode.EndsWith("\x1b\\", StringComparison.Ordinal) ? 2 : 1)]
        : "";

      if (string.IsNullOrEmpty(url))
      {
        // This is an end hyperlink, remove hyperlink from state
        // For simplicity, we'll just keep accumulating - hyperlinks are self-closing
      }
      else
      {
        // This is a start hyperlink, add to state
        state.Append(ansiCode);
      }
    }
    else
    {
      // Accumulate other ANSI codes (colors, styles)
      state.Append(ansiCode);
    }
  }
}
