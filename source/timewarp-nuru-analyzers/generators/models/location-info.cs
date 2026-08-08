namespace TimeWarp.Nuru.Generators;

using Microsoft.CodeAnalysis.Text;

/// <summary>
/// Value-equatable representation of a source location for use inside incremental-generator
/// models. Roslyn's <see cref="Location"/> is tied to a live <c>SyntaxTree</c>, so carrying it
/// in a model record defeats value equality (every edit produces a fresh tree) and forces the
/// emit stage to re-run. This record stores only the path and spans (all value types), then
/// rebuilds a <see cref="Location"/> on demand when a diagnostic is actually created.
/// </summary>
/// <param name="FilePath">Source file path.</param>
/// <param name="TextSpan">Character span within the file.</param>
/// <param name="LineSpan">Line/column span within the file.</param>
public sealed record LocationInfo(
  string FilePath,
  TextSpan TextSpan,
  LinePositionSpan LineSpan)
{
  /// <summary>
  /// Rebuilds a <see cref="Location"/> for diagnostic reporting. Produces an external-file
  /// location whose equality is path/span based rather than tree-reference based.
  /// </summary>
  public Location ToLocation() => Location.Create(FilePath, TextSpan, LineSpan);

  /// <summary>
  /// Captures a <see cref="Location"/> into an equatable <see cref="LocationInfo"/>,
  /// or returns null when the location has no source information.
  /// </summary>
  public static LocationInfo? CreateFrom(Location? location)
  {
    if (location is null || location.SourceTree is null)
      return null;

    FileLinePositionSpan lineSpan = location.GetLineSpan();
    return new LocationInfo(lineSpan.Path, location.SourceSpan, lineSpan.Span);
  }
}
