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
}
