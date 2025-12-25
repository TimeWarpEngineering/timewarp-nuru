namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Represents the source location of a RunAsync() call site.
/// Used to generate the [InterceptsLocation] attribute.
/// </summary>
/// <param name="FilePath">Absolute path to the source file</param>
/// <param name="Line">1-based line number</param>
/// <param name="Column">1-based column number</param>
internal sealed record InterceptSiteModel(
  string FilePath,
  int Line,
  int Column)
{
  /// <summary>
  /// Formats the intercept location for diagnostic messages.
  /// </summary>
  public override string ToString() => $"{FilePath}({Line},{Column})";

  /// <summary>
  /// Creates from a Roslyn Location.
  /// </summary>
  public static InterceptSiteModel FromLocation(Location location)
  {
    FileLinePositionSpan lineSpan = location.GetLineSpan();
    return new InterceptSiteModel(
      FilePath: lineSpan.Path,
      Line: lineSpan.StartLinePosition.Line + 1,
      Column: lineSpan.StartLinePosition.Character + 1);
  }
}
