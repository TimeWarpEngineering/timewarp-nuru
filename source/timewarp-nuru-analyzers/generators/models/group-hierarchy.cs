// Design-time representation of hierarchical group structure for capabilities emission.
// Separates grouped vs ungrouped routes and builds a tree from GroupPrefix values.

namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Represents a group node in the capabilities hierarchy.
/// Used during source generation to organize routes by their group prefixes.
/// </summary>
/// <param name="Name">The single-word group name (e.g., "admin", "config")</param>
internal sealed class GroupNode(string Name)
{
  /// <summary>
  /// Gets the single-word group name.
  /// </summary>
  public string Name { get; } = Name;

  /// <summary>
  /// Gets or sets nested child groups.
  /// </summary>
  public List<GroupNode> Children { get; } = [];

  /// <summary>
  /// Gets or sets routes directly in this group (not in nested groups).
  /// </summary>
  public List<RouteDefinition> Routes { get; } = [];
}

/// <summary>
/// Result of building the group hierarchy from routes.
/// </summary>
/// <param name="RootGroups">Top-level groups in the hierarchy</param>
/// <param name="UngroupedRoutes">Routes without any group prefix</param>
internal sealed record GroupHierarchyResult(
  IReadOnlyList<GroupNode> RootGroups,
  IReadOnlyList<RouteDefinition> UngroupedRoutes);

/// <summary>
/// Builds a hierarchical group structure from route definitions.
/// Parses GroupPrefix values and organizes routes into a tree structure.
/// </summary>
internal static class GroupHierarchyBuilder
{
  /// <summary>
  /// Builds the group hierarchy from a collection of routes.
  /// Routes with GroupPrefix are organized hierarchically; others are returned as ungrouped.
  /// </summary>
  /// <param name="routes">All routes to organize</param>
  /// <returns>Hierarchy result with root groups and ungrouped routes</returns>
  /// <example>
  /// Given routes with prefixes:
  /// - "admin" (route: "admin status")
  /// - "admin config" (route: "admin config get {key}")
  /// - null (route: "version")
  ///
  /// Result:
  /// - RootGroups: [{Name: "admin", Children: [{Name: "config", Routes: [...]}], Routes: [...]}]
  /// - UngroupedRoutes: [version route]
  /// </example>
  public static GroupHierarchyResult BuildHierarchy(IEnumerable<RouteDefinition> routes)
  {
    List<RouteDefinition> ungrouped = [];
    Dictionary<string, GroupNode> rootGroupsByName = new(StringComparer.OrdinalIgnoreCase);

    foreach (RouteDefinition route in routes)
    {
      if (string.IsNullOrEmpty(route.GroupPrefix))
      {
        ungrouped.Add(route);
        continue;
      }

      // Parse GroupPrefix into path segments (e.g., "admin config" -> ["admin", "config"])
      string[] segments = route.GroupPrefix.Split(' ', StringSplitOptions.RemoveEmptyEntries);

      if (segments.Length == 0)
      {
        ungrouped.Add(route);
        continue;
      }

      // Get or create the root group
      string rootName = segments[0];
      if (!rootGroupsByName.TryGetValue(rootName, out GroupNode? currentNode))
      {
        currentNode = new GroupNode(rootName);
        rootGroupsByName[rootName] = currentNode;
      }

      // Navigate/create nested groups for remaining segments
      for (int i = 1; i < segments.Length; i++)
      {
        string childName = segments[i];
        GroupNode? childNode = currentNode.Children.Find(c =>
          string.Equals(c.Name, childName, StringComparison.OrdinalIgnoreCase));

        if (childNode is null)
        {
          childNode = new GroupNode(childName);
          currentNode.Children.Add(childNode);
        }

        currentNode = childNode;
      }

      // Add route to the deepest group
      currentNode.Routes.Add(route);
    }

    // Sort groups alphabetically for consistent output
    List<GroupNode> rootGroups = [.. rootGroupsByName.Values.OrderBy(g => g.Name, StringComparer.OrdinalIgnoreCase)];
    SortGroupsRecursively(rootGroups);

    return new GroupHierarchyResult(rootGroups, ungrouped);
  }

  /// <summary>
  /// Recursively sorts child groups and routes within each group.
  /// </summary>
  private static void SortGroupsRecursively(List<GroupNode> groups)
  {
    foreach (GroupNode group in groups)
    {
      group.Children.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
      group.Routes.Sort((a, b) => string.Compare(a.FullPattern, b.FullPattern, StringComparison.OrdinalIgnoreCase));
      SortGroupsRecursively(group.Children);
    }
  }
}
