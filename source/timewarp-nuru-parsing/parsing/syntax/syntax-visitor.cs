namespace TimeWarp.Nuru.Parsing;

/// <summary>
/// Base visitor class that provides default implementations.
/// </summary>
/// <typeparam name="T">The return type of visitor methods.</typeparam>
internal abstract class SyntaxVisitor<T> : ISyntaxVisitor<T>
{
  /// <inheritdoc />
  public virtual T VisitPattern(Syntax pattern)
  {
    ArgumentNullException.ThrowIfNull(pattern, nameof(pattern));

    foreach (SegmentSyntax segment in pattern.Segments)
    {
      Visit(segment);
    }

    return default(T)!;
  }

  /// <inheritdoc />
  public abstract T VisitLiteral(LiteralSyntax literal);
  /// <inheritdoc />
  public abstract T VisitParameter(ParameterSyntax parameter);
  /// <inheritdoc />
  public abstract T VisitOption(OptionSyntax optionNode);

  /// <summary>
  /// Dispatches to the appropriate visit method based on the node type.
  /// </summary>
  /// <param name="node">The node to visit.</param>
  /// <returns>The result of visiting the node.</returns>
  protected T Visit(SegmentSyntax node) => node switch
  {
    LiteralSyntax literal => VisitLiteral(literal),
    ParameterSyntax parameter => VisitParameter(parameter),
    OptionSyntax option => VisitOption(option),
    _ => throw new ArgumentException($"Unknown segment node type: {node.GetType()}")
  };
}
