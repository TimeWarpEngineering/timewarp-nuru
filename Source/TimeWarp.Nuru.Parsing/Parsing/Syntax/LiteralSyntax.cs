namespace TimeWarp.Nuru.Parsing;

/// <summary>
/// A literal segment that must match exactly.
/// Examples: "git", "status", "commit"
/// </summary>
/// <param name="Value">The literal text that must match.</param>
internal record LiteralSyntax(string Value) : SegmentSyntax
{
  public override string ToString() => $"Literal: '{Value}'";
}