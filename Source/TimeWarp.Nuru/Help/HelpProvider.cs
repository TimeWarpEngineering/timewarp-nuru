namespace TimeWarp.Nuru;

/// <summary>
/// Provides help display functionality for commands.
/// </summary>
public static class HelpProvider
{

  /// <summary>
  /// Gets help text for all registered routes.
  /// </summary>
  public static string GetHelpText(EndpointCollection endpoints, string? appName = null, string? appDescription = null)
  {
    ArgumentNullException.ThrowIfNull(endpoints);
    List<Endpoint> routes = FilterHelpRoutes(endpoints.Endpoints);

    if (routes.Count == 0)
    {
      return "No routes are registered.";
    }

    var sb = new StringBuilder();

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

    // Separate commands and options
    var commands = routes.Where(r => !r.RoutePattern.StartsWith("--", StringComparison.Ordinal)).ToList();
    var options = routes.Where(r => r.RoutePattern.StartsWith("--", StringComparison.Ordinal)).ToList();

    // Commands section
    if (commands.Count > 0)
    {
      sb.AppendLine("Commands:");
      foreach (Endpoint command in commands.OrderBy(c => c.RoutePattern))
      {
        AppendCommand(sb, command);
      }

      sb.AppendLine();
    }

    // Options section
    if (options.Count > 0)
    {
      sb.AppendLine("Options:");
      foreach (Endpoint option in options.OrderBy(o => o.RoutePattern))
      {
        AppendOption(sb, option);
      }
    }

    return sb.ToString().TrimEnd();
  }

  /// <summary>
  /// Filters out help routes from the endpoint list.
  /// </summary>
  private static List<Endpoint> FilterHelpRoutes(IReadOnlyList<Endpoint> endpoints)
  {
    var filtered = new List<Endpoint>();

    foreach (Endpoint endpoint in endpoints)
    {
      string pattern = endpoint.RoutePattern;

      // Skip help routes
      if (pattern == "help" || pattern == "--help")
        continue;

      // Skip command-specific help routes (e.g., "git --help", "add --help")
      if (pattern.EndsWith(" --help", StringComparison.Ordinal))
        continue;

      filtered.Add(endpoint);
    }

    return filtered;
  }

  /// <summary>
  /// Appends a command to the string builder with proper formatting.
  /// </summary>
  private static void AppendCommand(StringBuilder sb, Endpoint command)
  {
    string pattern = FormatCommandPattern(command.RoutePattern);
    string? description = command.Description;

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
  /// Appends an option to the string builder with proper formatting.
  /// </summary>
  private static void AppendOption(StringBuilder sb, Endpoint option)
  {
    string pattern = option.RoutePattern;
    string? description = option.Description;

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

  private static void AppendRoute(StringBuilder sb, Endpoint route, bool indent = false)
  {
    string prefix = indent ? "  " : "";
    string pattern = route.RoutePattern;
    string? description = route.Description;

    if (!string.IsNullOrEmpty(description))
    {
      // Calculate padding for alignment
      int padding = 40 - pattern.Length - prefix.Length;
      if (padding < 2) padding = 2;

      sb.AppendLine(CultureInfo.InvariantCulture, $"{prefix}{pattern}{new string(' ', padding)}{description}");
    }
    else
    {
      sb.AppendLine(CultureInfo.InvariantCulture, $"{prefix}{pattern}");
    }
  }

  private static Dictionary<string, List<Endpoint>> GroupRoutesByPrefix(IReadOnlyList<Endpoint> routes)
  {
    var groups = new Dictionary<string, List<Endpoint>>();

    foreach (Endpoint? route in routes.OrderBy(r => r.RoutePattern))
    {
      string prefix = GetCommandPrefix(route.RoutePattern);

      if (!groups.TryGetValue(prefix, out List<Endpoint>? list))
      {
        list = [];
        groups[prefix] = list;
      }

      list.Add(route);
    }

    return groups;
  }

  private static string GetCommandPrefix(string routePattern)
  {
    // Skip catch-all patterns
    if (routePattern.StartsWith('{'))
    {
      return "";
    }

    // Extract the first word as the command prefix
    string[] parts = routePattern.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length > 1)
    {
      // Multi-part command, use first part as group (e.g., "git" from "git status")
      return char.ToUpper(parts[0][0], CultureInfo.InvariantCulture) + parts[0].Substring(1);
    }

    // Single word command, no grouping
    return "";
  }
}
