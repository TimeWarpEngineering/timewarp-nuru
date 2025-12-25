namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Represents the source location of a RunAsync() call site.
/// Used to generate the [InterceptsLocation] attribute.
/// </summary>
/// <remarks>
/// In .NET 10 / C# 14, interceptors use the new <see cref="InterceptableLocation"/> API
/// which provides version-aware opaque data encoding instead of file/line/column.
/// This model wraps <see cref="InterceptableLocation"/> and provides backward-compatible
/// file/line/column for diagnostics and display.
/// </remarks>
/// <param name="InterceptableLocation">The Roslyn InterceptableLocation for generating the attribute</param>
/// <param name="FilePath">Absolute path to the source file (for diagnostics)</param>
/// <param name="Line">1-based line number (for diagnostics)</param>
/// <param name="Column">1-based column number (for diagnostics)</param>
internal sealed record InterceptSiteModel(
  InterceptableLocation InterceptableLocation,
  string FilePath,
  int Line,
  int Column)
{
  /// <summary>
  /// Formats the intercept location for diagnostic messages.
  /// </summary>
  public override string ToString() => $"{FilePath}({Line},{Column})";

  /// <summary>
  /// Gets the attribute syntax for the interceptor.
  /// Uses the new versioned encoding: [InterceptsLocation(version, data)]
  /// </summary>
  public string GetAttributeSyntax() => InterceptableLocation.GetInterceptsLocationAttributeSyntax();

  /// <summary>
  /// Gets a human-readable display location.
  /// </summary>
  public string GetDisplayLocation() => InterceptableLocation.GetDisplayLocation();

  /// <summary>
  /// Creates from a Roslyn InterceptableLocation.
  /// </summary>
  public static InterceptSiteModel FromInterceptableLocation(InterceptableLocation interceptableLocation, Location location)
  {
    FileLinePositionSpan lineSpan = location.GetLineSpan();
    return new InterceptSiteModel(
      InterceptableLocation: interceptableLocation,
      FilePath: lineSpan.Path,
      Line: lineSpan.StartLinePosition.Line + 1,
      Column: lineSpan.StartLinePosition.Character + 1);
  }
}
