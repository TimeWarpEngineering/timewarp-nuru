namespace TimeWarp.Nuru;

/// <summary>
/// Provides help display functionality for commands.
/// </summary>
public static class HelpProvider
{

  /// <summary>
  /// Gets help text for all registered routes.
  /// </summary>
  public static string GetHelpText(EndpointCollection endpoints)
  {
    ArgumentNullException.ThrowIfNull(endpoints);
    IReadOnlyList<Endpoint> routes = endpoints.Endpoints;

    if (routes.Count == 0)
    {
      return "No routes are registered.";
    }

    var sb = new StringBuilder();
    sb.AppendLine("Available Routes:");
    sb.AppendLine();

    // Group routes by their command prefix
    Dictionary<string, List<Endpoint>> groupedRoutes = GroupRoutesByPrefix(routes);

    // Display ungrouped routes first
    if (groupedRoutes.TryGetValue("", out List<Endpoint>? ungroupedRoutes))
    {
      foreach (Endpoint route in ungroupedRoutes)
      {
        AppendRoute(sb, route);
      }

      if (groupedRoutes.Count > 1)
      {
        sb.AppendLine();
      }
    }

    // Display grouped routes
    foreach (KeyValuePair<string, List<Endpoint>> group in groupedRoutes.Where(g => !string.IsNullOrEmpty(g.Key)).OrderBy(g => g.Key))
    {
      sb.AppendLine(CultureInfo.InvariantCulture, $"{group.Key} Commands:");
      foreach (Endpoint? route in group.Value)
      {
        AppendRoute(sb, route, indent: true);
      }

      sb.AppendLine();
    }

    return sb.ToString().TrimEnd();
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
