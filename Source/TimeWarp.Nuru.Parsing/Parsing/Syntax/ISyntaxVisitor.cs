namespace TimeWarp.Nuru.Parsing;

/// <summary>
/// Visitor interface for processing route pattern syntax nodes.
/// </summary>
/// <typeparam name="T">The return type of visitor methods.</typeparam>
internal interface ISyntaxVisitor<T>
{
  /// <summary>
  /// Visits the root pattern node.
  /// </summary>
  /// <param name="pattern">The route syntax tree.</param>
  /// <returns>The result of visiting this node.</returns>
  T VisitPattern(Syntax pattern);
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
