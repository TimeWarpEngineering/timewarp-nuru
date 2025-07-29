namespace TimeWarp.Nuru.Help;

/// <summary>
/// Provides help display functionality for route-based commands.
/// </summary>
public static class RouteHelpProvider
{

  /// <summary>
  /// Gets help text for all registered routes.
  /// </summary>
  public static string GetHelpText(EndpointCollection endpoints)
  {
    ArgumentNullException.ThrowIfNull(endpoints);
    IReadOnlyList<RouteEndpoint> routes = endpoints.Endpoints;

    if (routes.Count == 0)
    {
      return "No routes are registered.";
    }

    var sb = new System.Text.StringBuilder();
    sb.AppendLine("Available Routes:");
    sb.AppendLine();

    // Group routes by their command prefix
    Dictionary<string, List<RouteEndpoint>> groupedRoutes = GroupRoutesByPrefix(routes);

    // Display ungrouped routes first
    if (groupedRoutes.TryGetValue("", out List<RouteEndpoint>? ungroupedRoutes))
    {
      foreach (RouteEndpoint route in ungroupedRoutes)
      {
        AppendRoute(sb, route);
      }

      if (groupedRoutes.Count > 1)
      {
        sb.AppendLine();
      }
    }

    // Display grouped routes
    foreach (KeyValuePair<string, List<RouteEndpoint>> group in groupedRoutes.Where(g => !string.IsNullOrEmpty(g.Key)).OrderBy(g => g.Key))
    {
      sb.AppendLine($"{group.Key} Commands:");
      foreach (RouteEndpoint? route in group.Value)
      {
        AppendRoute(sb, route, indent: true);
      }

      sb.AppendLine();
    }

    return sb.ToString().TrimEnd();
  }

  private static void AppendRoute(System.Text.StringBuilder sb, RouteEndpoint route, bool indent = false)
  {
    string prefix = indent ? "  " : "";
    string pattern = route.RoutePattern;
    string? description = route.Description;

    if (!string.IsNullOrEmpty(description))
    {
      // Calculate padding for alignment
      int padding = 40 - pattern.Length - prefix.Length;
      if (padding < 2) padding = 2;

      sb.AppendLine($"{prefix}{pattern}{new string(' ', padding)}{description}");
    }
    else
    {
      sb.AppendLine($"{prefix}{pattern}");
    }
  }

  private static Dictionary<string, List<RouteEndpoint>> GroupRoutesByPrefix(IReadOnlyList<RouteEndpoint> routes)
  {
    var groups = new Dictionary<string, List<RouteEndpoint>>();

    foreach (RouteEndpoint? route in routes.OrderBy(r => r.RoutePattern))
    {
      string prefix = GetCommandPrefix(route.RoutePattern);

      if (!groups.TryGetValue(prefix, out List<RouteEndpoint>? list))
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
