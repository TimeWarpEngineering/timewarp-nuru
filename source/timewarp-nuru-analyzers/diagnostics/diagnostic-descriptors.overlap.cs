namespace TimeWarp.Nuru;

/// <summary>
/// Diagnostic descriptors for route overlap errors (cross-route analysis).
/// </summary>
internal static partial class DiagnosticDescriptors
{
  internal const string OverlapCategory = "RoutePattern.Overlap";

  /// <summary>
  /// NURU_R001: Routes with same structure but different type constraints.
  /// This includes typed vs untyped (e.g., {id:int} vs {id}) and
  /// different types (e.g., {id:int} vs {id:guid}).
  /// </summary>
  public static readonly DiagnosticDescriptor OverlappingTypeConstraints = new(
      id: "NURU_R001",
      title: "Overlapping routes with different type constraints",
      messageFormat: "Routes '{0}' and '{1}' have the same structure with different type constraints. Type conversion failures produce errors, not fallback to other routes. Use explicit subcommands or flags instead.",
      category: OverlapCategory,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "Routes with the same literal and parameter structure but different type constraints create ambiguous behavior. " +
                   "When type conversion fails, the framework emits a clear error message rather than falling back to another route. " +
                   "Use explicit subcommands (e.g., 'get-by-id {id:int}' vs 'get-by-name {name}') or flags (e.g., 'get --id {id:int}' vs 'get --name {name}') instead.");

  /// <summary>
  /// NURU_R002: Duplicate route pattern defined multiple times.
  /// </summary>
  public static readonly DiagnosticDescriptor DuplicateRoutePattern = new(
      id: "NURU_R002",
      title: "Duplicate route pattern",
      messageFormat: "Route pattern '{0}' is defined multiple times. Each route pattern must be unique within an application.",
      category: OverlapCategory,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "The same route pattern cannot be defined more than once within an application. " +
                   "This can happen when a fluent route (.Map()) has the same pattern as an endpoint ([NuruRoute]), " +
                   "or when the same pattern is registered multiple times via the fluent API.");

  /// <summary>
  /// NURU_R003: Route is unreachable because another route with equal or higher specificity matches all the same inputs.
  /// </summary>
  public static readonly DiagnosticDescriptor UnreachableRoute = new(
      id: "NURU_R003",
      title: "Unreachable route",
      messageFormat: "Route '{0}' is unreachable. Route '{1}' (defined at {4}) will match all the same inputs with equal or higher specificity ({2} vs {3} points).",
      category: OverlapCategory,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "A route is unreachable when another route with equal or higher specificity matches all the same inputs. " +
                   "This happens when two routes have the same required structure (literals and required parameters) but one has " +
                   "additional optional elements (options, optional parameters). The route with higher specificity will always " +
                   "be matched first, making the lower specificity route dead code. Consider removing the unreachable route or " +
                   "differentiating the patterns.");

  /// <summary>
  /// NURU_R004: REPL AutoStartWhenEmpty makes a top-level default route unreachable.
  /// The generated interceptor starts the REPL when <c>routeArgs.Length == 0</c> before user routes
  /// are matched, so a top-level <c>""</c> route can never run.
  /// </summary>
  public static readonly DiagnosticDescriptor ReplAutoStartConflictsWithDefaultRoute = new(
      id: "NURU_R004",
      title: "REPL AutoStartWhenEmpty conflicts with default route",
      messageFormat: "REPL AutoStartWhenEmpty makes the default route unreachable; remove the default route or disable AutoStartWhenEmpty",
      category: OverlapCategory,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "When AutoStartWhenEmpty is true, an empty argument list starts the REPL before any user route is matched. " +
                   "A top-level default route (pattern \"\") that matches that same empty argument list can never run. " +
                   "A \"\" route inside a group does not conflict, because the group prefix is required. " +
                   "A \"\" route with a required positional parameter or required option does not match an empty argument list, so it does not conflict. " +
                   "Remove the default route, or set AutoStartWhenEmpty to false and start the REPL with --interactive or -i.");

  /// <summary>
  /// NURU_R005: A user option's long form is the reserved built-in <c>json-args</c>.
  /// User routes may still override <c>--help</c>. This diagnostic is new; there is
  /// no <c>--help</c> or <c>--capabilities</c> diagnostic to copy.
  /// </summary>
  public static readonly DiagnosticDescriptor ReservedJsonArgsOption = new(
      id: "NURU_R005",
      title: "Option long form json-args is reserved",
      messageFormat: "Option long form 'json-args' on route '{0}' is reserved. --json-args is a built-in and cannot be a user option.",
      category: OverlapCategory,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "--json-args is peeled from the original argument list before user routes run. " +
                   "A user option whose long form is json-args would claim that flag. " +
                   "Rename the option. There is no short form to use instead.");
}
