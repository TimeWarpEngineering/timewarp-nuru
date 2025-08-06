namespace TimeWarp.Nuru.Parsing;

using TimeWarp.Nuru.Parsing.Ast;

/// <summary>
/// Visitor interface for processing route pattern syntax nodes.
/// </summary>
/// <typeparam name="T">The return type of visitor methods.</typeparam>
public interface ISyntaxVisitor<T>
{
  /// <summary>
  /// Visits the root pattern node.
  /// </summary>
  /// <param name="pattern">The route syntax tree.</param>
  /// <returns>The result of visiting this node.</returns>
  T VisitPattern(RouteSyntax pattern);
  /// <summary>
  /// Visits a literal segment node.
  /// </summary>
  /// <param name="literal">The literal node.</param>
  /// <returns>The result of visiting this node.</returns>
  T VisitLiteral(LiteralSyntax literal);
  /// <summary>
  /// Visits a parameter segment node.
  /// </summary>
  /// <param name="parameter">The parameter node.</param>
  /// <returns>The result of visiting this node.</returns>
  T VisitParameter(ParameterSyntax parameter);
  /// <summary>
  /// Visits an option segment node.
  /// </summary>
  /// <param name="optionNode">The option node.</param>
  /// <returns>The result of visiting this node.</returns>
  T VisitOption(OptionSyntax optionNode);
}

/// <summary>
/// Base visitor class that provides default implementations.
/// </summary>
/// <typeparam name="T">The return type of visitor methods.</typeparam>
public abstract class SyntaxVisitor<T> : ISyntaxVisitor<T>
{
  /// <inheritdoc />
  public virtual T VisitPattern(RouteSyntax pattern)
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

