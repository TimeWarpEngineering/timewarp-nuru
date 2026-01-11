// Overlap validator that detects routes with same structure but different type constraints.
//
// This validator compares routes by their "structure signature" - a normalized form
// that ignores parameter names and type constraints, leaving only:
// - Literal segments
// - Parameter positions (marked as {P}, {P?}, or {*})
// - Option names (without value types)
//
// Two routes with the same signature but different constraints produce NURU_R001.

namespace TimeWarp.Nuru.Validation;

using TimeWarp.Nuru.Generators;

/// <summary>
/// Validates that routes don't have overlapping structures with different type constraints.
/// </summary>
internal static class OverlapValidator
{
  /// <summary>
  /// Validates routes for overlapping structure with different type constraints.
  /// </summary>
  /// <param name="routes">The routes to validate.</param>
  /// <param name="routeLocations">Map from route pattern to source location for error reporting.</param>
  /// <returns>Diagnostics for any overlapping routes found.</returns>
  public static ImmutableArray<Diagnostic> Validate(
    ImmutableArray<RouteDefinition> routes,
    IReadOnlyDictionary<string, Location> routeLocations)
  {
    if (routes.Length < 2)
      return [];

    List<Diagnostic> diagnostics = [];

    // Group routes by their structure signature
    Dictionary<string, List<RouteDefinition>> routesBySignature = [];

    foreach (RouteDefinition route in routes)
    {
      string signature = ComputeStructureSignature(route);

      if (!routesBySignature.TryGetValue(signature, out List<RouteDefinition>? group))
      {
        group = [];
        routesBySignature[signature] = group;
      }

      group.Add(route);
    }

    // Check each group for conflicts
    foreach (KeyValuePair<string, List<RouteDefinition>> entry in routesBySignature)
    {
      List<RouteDefinition> group = entry.Value;
      if (group.Count < 2)
        continue;

      // Check if routes in this group have different type constraints
      ImmutableArray<Diagnostic> groupDiagnostics = CheckGroupForTypeConflicts(group, routeLocations);
      diagnostics.AddRange(groupDiagnostics);
    }

    return [.. diagnostics];
  }

  /// <summary>
  /// Computes a structure signature for a route that ignores parameter names and types.
  /// </summary>
  /// <remarks>
  /// Examples:
  /// - "get {id:int}"      -> "get {P}"
  /// - "get {name:string}" -> "get {P}"
  /// - "get {x}"           -> "get {P}"
  /// - "get {id:int?}"     -> "get {P?}"
  /// - "get {*args}"       -> "get {*}"
  /// - "--verbose"         -> "--verbose"
  /// - "--output {path}"   -> "--output {P}"
  /// </remarks>
  private static string ComputeStructureSignature(RouteDefinition route)
  {
    // Use the parsed pattern segments to build signature
    StringBuilder signature = new();

    // Include group prefix if present
    if (!string.IsNullOrEmpty(route.GroupPrefix))
    {
      signature.Append(route.GroupPrefix);
    }

    foreach (SegmentDefinition segment in route.Segments)
    {
      if (signature.Length > 0)
        signature.Append(' ');

      switch (segment)
      {
        case LiteralDefinition literal:
          signature.Append(literal.Value);
          break;

        case ParameterDefinition param:
          if (param.IsCatchAll)
          {
            signature.Append("{*}");
          }
          else if (param.IsOptional)
          {
            signature.Append("{P?}");
          }
          else
          {
            signature.Append("{P}");
          }

          break;

        case OptionDefinition option:
          // Include option name (long form preferred) but normalize parameter
          string optionName = option.LongFormWithPrefix ?? option.ShortFormWithPrefix ?? "--option";
          signature.Append(optionName);

          if (option.ExpectsValue)
          {
            if (option.ParameterIsOptional)
            {
              signature.Append(" {P?}");
            }
            else
            {
              signature.Append(" {P}");
            }
          }

          break;
      }
    }

    return signature.ToString();
  }

  /// <summary>
  /// Checks a group of routes with the same signature for type constraint conflicts.
  /// </summary>
  private static ImmutableArray<Diagnostic> CheckGroupForTypeConflicts(
    List<RouteDefinition> group,
    IReadOnlyDictionary<string, Location> routeLocations)
  {
    List<Diagnostic> diagnostics = [];

    // Compare each pair
    for (int i = 0; i < group.Count; i++)
    {
      for (int j = i + 1; j < group.Count; j++)
      {
        RouteDefinition route1 = group[i];
        RouteDefinition route2 = group[j];

        // Check if they have different type constraints
        if (HaveDifferentTypeConstraints(route1, route2))
        {
          // Get location for the first route (or use a default)
          Location location = routeLocations.TryGetValue(route1.OriginalPattern, out Location? loc)
            ? loc
            : Location.None;

          Diagnostic diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.OverlappingTypeConstraints,
            location,
            route1.OriginalPattern,
            route2.OriginalPattern);

          diagnostics.Add(diagnostic);
        }
      }
    }

    return [.. diagnostics];
  }

  /// <summary>
  /// Determines if two routes have different type constraints at corresponding positions.
  /// </summary>
  private static bool HaveDifferentTypeConstraints(RouteDefinition route1, RouteDefinition route2)
  {
    // If patterns are identical, they're duplicates (not type conflicts)
    // That's a different error
    if (route1.OriginalPattern == route2.OriginalPattern)
      return false;

    // Get parameter segments from each route
    List<ParameterDefinition> params1 = [.. route1.Parameters];
    List<ParameterDefinition> params2 = [.. route2.Parameters];

    // Must have same number of parameters (since signatures match)
    if (params1.Count != params2.Count)
      return false;

    // Check each parameter pair
    for (int i = 0; i < params1.Count; i++)
    {
      ParameterDefinition p1 = params1[i];
      ParameterDefinition p2 = params2[i];

      // Compare type constraints
      // Different if: one has type and other doesn't, or both have different types
      bool p1HasType = !string.IsNullOrEmpty(p1.TypeConstraint);
      bool p2HasType = !string.IsNullOrEmpty(p2.TypeConstraint);

      if (p1HasType != p2HasType)
      {
        // One typed, one untyped - conflict
        return true;
      }

      if (p1HasType && p2HasType && p1.TypeConstraint != p2.TypeConstraint)
      {
        // Both typed but different types - conflict
        return true;
      }
    }

    // Also check option value type constraints
    List<OptionDefinition> opts1 = [.. route1.Options];
    List<OptionDefinition> opts2 = [.. route2.Options];

    // Match options by name
    foreach (OptionDefinition opt1 in opts1)
    {
      OptionDefinition? opt2 = opts2.FirstOrDefault(o =>
        o.LongForm == opt1.LongForm || o.ShortForm == opt1.ShortForm);

      if (opt2 is null)
        continue;

      // Compare value type constraints if both have values
      if (opt1.ExpectsValue && opt2.ExpectsValue)
      {
        bool opt1HasType = !string.IsNullOrEmpty(opt1.TypeConstraint);
        bool opt2HasType = !string.IsNullOrEmpty(opt2.TypeConstraint);

        if (opt1HasType != opt2HasType)
          return true;

        if (opt1HasType && opt2HasType && opt1.TypeConstraint != opt2.TypeConstraint)
          return true;
      }
    }

    return false;
  }
}
