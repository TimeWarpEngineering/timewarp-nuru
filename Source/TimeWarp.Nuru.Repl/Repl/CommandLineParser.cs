namespace TimeWarp.Nuru.Repl;

/// <summary>
/// Parses command line input strings into argument arrays, handling quotes and escapes.
/// </summary>
public static class CommandLineParser
{
  /// <summary>
  /// Parses a command line string into an array of arguments, respecting quoted strings.
  /// </summary>
  /// <param name="input">The raw command line input string.</param>
  /// <returns>An array of parsed arguments.</returns>
  /// <example>
  /// <code>
  /// Parse("greet \"John Doe\" --loud")
  /// // Returns: ["greet", "John Doe", "--loud"]
  ///
  /// Parse("deploy 'staging' --message 'Deploy to staging'")
  /// // Returns: ["deploy", "staging", "--message", "Deploy to staging"]
  ///
  /// Parse("echo \"Hello\\\"World\"")
  /// // Returns: ["echo", "Hello\"World"]
  /// </code>
  /// </example>
  public static string[] Parse(string input)
  {
    if (string.IsNullOrWhiteSpace(input))
    {
      return [];
    }

    List<string> arguments = [];
    StringBuilder currentArgument = new();
    bool inQuotes = false;
    char quoteChar = '\0';
    bool escape = false;
    bool hasArgument = false; // Track if we have a valid argument (including empty quoted strings)

    for (int i = 0; i < input.Length; i++)
    {
      char c = input[i];

      if (escape)
      {
        // Handle escaped character
        currentArgument.Append(c);
        escape = false;
        hasArgument = true;
        continue;
      }

      if (c == '\\' && i + 1 < input.Length)
      {
        char next = input[i + 1];
        // Only escape quotes and backslashes
        if (next == '"' || next == '\'' || next == '\\')
        {
          escape = true;
          continue;
        }
      }

      if (!inQuotes)
      {
        if (c == '"' || c == '\'')
        {
          // Start quoted section
          inQuotes = true;
          quoteChar = c;
          hasArgument = true; // Even empty quotes count as an argument
        }
        else if (char.IsWhiteSpace(c))
        {
          // End of argument
          if (hasArgument || currentArgument.Length > 0)
          {
            arguments.Add(currentArgument.ToString());
            currentArgument.Clear();
            hasArgument = false;
          }
        }
        else
        {
          currentArgument.Append(c);
          hasArgument = true;
        }
      }
      else
      {
        if (c == quoteChar)
        {
          // End quoted section
          inQuotes = false;
          quoteChar = '\0';
        }
        else
        {
          currentArgument.Append(c);
        }
      }
    }

    // Add final argument if any
    if (hasArgument || currentArgument.Length > 0)
    {
      arguments.Add(currentArgument.ToString());
    }

    return [.. arguments];
  }
}
