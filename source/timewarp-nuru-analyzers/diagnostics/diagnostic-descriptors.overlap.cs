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
                   "This can happen when a fluent route (.Map()) has the same pattern as an attributed route ([NuruRoute]), " +
                   "or when the same pattern is registered multiple times via the fluent API.");

  /// <summary>
  /// NURU_R003: Route is unreachable because another route with equal or higher specificity matches all the same inputs.
  /// </summary>
  public static readonly DiagnosticDescriptor UnreachableRoute = new(
      id: "NURU_R003",
      title: "Unreachable route",
      messageFormat: "Route '{0}' is unreachable. Route '{1}' will match all the same inputs with equal or higher specificity ({2} vs {3} points).",
      category: OverlapCategory,
      defaultSeverity: DiagnosticSeverity.Error,
      isEnabledByDefault: true,
      description: "A route is unreachable when another route with equal or higher specificity matches all the same inputs. " +
                   "This happens when two routes have the same required structure (literals and required parameters) but one has " +
                   "additional optional elements (options, optional parameters). The route with higher specificity will always " +
                   "be matched first, making the lower specificity route dead code. Consider removing the unreachable route or " +
                   "differentiating the patterns.");
}
