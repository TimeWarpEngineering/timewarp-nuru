namespace TimeWarp.Nuru.Help;

/// <summary>
/// Provides help display functionality for route-based commands.
/// </summary>
public class RouteHelpProvider
{
    private readonly EndpointCollection _endpoints;

    public RouteHelpProvider(EndpointCollection endpoints)
    {
        _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
    }

    /// <summary>
    /// Displays help for all registered routes.
    /// </summary>
    public void ShowHelp()
    {
        var routes = _endpoints.Endpoints;

        if (!routes.Any())
        {
            System.Console.WriteLine("No routes are registered.");
            return;
        }

        System.Console.WriteLine("Available Routes:");
        System.Console.WriteLine();

        // Group routes by their command prefix
        var groupedRoutes = GroupRoutesByPrefix(routes);

        // Display ungrouped routes first
        if (groupedRoutes.ContainsKey(""))
        {
            foreach (var route in groupedRoutes[""])
            {
                DisplayRoute(route);
            }

            if (groupedRoutes.Count > 1)
            {
                System.Console.WriteLine();
            }
        }

        // Display grouped routes
        foreach (var group in groupedRoutes.Where(g => !string.IsNullOrEmpty(g.Key)).OrderBy(g => g.Key))
        {
            System.Console.WriteLine($"{group.Key} Commands:");
            foreach (var route in group.Value)
            {
                DisplayRoute(route, indent: true);
            }
            System.Console.WriteLine();
        }
    }

    private void DisplayRoute(RouteEndpoint route, bool indent = false)
    {
        var prefix = indent ? "  " : "";
        var pattern = route.RoutePattern;
        var description = route.Description;

        if (!string.IsNullOrEmpty(description))
        {
            // Calculate padding for alignment
            var padding = 40 - pattern.Length - prefix.Length;
            if (padding < 2) padding = 2;

            System.Console.WriteLine($"{prefix}{pattern}{new string(' ', padding)}{description}");
        }
        else
        {
            System.Console.WriteLine($"{prefix}{pattern}");
        }
    }

    private Dictionary<string, List<RouteEndpoint>> GroupRoutesByPrefix(IReadOnlyList<RouteEndpoint> routes)
    {
        var groups = new Dictionary<string, List<RouteEndpoint>>();

        foreach (var route in routes.OrderBy(r => r.RoutePattern))
        {
            var prefix = GetCommandPrefix(route.RoutePattern);

            if (!groups.ContainsKey(prefix))
            {
                groups[prefix] = new List<RouteEndpoint>();
            }

            groups[prefix].Add(route);
        }

        return groups;
    }

    private string GetCommandPrefix(string routePattern)
    {
        // Skip catch-all patterns
        if (routePattern.StartsWith("{"))
        {
            return "";
        }

        // Extract the first word as the command prefix
        var parts = routePattern.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 1)
        {
            // Multi-part command, use first part as group (e.g., "git" from "git status")
            return char.ToUpper(parts[0][0]) + parts[0].Substring(1);
        }

        // Single word command, no grouping
        return "";
    }
}