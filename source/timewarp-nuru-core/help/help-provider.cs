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
  public static string GetHelpText(
    EndpointCollection endpoints,
    string? appName = null,
    string? appDescription = null,
    HelpOptions? options = null,
    HelpContext context = HelpContext.Cli)
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
      sb.AppendLine("Description:");
      sb.AppendLine("  " + appDescription);
      sb.AppendLine();
    }

    // Usage section
    sb.AppendLine("Usage:");
    sb.AppendLine("  " + (appName ?? "nuru-app") + " [command] [options]");
    sb.AppendLine();

    // Group endpoints by description for alias grouping
    List<EndpointGroup> groups = GroupByDescription(routes);

    // Separate commands and options
    List<EndpointGroup> commandGroups = [.. groups.Where(g => !g.FirstPattern.StartsWith('-'))];
    List<EndpointGroup> optionGroups = [.. groups.Where(g => g.FirstPattern.StartsWith('-'))];

    // Commands section
    if (commandGroups.Count > 0)
    {
      sb.AppendLine("Commands:");
      foreach (EndpointGroup group in commandGroups.OrderBy(g => g.FirstPattern))
      {
        AppendGroup(sb, group);
      }

      sb.AppendLine();
    }

    // Options section
    if (optionGroups.Count > 0)
    {
      sb.AppendLine("Options:");
      foreach (EndpointGroup group in optionGroups.OrderBy(g => g.FirstPattern))
      {
        AppendGroup(sb, group);
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
  private static void AppendGroup(StringBuilder sb, EndpointGroup group)
  {
    // Format all patterns (convert {x} to <x>)
    List<string> formattedPatterns = [.. group.Patterns.Select(FormatCommandPattern)];

    // Join patterns with comma for alias display
    string pattern = string.Join(", ", formattedPatterns);
    string? description = group.Description;

    if (!string.IsNullOrEmpty(description))
    {
      int padding = 30 - pattern.Length;
      if (padding < 2) padding = 2;
      sb.AppendLine(CultureInfo.InvariantCulture, $"  {pattern}{new string(' ', padding)}{description}");
    }
    else
    {
      sb.AppendLine(CultureInfo.InvariantCulture, $"  {pattern}");
    }
  }

  /// <summary>
  /// Formats a command pattern for display (e.g., "greet {name}" -> "greet <name>").
  /// </summary>
  private static string FormatCommandPattern(string pattern)
  {
    return pattern
      .Replace("{", "<", StringComparison.Ordinal)
      .Replace("}", ">", StringComparison.Ordinal)
      .Replace("{?", "<", StringComparison.Ordinal)
      .Replace("?}", ">", StringComparison.Ordinal)
      .Replace("{:", "<", StringComparison.Ordinal)
      .Replace("*", "...", StringComparison.Ordinal);
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
  /// Represents a group of endpoints with the same description (aliases).
  /// </summary>
  private sealed class EndpointGroup
  {
    public required List<string> Patterns { get; init; }
    public string? Description { get; init; }
    public required string FirstPattern { get; init; }
  }
}
