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
  /// Rebuilds a <see cref="Location"/> for diagnostic reporting.
  /// When <paramref name="compilation"/> contains the source file, the location is bound to
  /// that syntax tree so <c>#pragma warning</c> can suppress it. A path-only location has no
  /// <see cref="Location.SourceTree"/>, and pragmas do not apply.
  /// </summary>
  public Location ToLocation(Compilation? compilation = null)
  {
    if (compilation is not null)
    {
      foreach (SyntaxTree tree in compilation.SyntaxTrees)
      {
        if (string.Equals(tree.FilePath, FilePath, StringComparison.Ordinal))
          return Location.Create(tree, TextSpan);
      }
    }

    return Location.Create(FilePath, TextSpan, LineSpan);
  }

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
