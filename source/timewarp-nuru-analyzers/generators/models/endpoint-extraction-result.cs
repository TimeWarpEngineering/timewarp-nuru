namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Result of extracting a RouteDefinition from an endpoint class ([NuruRoute]).
/// Contains both the extracted route (if successful) and any diagnostics encountered.
/// </summary>
/// <param name="Route">The extracted RouteDefinition, or null if extraction failed completely.</param>
/// <param name="Diagnostics">All diagnostics encountered during extraction (errors, warnings).</param>
/// <remarks>
/// Possible states:
/// - Route is not null, Diagnostics is empty: Extraction fully successful
/// - Route is not null, Diagnostics has items: Partial success (route built, but with warnings or non-fatal errors)
/// - Route is null, Diagnostics has items: Extraction failed (errors explain why)
/// - Route is null, Diagnostics is empty: No route to extract (e.g., missing Handler class)
/// </remarks>
public sealed record EndpointExtractionResult(
  RouteDefinition? Route,
  ImmutableArray<Diagnostic> Diagnostics)
{
  /// <summary>
  /// Creates a successful extraction result with no diagnostics.
  /// </summary>
  public static EndpointExtractionResult Success(RouteDefinition route) =>
    new(route, []);

  /// <summary>
  /// Creates a successful extraction result with diagnostics (warnings or non-fatal errors).
  /// </summary>
  public static EndpointExtractionResult SuccessWithDiagnostics(RouteDefinition route, ImmutableArray<Diagnostic> diagnostics) =>
    new(route, diagnostics);

  /// <summary>
  /// Creates a failed extraction result with a single diagnostic.
  /// </summary>
  public static EndpointExtractionResult Failure(Diagnostic diagnostic) =>
    new(null, [diagnostic]);

  /// <summary>
  /// Creates a failed extraction result with multiple diagnostics.
  /// </summary>
  public static EndpointExtractionResult Failure(ImmutableArray<Diagnostic> diagnostics) =>
    new(null, diagnostics);

  /// <summary>
  /// Creates an empty result (no route, no diagnostics).
  /// Used when there's nothing to extract (e.g., missing Handler class).
  /// </summary>
  public static EndpointExtractionResult Empty => new(null, []);

  /// <summary>
  /// Gets whether extraction was successful (route was built).
  /// </summary>
  public bool IsSuccess => Route is not null;

  /// <summary>
  /// Gets whether there are any diagnostics (errors or warnings).
  /// </summary>
  public bool HasDiagnostics => Diagnostics.Length > 0;

  /// <summary>
  /// Gets whether there are any error-level diagnostics.
  /// </summary>
  public bool HasErrors => Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);
}
