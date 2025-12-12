namespace TimeWarp.Nuru;

/// <summary>
/// Provides help display functionality for commands.
/// </summary>
public static class HelpProvider
{
  /// <summary>
  /// Gets help text for all registered routes.
  /// </summary>
  /// <param name="endpoints">The endpoint collection.</param>
  /// <param name="appName">Optional application name.</param>
  /// <param name="appDescription">Optional application description.</param>
  /// <param name="options">Optional help options for filtering.</param>
  /// <param name="context">The help context (CLI or REPL).</param>
  /// <param name="useColor">Whether to include ANSI color codes in the output.</param>
  public static string GetHelpText(
    EndpointCollection endpoints,
    string? appName = null,
    string? appDescription = null,
    HelpOptions? options = null,
    HelpContext context = HelpContext.Cli,
    bool useColor = true)
  {
    ArgumentNullException.ThrowIfNull(endpoints);
    options ??= new HelpOptions();

    List<Endpoint> routes = FilterRoutes(endpoints.Endpoints, options, context);

    if (routes.Count == 0)
    {
      return "No routes are registered.";
    }

    StringBuilder sb = new();

    // Description section
    if (!string.IsNullOrEmpty(appDescription))
    {
      sb.AppendLine(FormatSectionHeader("Description", useColor));
      sb.AppendLine("  " + FormatDescription(appDescription, useColor));
      sb.AppendLine();
    }

    // Usage section
    sb.AppendLine(FormatSectionHeader("Usage", useColor));
    sb.AppendLine("  " + FormatUsage(appName ?? GetDefaultAppName(), useColor));
    sb.AppendLine();

    // Group endpoints by description for alias grouping
    List<EndpointGroup> groups = GroupByDescription(routes);

    // Separate commands and options
    List<EndpointGroup> commandGroups = [.. groups.Where(g => !g.FirstPattern.StartsWith('-'))];
    List<EndpointGroup> optionGroups = [.. groups.Where(g => g.FirstPattern.StartsWith('-'))];

    // Commands section
    if (commandGroups.Count > 0)
    {
      sb.AppendLine(FormatSectionHeader("Commands", useColor));
      foreach (EndpointGroup group in commandGroups.OrderBy(g => g.FirstPattern))
      {
        AppendGroup(sb, group, useColor);
      }

      sb.AppendLine();
    }

    // Options section
    if (optionGroups.Count > 0)
    {
      sb.AppendLine(FormatSectionHeader("Options", useColor));
      foreach (EndpointGroup group in optionGroups.OrderBy(g => g.FirstPattern))
      {
        AppendGroup(sb, group, useColor);
      }
    }

    return sb.ToString().TrimEnd();
  }

  /// <summary>
  /// Filters routes based on help options and context.
  /// </summary>
  private static List<Endpoint> FilterRoutes(IReadOnlyList<Endpoint> endpoints, HelpOptions options, HelpContext context)
  {
    List<Endpoint> filtered = [];

    foreach (Endpoint endpoint in endpoints)
    {
      if (ShouldFilter(endpoint, options, context))
        continue;

      filtered.Add(endpoint);
    }

    return filtered;
  }

  /// <summary>
  /// Determines if an endpoint should be filtered from help output.
  /// </summary>
  private static bool ShouldFilter(Endpoint endpoint, HelpOptions options, HelpContext context)
  {
    string pattern = endpoint.RoutePattern;

    // Always filter the base help routes (--help and help) - they don't need to be shown in their own output
    if (pattern is "--help" or "--help?" or "help")
      return true;

    // Filter per-command help routes (e.g., "blog --help?")
    if (!options.ShowPerCommandHelpRoutes)
    {
      if (pattern.EndsWith(" --help", StringComparison.Ordinal) ||
          pattern.EndsWith(" --help?", StringComparison.Ordinal))
        return true;
    }

    // Filter REPL commands in CLI context
    if (!options.ShowReplCommandsInCli && context == HelpContext.Cli)
    {
      if (HelpOptions.ReplCommandPatterns.Contains(pattern))
        return true;
    }

    // Filter completion routes
    if (!options.ShowCompletionRoutes)
    {
      foreach (string prefix in HelpOptions.CompletionRoutePrefixes)
      {
        if (pattern.StartsWith(prefix, StringComparison.Ordinal))
          return true;
      }
    }

    // Filter by custom exclude patterns
    if (options.ExcludePatterns is { Count: > 0 })
    {
      foreach (string excludePattern in options.ExcludePatterns)
      {
        if (MatchesWildcard(pattern, excludePattern))
          return true;
      }
    }

    return false;
  }

  /// <summary>
  /// Groups endpoints by description for alias display.
  /// Same description = alias group (e.g., exit, quit, q all have "Exit the REPL").
  /// </summary>
  private static List<EndpointGroup> GroupByDescription(List<Endpoint> endpoints)
  {
    // Group by description (use empty string for null descriptions to satisfy dictionary constraint)
    Dictionary<string, List<Endpoint>> byDescription = [];

    foreach (Endpoint endpoint in endpoints)
    {
      string desc = endpoint.Description ?? string.Empty;

      if (!byDescription.TryGetValue(desc, out List<Endpoint>? list))
      {
        list = [];
        byDescription[desc] = list;
      }

      list.Add(endpoint);
    }

    // Convert to EndpointGroup list
    List<EndpointGroup> groups = [];

    foreach ((string description, List<Endpoint> groupEndpoints) in byDescription)
    {
      // Sort patterns within group for consistent display
      List<string> patterns = [.. groupEndpoints.OrderBy(e => e.RoutePattern).Select(e => e.RoutePattern)];

      groups.Add(new EndpointGroup
      {
        Patterns = patterns,
        Description = string.IsNullOrEmpty(description) ? null : description,
        FirstPattern = patterns[0]
      });
    }

    return groups;
  }

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

    if (!string.IsNullOrEmpty(description))
    {
      // Use ANSI-aware padding for proper alignment when colors are present
      int visibleLength = useColor ? AnsiStringUtils.GetVisibleLength(pattern) : pattern.Length;
      int padding = 30 - visibleLength;
      if (padding < 2) padding = 2;
      string formattedDesc = FormatDescription(description, useColor);
      sb.AppendLine(CultureInfo.InvariantCulture, $"  {pattern}{new string(' ', padding)}{formattedDesc}");
    }
    else
    {
      sb.AppendLine(CultureInfo.InvariantCulture, $"  {pattern}");
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
  /// Matches a pattern against a wildcard expression.
  /// Supports * to match any characters.
  /// </summary>
  private static bool MatchesWildcard(string input, string wildcardPattern)
  {
    // Convert wildcard pattern to regex
    // Escape all regex special chars except *, then replace * with .*
    string regexPattern = "^" +
      Regex.Escape(wildcardPattern).Replace("\\*", ".*", StringComparison.Ordinal) +
      "$";

    return Regex.IsMatch(input, regexPattern, RegexOptions.IgnoreCase);
  }

  /// <summary>
  /// Gets the default application name using AppNameDetector.
  /// Falls back to "nuru-app" if detection fails.
  /// </summary>
  private static string GetDefaultAppName()
  {
    try
    {
      return AppNameDetector.GetEffectiveAppName();
    }
    catch (InvalidOperationException)
    {
      return "nuru-app";
    }
  }

  #region Formatting Helpers

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

  /// <summary>
  /// Formats a section header with optional color (bold yellow).
  /// </summary>
  private static string FormatSectionHeader(string header, bool useColor)
  {
    if (!useColor)
      return header + ":";

    return header.Yellow().Bold() + ":";
  }

  /// <summary>
  /// Formats the usage line with optional color.
  /// </summary>
  private static string FormatUsage(string appName, bool useColor)
  {
    if (!useColor)
      return appName + " [command] [options]";

    return appName.Cyan() + " " + "[command]".Gray() + " " + "[options]".Gray();
  }

  /// <summary>
  /// Formats description text with optional color (gray/dim).
  /// </summary>
  private static string FormatDescription(string description, bool useColor)
  {
    if (!useColor)
      return description;

    return description.Gray();
  }

  /// <summary>
  /// Formats the "(default)" marker with optional color.
  /// </summary>
  private static string FormatDefaultMarker(bool useColor)
  {
    if (!useColor)
      return "(default)";

    return "(default)".Dim();
  }

  #endregion

  /// <summary>
  /// Represents a group of endpoints with the same description (aliases).
  /// </summary>
  private sealed class EndpointGroup
  {
    public required List<string> Patterns { get; init; }
    public string? Description { get; init; }
    public required string FirstPattern { get; init; }
  }
}
