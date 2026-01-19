// Model-level validation orchestrator.
// Runs all validators on an AppModel and collects diagnostics.

namespace TimeWarp.Nuru.Validation;

/// <summary>
/// Orchestrates validation of an AppModel.
/// Runs all validators and collects diagnostics.
/// </summary>
internal static class ModelValidator
{
  /// <summary>
  /// Validates an AppModel and returns all diagnostics found.
  /// </summary>
  /// <param name="model">The model to validate.</param>
  /// <param name="routeLocations">Map from route pattern to source location for error reporting.</param>
  /// <returns>All diagnostics found during validation.</returns>
  public static ImmutableArray<Diagnostic> Validate(
    AppModel model,
    IReadOnlyDictionary<string, Location> routeLocations)
  {
    ArgumentNullException.ThrowIfNull(model);
    ArgumentNullException.ThrowIfNull(routeLocations);

    List<Diagnostic> diagnostics = [];

    // Run overlap validator
    ImmutableArray<Diagnostic> overlapDiagnostics = OverlapValidator.Validate(model.Routes, routeLocations);
    diagnostics.AddRange(overlapDiagnostics);

    // Future: Add more validators here
    // - Handler validator (moved from NuruHandlerAnalyzer)
    // - Route pattern validator (moved from NuruRouteAnalyzer)
    // - Cross-route validators

    return [.. diagnostics];
  }
}
