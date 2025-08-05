namespace TimeWarp.Nuru.Parsing;

using TimeWarp.Nuru.Parsing.Ast;

/// <summary>
/// Visitor interface for processing route pattern AST nodes.
/// </summary>
/// <typeparam name="T">The return type of visitor methods.</typeparam>
public interface IRoutePatternVisitor<T>
{
  /// <summary>
  /// Visits the root pattern node.
  /// </summary>
  /// <param name="pattern">The route pattern AST.</param>
  /// <returns>The result of visiting this node.</returns>
  T VisitPattern(RoutePatternAst pattern);
  /// <summary>
  /// Visits a literal segment node.
  /// </summary>
  /// <param name="literal">The literal node.</param>
  /// <returns>The result of visiting this node.</returns>
  T VisitLiteral(LiteralNode literal);
  /// <summary>
  /// Visits a parameter segment node.
  /// </summary>
  /// <param name="parameter">The parameter node.</param>
  /// <returns>The result of visiting this node.</returns>
  T VisitParameter(ParameterNode parameter);
  /// <summary>
  /// Visits an option segment node.
  /// </summary>
  /// <param name="optionNode">The option node.</param>
  /// <returns>The result of visiting this node.</returns>
  T VisitOption(OptionNode optionNode);
}

/// <summary>
/// Base visitor class that provides default implementations.
/// </summary>
/// <typeparam name="T">The return type of visitor methods.</typeparam>
public abstract class RoutePatternVisitor<T> : IRoutePatternVisitor<T>
{
  /// <inheritdoc />
  public virtual T VisitPattern(RoutePatternAst pattern)
  {
    ArgumentNullException.ThrowIfNull(pattern, nameof(pattern));

    foreach (SegmentNode segment in pattern.Segments)
    {
      Visit(segment);
    }

    return default(T)!;
  }

  /// <inheritdoc />
  public abstract T VisitLiteral(LiteralNode literal);
  /// <inheritdoc />
  public abstract T VisitParameter(ParameterNode parameter);
  /// <inheritdoc />
  public abstract T VisitOption(OptionNode optionNode);

  /// <summary>
  /// Dispatches to the appropriate visit method based on the node type.
  /// </summary>
  /// <param name="node">The node to visit.</param>
  /// <returns>The result of visiting the node.</returns>
  protected T Visit(SegmentNode node) => node switch
  {
    LiteralNode literal => VisitLiteral(literal),
    ParameterNode parameter => VisitParameter(parameter),
    OptionNode option => VisitOption(option),
    _ => throw new ArgumentException($"Unknown segment node type: {node.GetType()}")
  };
}

