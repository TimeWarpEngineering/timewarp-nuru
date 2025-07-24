namespace TimeWarp.Nuru.Help;

/// <summary>
/// Provides help display functionality for route-based commands.
/// </summary>
public class RouteHelpProvider
{
  private readonly EndpointCollection Endpoints;

  public RouteHelpProvider(EndpointCollection endpoints)
  {
    Endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
  }

  /// <summary>
  /// Displays help for all registered routes.
  /// </summary>
  public void ShowHelp()
  {
    IReadOnlyList<RouteEndpoint> routes = Endpoints.Endpoints;

    if (routes.Count == 0)
    {
      Console.WriteLine("No routes are registered.");
      return;
    }

    Console.WriteLine("Available Routes:");
    Console.WriteLine();

    // Group routes by their command prefix
    Dictionary<string, List<RouteEndpoint>> groupedRoutes = GroupRoutesByPrefix(routes);

    // Display ungrouped routes first
    if (groupedRoutes.TryGetValue("", out List<RouteEndpoint>? ungroupedRoutes))
    {
      foreach (RouteEndpoint route in ungroupedRoutes)
      {
        DisplayRoute(route);
      }

      if (groupedRoutes.Count > 1)
      {
        Console.WriteLine();
      }
    }

    // Display grouped routes
    foreach (KeyValuePair<string, List<RouteEndpoint>> group in groupedRoutes.Where(g => !string.IsNullOrEmpty(g.Key)).OrderBy(g => g.Key))
    {
      Console.WriteLine($"{group.Key} Commands:");
      foreach (RouteEndpoint? route in group.Value)
      {
        DisplayRoute(route, indent: true);
      }

      Console.WriteLine();
    }
  }

  private static void DisplayRoute(RouteEndpoint route, bool indent = false)
  {
    string prefix = indent ? "  " : "";
    string pattern = route.RoutePattern;
    string? description = route.Description;

    if (!string.IsNullOrEmpty(description))
    {
      // Calculate padding for alignment
      int padding = 40 - pattern.Length - prefix.Length;
      if (padding < 2) padding = 2;

      Console.WriteLine($"{prefix}{pattern}{new string(' ', padding)}{description}");
    }
    else
    {
      Console.WriteLine($"{prefix}{pattern}");
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
