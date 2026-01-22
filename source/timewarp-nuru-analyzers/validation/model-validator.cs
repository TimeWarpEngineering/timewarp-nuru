// Model-level validation orchestrator.
// Runs all validators on an AppModel and collects diagnostics.

namespace TimeWarp.Nuru.Validation;

using TimeWarp.Nuru.Generators;

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
  /// <param name="extensionMethods">Extension method calls detected during service extraction.</param>
  /// <returns>All diagnostics found during validation.</returns>
  public static ImmutableArray<Diagnostic> Validate(
    AppModel model,
    IReadOnlyDictionary<string, Location> routeLocations,
    ImmutableArray<ExtensionMethodCall> extensionMethods = default)
  {
    ArgumentNullException.ThrowIfNull(model);
    ArgumentNullException.ThrowIfNull(routeLocations);

    List<Diagnostic> diagnostics = [];

    // Run overlap validator
    ImmutableArray<Diagnostic> overlapDiagnostics = OverlapValidator.Validate(model.Routes, routeLocations);
    diagnostics.AddRange(overlapDiagnostics);

    // Run service validator (NURU050, NURU051, NURU053, NURU054)
    ImmutableArray<Diagnostic> serviceDiagnostics = ServiceValidator.Validate(model);
    diagnostics.AddRange(serviceDiagnostics);

    // Run extension method validation (NURU052)
    if (!extensionMethods.IsDefaultOrEmpty)
    {
      ImmutableArray<Diagnostic> extensionDiagnostics = ServiceValidator.ValidateExtensionMethods(
        extensionMethods,
        model.UseMicrosoftDependencyInjection);
      diagnostics.AddRange(extensionDiagnostics);
    }

    // Future: Add more validators here
    // - Handler validator (moved from NuruHandlerAnalyzer)
    // - Route pattern validator (moved from NuruRouteAnalyzer)
    // - Cross-route validators

    return [.. diagnostics];
  }
}
