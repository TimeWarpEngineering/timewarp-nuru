namespace TimeWarp.Nuru;

/// <summary>
/// HelpProvider - route filtering, wildcard matching, and endpoint grouping.
/// </summary>
public static partial class HelpProvider
{
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

      // Use the message type from the first endpoint in the group
      MessageType messageType = groupEndpoints[0].MessageType;

      groups.Add(new EndpointGroup
      {
        Patterns = patterns,
        Description = string.IsNullOrEmpty(description) ? null : description,
        FirstPattern = patterns[0],
        MessageType = messageType
      });
    }

    return groups;
  }

  /// <summary>
  /// Represents a group of endpoints with the same description (aliases).
  /// </summary>
  private sealed class EndpointGroup
  {
    public required List<string> Patterns { get; init; }
    public string? Description { get; init; }
    public required string FirstPattern { get; init; }
    public MessageType MessageType { get; init; } = MessageType.Command;
  }
}
