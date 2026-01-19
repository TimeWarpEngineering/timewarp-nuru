namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Result of extracting an AppModel from source code.
/// Contains both the extracted model (if successful) and any diagnostics encountered.
/// </summary>
/// <param name="Model">The extracted AppModel, or null if extraction failed completely.</param>
/// <param name="Diagnostics">All diagnostics encountered during extraction (errors, warnings).</param>
/// <remarks>
/// This type enables error collection rather than throwing exceptions during extraction.
/// Both analyzer and generator use this to get consistent error handling.
/// 
/// Possible states:
/// - Model is not null, Diagnostics is empty: Extraction fully successful
/// - Model is not null, Diagnostics has items: Partial success (model built, but with warnings or non-fatal errors)
/// - Model is null, Diagnostics has items: Extraction failed (errors explain why)
/// - Model is null, Diagnostics is empty: Should not happen (indicates a bug)
/// </remarks>
public sealed record ExtractionResult(
  AppModel? Model,
  ImmutableArray<Diagnostic> Diagnostics)
{
  /// <summary>
  /// Creates a successful extraction result with no diagnostics.
  /// </summary>
  public static ExtractionResult Success(AppModel model) =>
    new(model, []);

  /// <summary>
  /// Creates a successful extraction result with diagnostics (warnings or non-fatal errors).
  /// </summary>
  public static ExtractionResult SuccessWithDiagnostics(AppModel model, ImmutableArray<Diagnostic> diagnostics) =>
    new(model, diagnostics);

  /// <summary>
  /// Creates a failed extraction result with a single diagnostic.
  /// </summary>
  public static ExtractionResult Failure(Diagnostic diagnostic) =>
    new(null, [diagnostic]);

  /// <summary>
  /// Creates a failed extraction result with multiple diagnostics.
  /// </summary>
  public static ExtractionResult Failure(ImmutableArray<Diagnostic> diagnostics) =>
    new(null, diagnostics);

  /// <summary>
  /// Creates an empty result (no model, no diagnostics).
  /// Used when there's nothing to extract (e.g., no DSL code found).
  /// </summary>
  public static ExtractionResult Empty => new(null, []);

  /// <summary>
  /// Gets whether extraction was successful (model was built).
  /// </summary>
  public bool IsSuccess => Model is not null;

  /// <summary>
  /// Gets whether there are any diagnostics (errors or warnings).
  /// </summary>
  public bool HasDiagnostics => Diagnostics.Length > 0;

  /// <summary>
  /// Gets whether there are any error-level diagnostics.
  /// </summary>
  public bool HasErrors => Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

  /// <summary>
  /// Combines this result with another, merging diagnostics.
  /// If either model is null, the result model is null.
  /// </summary>
  public ExtractionResult Merge(ExtractionResult other)
  {
    ArgumentNullException.ThrowIfNull(other);

    AppModel? mergedModel = Model is not null && other.Model is not null
      ? Model // Keep this model (or could implement actual merging if needed)
      : null;

    ImmutableArray<Diagnostic> mergedDiagnostics = Diagnostics.AddRange(other.Diagnostics);

    return new ExtractionResult(mergedModel, mergedDiagnostics);
  }
}
