namespace TimeWarp.Nuru.Parsing;

/// <summary>
/// Root node representing a complete route pattern.
/// </summary>
/// <param name="Segments">The segments that make up this route pattern.</param>
public record Syntax(IReadOnlyList<SegmentSyntax> Segments) : SyntaxNode;