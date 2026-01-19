namespace TimeWarp.Nuru;

/// <summary>
/// Tokenizes command-line input for completion analysis.
/// </summary>
/// <remarks>
/// <para>
/// This is the first stage of the completion pipeline. It parses raw input into
/// a structured <see cref="ParsedInput"/> that clearly identifies:
/// </para>
/// <list type="bullet">
/// <item><description>Which words are complete (followed by whitespace)</description></item>
/// <item><description>Which word (if any) is being typed</description></item>
/// <item><description>Whether the user wants completions for the next position</description></item>
/// </list>
/// <para>
/// The tokenizer handles:
/// </para>
/// <list type="bullet">
/// <item><description>Quoted strings (single and double quotes)</description></item>
/// <item><description>Escape sequences within quotes (\", \', \\)</description></item>
/// <item><description>Multiple consecutive whitespace characters</description></item>
/// <item><description>Empty input and whitespace-only input</description></item>
/// </list>
/// </remarks>
public static class InputTokenizer
{
  /// <summary>
  /// Parses raw command-line input into structured tokens for completion.
  /// </summary>
  /// <param name="input">The raw command-line input string.</param>
  /// <returns>A <see cref="ParsedInput"/> representing the tokenized input.</returns>
  /// <example>
  /// <code>
  /// // Empty input
  /// InputTokenizer.Parse("") // => ParsedInput([], null, false)
  ///
  /// // Single partial word
  /// InputTokenizer.Parse("g") // => ParsedInput([], "g", false)
  ///
  /// // Complete word with trailing space
  /// InputTokenizer.Parse("git ") // => ParsedInput(["git"], null, true)
  ///
  /// // Multiple words with partial
  /// InputTokenizer.Parse("git sta") // => ParsedInput(["git"], "sta", false)
  ///
  /// // Quoted string
  /// InputTokenizer.Parse("echo \"hello") // => ParsedInput(["echo"], "\"hello", false)
  /// </code>
  /// </example>
  public static ParsedInput Parse(string input)
  {
    if (string.IsNullOrEmpty(input))
    {
      return ParsedInput.Empty;
    }

    // Check for trailing whitespace before parsing
    bool hasTrailingSpace = char.IsWhiteSpace(input[^1]);

    // Parse all words from input
    string[] allWords = ParseWords(input);

    // If no words found (input was all whitespace)
    if (allWords.Length == 0)
    {
      return new ParsedInput([], null, hasTrailingSpace);
    }

    // If trailing space, all words are complete
    if (hasTrailingSpace)
    {
      return new ParsedInput(allWords, null, true);
    }

    // No trailing space means last word is partial
    if (allWords.Length == 1)
    {
      return new ParsedInput([], allWords[0], false);
    }

    // Multiple words: all but last are complete, last is partial
    string[] completedWords = allWords[..^1];
    string partialWord = allWords[^1];

    return new ParsedInput(completedWords, partialWord, false);
  }

  /// <summary>
  /// Parses input into individual words, respecting quotes and escapes.
  /// </summary>
  private static string[] ParseWords(string input)
  {
    List<string> words = [];
    StringBuilder currentWord = new();
    bool inQuotes = false;
    char quoteChar = '\0';
    bool escape = false;
    bool hasContent = false;

    for (int i = 0; i < input.Length; i++)
    {
      char c = input[i];

      // Handle escape sequences
      if (escape)
      {
        currentWord.Append(c);
        escape = false;
        hasContent = true;
        continue;
      }

      // Check for escape character
      if (c == '\\' && i + 1 < input.Length)
      {
        char next = input[i + 1];
        if (next == '"' || next == '\'' || next == '\\')
        {
          escape = true;
          continue;
        }
      }

      if (!inQuotes)
      {
        // Start of quoted section
        if (c == '"' || c == '\'')
        {
          inQuotes = true;
          quoteChar = c;
          // Include the quote in the word for completion context
          currentWord.Append(c);
          hasContent = true;
        }
        // Whitespace ends current word
        else if (char.IsWhiteSpace(c))
        {
          if (hasContent || currentWord.Length > 0)
          {
            words.Add(currentWord.ToString());
            currentWord.Clear();
            hasContent = false;
          }
        }
        // Regular character
        else
        {
          currentWord.Append(c);
          hasContent = true;
        }
      }
      else
      {
        // Inside quoted section
        if (c == quoteChar)
        {
          // End quoted section
          inQuotes = false;
          quoteChar = '\0';
          // Include closing quote
          currentWord.Append(c);
        }
        else
        {
          currentWord.Append(c);
        }
      }
    }

    // Add final word if any content remains
    if (hasContent || currentWord.Length > 0)
    {
      words.Add(currentWord.ToString());
    }

    return [.. words];
  }

  /// <summary>
  /// Creates a ParsedInput from pre-split arguments (for CompletionContext compatibility).
  /// </summary>
  /// <param name="args">The pre-split arguments.</param>
  /// <param name="hasTrailingSpace">Whether the original input had trailing whitespace.</param>
  /// <returns>A <see cref="ParsedInput"/> representing the tokenized input.</returns>
  /// <remarks>
  /// This overload is provided for backward compatibility with <see cref="CompletionContext"/>
  /// which already provides split arguments.
  /// </remarks>
  public static ParsedInput FromArgs(string[] args, bool hasTrailingSpace)
  {
    ArgumentNullException.ThrowIfNull(args);

    if (args.Length == 0)
    {
      return new ParsedInput([], null, hasTrailingSpace);
    }

    if (hasTrailingSpace)
    {
      return new ParsedInput(args, null, true);
    }

    if (args.Length == 1)
    {
      return new ParsedInput([], args[0], false);
    }

    return new ParsedInput(args[..^1], args[^1], false);
  }
}
