namespace TimeWarp.Nuru.Parsing;

/// <summary>
/// Base class for route segments (literals, parameters, options).
/// </summary>
public abstract record SegmentSyntax : SyntaxNode
{
  /// <summary>
  /// Position in the original input string where this segment starts.
  /// </summary>
  public int Position { get; init; }

  /// <summary>
  /// Length of this segment in the original input string.
  /// </summary>
  public int Length { get; init; }
}