namespace TimeWarp.Nuru;

/// <summary>
/// HelpProvider - pattern formatting and display conversion logic.
/// </summary>
public static partial class HelpProvider
{
  /// <summary>
  /// Appends a group (potentially with multiple alias patterns) to the help output.
  /// </summary>
  private static void AppendGroup(StringBuilder sb, EndpointGroup group, bool useColor)
  {
    // Check if this group contains a default route (empty pattern)
    bool hasDefaultRoute = group.Patterns.Contains(string.Empty);

    // Format all non-empty patterns (convert {x} to <x>)
    List<string> formattedPatterns = [.. group.Patterns
      .Where(p => !string.IsNullOrEmpty(p))
      .Select(p => FormatCommandPattern(p, useColor))];

    // If only default route exists with no other patterns, show "(default)"
    if (formattedPatterns.Count == 0 && hasDefaultRoute)
    {
      formattedPatterns.Add(FormatDefaultMarker(useColor));
    }
    // If there are patterns alongside a default route, append "(default)" indicator
    else if (hasDefaultRoute && formattedPatterns.Count > 0)
    {
      formattedPatterns[0] += " " + FormatDefaultMarker(useColor);
    }

    // Join patterns with comma for alias display
    string pattern = string.Join(", ", formattedPatterns);
    string? description = group.Description;

    // Format message type indicator
    string messageTypeIndicator = FormatMessageTypeIndicator(group.MessageType, useColor);

    if (!string.IsNullOrEmpty(description))
    {
      // Use ANSI-aware padding for proper alignment when colors are present
      int visibleLength = useColor ? AnsiStringUtils.GetVisibleLength(pattern) : pattern.Length;
      int indicatorVisibleLength = useColor ? AnsiStringUtils.GetVisibleLength(messageTypeIndicator) : messageTypeIndicator.Length;
      int padding = 26 - visibleLength - indicatorVisibleLength;
      if (padding < 2) padding = 2;
      string formattedDesc = FormatDescription(description, useColor);
      sb.AppendLine(CultureInfo.InvariantCulture, $"  {pattern} {messageTypeIndicator}{new string(' ', padding)}{formattedDesc}");
    }
    else
    {
      sb.AppendLine(CultureInfo.InvariantCulture, $"  {pattern} {messageTypeIndicator}");
    }
  }

  /// <summary>
  /// Formats a command pattern for display with optional syntax coloring.
  /// Converts {x} to &lt;x&gt; and applies colors: commands in cyan, parameters in yellow, options in green.
  /// </summary>
  private static string FormatCommandPattern(string pattern, bool useColor)
  {
    if (!useColor)
    {
      // Plain text formatting - parse pattern to handle optional parameters properly
      return FormatPlainPattern(pattern);
    }

    // Apply syntax coloring by parsing the pattern
    StringBuilder result = new();
    int i = 0;

    while (i < pattern.Length)
    {
      if (pattern[i] == '{')
      {
        // Find the closing brace
        int closeBrace = pattern.IndexOf('}', i);
        if (closeBrace > i)
        {
          // Extract parameter content (between braces)
          string paramContent = pattern[(i + 1)..closeBrace];

          // Handle optional marker (?)
          bool isOptional = paramContent.EndsWith('?') || pattern[i + 1] == '?';
          if (paramContent.EndsWith('?'))
            paramContent = paramContent[..^1];
          if (paramContent.StartsWith('?'))
            paramContent = paramContent[1..];

          // Handle typed parameters (name:type or type|description)
          string displayName = paramContent;
          if (paramContent.Contains(':', StringComparison.Ordinal))
            displayName = paramContent.Split(':')[0];
          if (paramContent.Contains('|', StringComparison.Ordinal))
            displayName = paramContent.Split('|')[0];

          // Format as <name> with yellow color for parameters
          string bracketColor = isOptional ? AnsiColors.Gray : "";
          string paramColor = AnsiColors.Yellow;
          string openBracket = isOptional ? "[" : "<";
          string closeBracket = isOptional ? "]" : ">";

          if (isOptional)
          {
            result.Append(bracketColor);
            result.Append(openBracket);
            result.Append(AnsiColors.Reset);
          }
          else
          {
            result.Append(openBracket);
          }

          result.Append(paramColor);
          result.Append(displayName);
          result.Append(AnsiColors.Reset);

          if (isOptional)
          {
            result.Append(bracketColor);
            result.Append(closeBracket);
            result.Append(AnsiColors.Reset);
          }
          else
          {
            result.Append(closeBracket);
          }

          i = closeBrace + 1;
          continue;
        }
      }
      else if (pattern[i] == '-')
      {
        // Options (--flag or -f) in green
        int optionEnd = i + 1;
        while (optionEnd < pattern.Length && (char.IsLetterOrDigit(pattern[optionEnd]) || pattern[optionEnd] == '-'))
        {
          optionEnd++;
        }

        // Handle optional marker after option name
        if (optionEnd < pattern.Length && pattern[optionEnd] == '?')
        {
          string optionName = pattern[i..optionEnd];
          result.Append(AnsiColors.Gray);
          result.Append('[');
          result.Append(AnsiColors.Green);
          result.Append(optionName);
          result.Append(AnsiColors.Gray);
          result.Append(']');
          result.Append(AnsiColors.Reset);
          i = optionEnd + 1;
        }
        else
        {
          string optionName = pattern[i..optionEnd];
          result.Append(AnsiColors.Green);
          result.Append(optionName);
          result.Append(AnsiColors.Reset);
          i = optionEnd;
        }

        continue;
      }
      else if (pattern[i] == '*')
      {
        // Catch-all in magenta
        result.Append(AnsiColors.Magenta);
        result.Append("...");
        result.Append(AnsiColors.Reset);
        i++;
        continue;
      }
      else if (pattern[i] == ' ')
      {
        result.Append(' ');
        i++;
        continue;
      }
      else if (pattern[i] == ',')
      {
        result.Append(',');
        i++;
        continue;
      }

      // Command literals in cyan - find the extent of the literal
      int literalEnd = i;
      while (literalEnd < pattern.Length &&
             pattern[literalEnd] != ' ' &&
             pattern[literalEnd] != '{' &&
             pattern[literalEnd] != '-' &&
             pattern[literalEnd] != '*' &&
             pattern[literalEnd] != ',')
      {
        literalEnd++;
      }

      if (literalEnd > i)
      {
        string literal = pattern[i..literalEnd];
        result.Append(AnsiColors.Cyan);
        result.Append(literal);
        result.Append(AnsiColors.Reset);
        i = literalEnd;
      }
      else
      {
        // Fallback - just append the character
        result.Append(pattern[i]);
        i++;
      }
    }

    return result.ToString();
  }

  /// <summary>
  /// Formats a pattern for plain text display.
  /// Converts {x} to &lt;x&gt;, optional parameters to [x], and handles options.
  /// </summary>
  private static string FormatPlainPattern(string pattern)
  {
    StringBuilder result = new();
    int i = 0;

    while (i < pattern.Length)
    {
      if (pattern[i] == '{')
      {
        // Find the closing brace
        int closeBrace = pattern.IndexOf('}', i);
        if (closeBrace > i)
        {
          // Extract parameter content
          string paramContent = pattern[(i + 1)..closeBrace];

          // Handle optional marker
          bool isOptional = paramContent.EndsWith('?') || pattern[i + 1] == '?';
          if (paramContent.EndsWith('?'))
            paramContent = paramContent[..^1];
          if (paramContent.StartsWith('?'))
            paramContent = paramContent[1..];

          // Handle typed parameters - extract just the name
          string displayName = paramContent;
          if (paramContent.Contains(':', StringComparison.Ordinal))
            displayName = paramContent.Split(':')[0];
          if (paramContent.Contains('|', StringComparison.Ordinal))
            displayName = paramContent.Split('|')[0];

          // Format with appropriate brackets
          if (isOptional)
          {
            result.Append('[');
            result.Append(displayName);
            result.Append(']');
          }
          else
          {
            result.Append('<');
            result.Append(displayName);
            result.Append('>');
          }

          i = closeBrace + 1;
          continue;
        }
      }
      else if (pattern[i] == '-')
      {
        // Handle options (--flag or -f), including optional ones (--flag?)
        int optionEnd = i + 1;
        while (optionEnd < pattern.Length && (char.IsLetterOrDigit(pattern[optionEnd]) || pattern[optionEnd] == '-'))
        {
          optionEnd++;
        }

        string optionName = pattern[i..optionEnd];

        // Check for optional marker after option
        if (optionEnd < pattern.Length && pattern[optionEnd] == '?')
        {
          result.Append('[');
          result.Append(optionName);
          result.Append(']');
          i = optionEnd + 1;
        }
        else
        {
          result.Append(optionName);
          i = optionEnd;
        }

        continue;
      }
      else if (pattern[i] == '*')
      {
        result.Append("...");
        i++;
        continue;
      }

      result.Append(pattern[i]);
      i++;
    }

    return result.ToString();
  }
}
