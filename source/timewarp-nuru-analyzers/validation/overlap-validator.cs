// Overlap validator that detects:
// 1. Routes with same structure but different type constraints (NURU_R001)
// 2. Duplicate route patterns (NURU_R002)
// 3. Unreachable routes - where one route shadows another (NURU_R003)
//
// Structure signature: Normalized form ignoring parameter names and types
// Required signature: Only literals and required parameters (no options, no optional params)

namespace TimeWarp.Nuru.Validation;

using TimeWarp.Nuru.Generators;

/// <summary>
/// Validates routes for overlapping structures, duplicates, and unreachable routes.
/// </summary>
internal static class OverlapValidator
{
  /// <summary>
  /// Validates routes for various overlap and conflict issues.
  /// </summary>
  /// <param name="routes">The routes to validate.</param>
  /// <param name="routeLocations">Map from route pattern to source location for error reporting.</param>
  /// <returns>Diagnostics for any issues found.</returns>
  public static ImmutableArray<Diagnostic> Validate(
    ImmutableArray<RouteDefinition> routes,
    IReadOnlyDictionary<string, Location> routeLocations)
  {
    if (routes.Length < 2)
      return [];

    List<Diagnostic> diagnostics = [];

    // Check 1: Group by structure signature for type conflicts and duplicates
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

    foreach (KeyValuePair<string, List<RouteDefinition>> entry in routesBySignature)
    {
      List<RouteDefinition> group = entry.Value;
      if (group.Count < 2)
        continue;

      ImmutableArray<Diagnostic> groupDiagnostics = CheckGroupForTypeConflicts(group, routeLocations);
      diagnostics.AddRange(groupDiagnostics);
    }

    // Check 2: Group by required signature for unreachable routes
    ImmutableArray<Diagnostic> unreachableDiagnostics = CheckForUnreachableRoutes(routes, routeLocations);
    diagnostics.AddRange(unreachableDiagnostics);

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

        case EndOfOptionsSeparatorDefinition:
          // End-of-options separator is a distinguishing element
          signature.Append("--");
          break;
      }
    }

    return signature.ToString();
  }

  /// <summary>
  /// Checks a group of routes with the same signature for type constraint conflicts and duplicates.
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

        // Check for exact duplicate patterns first (use EffectivePattern for accurate comparison)
        if (route1.EffectivePattern == route2.EffectivePattern)
        {
          // Get location for the first route (or use a default)
          Location location = routeLocations.TryGetValue(route1.EffectivePattern, out Location? loc)
            ? loc
            : Location.None;

          Diagnostic diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.DuplicateRoutePattern,
            location,
            route1.EffectivePattern);

          diagnostics.Add(diagnostic);
          continue; // Don't also report type constraint conflict for same pattern
        }

        // Check if they have different type constraints
        if (HaveDifferentTypeConstraints(route1, route2))
        {
          // Get location for the first route (or use a default)
          Location location = routeLocations.TryGetValue(route1.EffectivePattern, out Location? loc)
            ? loc
            : Location.None;

          Diagnostic diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.OverlappingTypeConstraints,
            location,
            route1.EffectivePattern,
            route2.EffectivePattern);

          diagnostics.Add(diagnostic);
        }
      }
    }

    return [.. diagnostics];
  }

  /// <summary>
  /// Determines if two routes have different type constraints at corresponding positions.
  /// Note: Caller should check for duplicate patterns first before calling this method.
  /// </summary>
  private static bool HaveDifferentTypeConstraints(RouteDefinition route1, RouteDefinition route2)
  {
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

  /// <summary>
  /// Computes a required signature for a route - literals, required parameters, and required options.
  /// This excludes optional elements (optional options, optional parameters, catch-all).
  /// Routes with the same required signature match the same "core" inputs.
  /// </summary>
  /// <remarks>
  /// Examples:
  /// - "deploy {env} --force?"    -> "deploy {P}"           (optional flag)
  /// - "deploy {env}"             -> "deploy {P}"
  /// - "deploy production"        -> "deploy production"
  /// - "git commit --amend"       -> "git commit --amend"   (required flag)
  /// - "get {id} {name?}"         -> "get {P}"
  /// - "test --verbose --watch?"  -> "test --verbose"       (one required, one optional)
  /// - "round {v} --mode {m}"     -> "round {P} --mode {P}" (required option with value)
  /// </remarks>
  private static string ComputeRequiredSignature(RouteDefinition route)
  {
    StringBuilder signature = new();

    // Include group prefix if present
    if (!string.IsNullOrEmpty(route.GroupPrefix))
    {
      signature.Append(route.GroupPrefix);
    }

    foreach (SegmentDefinition segment in route.Segments)
    {
      switch (segment)
      {
        case LiteralDefinition literal:
          if (signature.Length > 0)
            signature.Append(' ');
          signature.Append(literal.Value);
          break;

        case ParameterDefinition param when !param.IsOptional && !param.IsCatchAll:
          // Only include required parameters (not optional, not catch-all)
          if (signature.Length > 0)
            signature.Append(' ');
          signature.Append("{P}");
          break;

        case OptionDefinition option when !option.IsOptional && option.ExpectsValue:
          // Include required options with values (those without ? that expect a value)
          // Boolean flags are excluded because they're always optional at runtime
          // (per design: "Boolean flags are scored but always optional")
          if (signature.Length > 0)
            signature.Append(' ');
          signature.Append(option.LongFormWithPrefix ?? option.ShortFormWithPrefix ?? "--opt");
          signature.Append(option.ParameterIsOptional ? " {P?}" : " {P}");
          break;

        case EndOfOptionsSeparatorDefinition:
          // End-of-options separator is always required when present
          if (signature.Length > 0)
            signature.Append(' ');
          signature.Append("--");
          break;

        // Skip: optional options (--flag?),
        //       boolean flags (--verbose) - always optional at runtime,
        //       optional parameters ({param?}),
        //       catch-all ({*args})
      }
    }

    return signature.ToString();
  }

  /// <summary>
  /// Checks for unreachable routes - routes that can never be matched because
  /// another route with equal or higher specificity matches all the same inputs.
  /// </summary>
  private static ImmutableArray<Diagnostic> CheckForUnreachableRoutes(
    ImmutableArray<RouteDefinition> routes,
    IReadOnlyDictionary<string, Location> routeLocations)
  {
    List<Diagnostic> diagnostics = [];

    // Group routes by their required signature
    Dictionary<string, List<RouteDefinition>> routesByRequiredSignature = [];

    foreach (RouteDefinition route in routes)
    {
      string requiredSig = ComputeRequiredSignature(route);

      if (!routesByRequiredSignature.TryGetValue(requiredSig, out List<RouteDefinition>? group))
      {
        group = [];
        routesByRequiredSignature[requiredSig] = group;
      }

      group.Add(route);
    }

    // Check each group for shadowing
    foreach (KeyValuePair<string, List<RouteDefinition>> entry in routesByRequiredSignature)
    {
      List<RouteDefinition> group = entry.Value;
      if (group.Count < 2)
        continue;

      // Sort by specificity descending (highest first)
      List<RouteDefinition> sortedGroup = [.. group.OrderByDescending(r => r.ComputedSpecificity)];

      // Track which routes have been reported as unreachable to avoid duplicates
      HashSet<string> reportedUnreachable = [];

      // Compare each pair - higher specificity route shadows lower ones
      for (int i = 0; i < sortedGroup.Count; i++)
      {
        RouteDefinition higherRoute = sortedGroup[i];

        for (int j = i + 1; j < sortedGroup.Count; j++)
        {
          RouteDefinition lowerRoute = sortedGroup[j];

          // Skip if same pattern (handled by duplicate detection)
          if (higherRoute.EffectivePattern == lowerRoute.EffectivePattern)
            continue;

          // Skip if already reported this route as unreachable
          if (reportedUnreachable.Contains(lowerRoute.EffectivePattern))
            continue;

          // Higher specificity route shadows lower specificity route with same required signature
          // Also check equal specificity - first one wins, second is unreachable
          if (higherRoute.ComputedSpecificity >= lowerRoute.ComputedSpecificity)
          {
            Location location = routeLocations.TryGetValue(lowerRoute.EffectivePattern, out Location? loc)
              ? loc
              : Location.None;

            // Get the location of the shadowing route for additional context
            Location? higherLocation = routeLocations.TryGetValue(higherRoute.EffectivePattern, out Location? higherLoc)
              ? higherLoc
              : null;

            IEnumerable<Location> additionalLocations = higherLocation is not null
              ? [higherLocation]
              : [];

            // Format the shadowing route's location for the message
            string higherLocationString = FormatLocation(higherLocation);

            Diagnostic diagnostic = Diagnostic.Create(
              DiagnosticDescriptors.UnreachableRoute,
              location,
              additionalLocations,
              lowerRoute.EffectivePattern,
              higherRoute.EffectivePattern,
              higherRoute.ComputedSpecificity,
              lowerRoute.ComputedSpecificity,
              higherLocationString);

            diagnostics.Add(diagnostic);
            reportedUnreachable.Add(lowerRoute.EffectivePattern);
          }
        }
      }
    }

    return [.. diagnostics];
  }

  /// <summary>
  /// Formats a location for display in diagnostic messages.
  /// </summary>
  private static string FormatLocation(Location? location)
  {
    if (location is null || location == Location.None)
      return "unknown location";

    FileLinePositionSpan lineSpan = location.GetLineSpan();
    if (!lineSpan.IsValid)
      return "unknown location";

    string filePath = lineSpan.Path;
    int line = lineSpan.StartLinePosition.Line + 1; // Convert to 1-based

    // Extract just the filename for brevity
    string fileName = Path.GetFileName(filePath);

    return $"{fileName}:{line}";
  }
}
