// Extracts interface implementation information from .Implements<T>(x => ...) expressions.
// See kanban task #316 for design.

namespace TimeWarp.Nuru.Generators;

using RoslynSyntaxNode = Microsoft.CodeAnalysis.SyntaxNode;

/// <summary>
/// Extracts <see cref="InterfaceImplementationDefinition"/> from <c>.Implements&lt;T&gt;(x =&gt; ...)</c> invocations.
/// Parses the expression tree to extract property assignments.
/// </summary>
internal static class ImplementsExtractor
{
  /// <summary>
  /// Extracts interface implementation information from an Implements&lt;T&gt;() invocation.
  /// </summary>
  /// <param name="invocation">The method invocation syntax.</param>
  /// <param name="semanticModel">Semantic model for type resolution.</param>
  /// <returns>The extracted implementation definition, or null if extraction fails.</returns>
  public static InterfaceImplementationDefinition? Extract(
    InvocationExpressionSyntax invocation,
    SemanticModel semanticModel)
  {
    // Get the generic type argument (TFilter) from Implements<TFilter>(...)
    string? filterTypeName = ExtractGenericTypeArgument(invocation, semanticModel);
    if (filterTypeName is null)
      return null;

    // Get the lambda expression argument
    ArgumentListSyntax? args = invocation.ArgumentList;
    if (args is null || args.Arguments.Count == 0)
      return null;

    ExpressionSyntax argExpr = args.Arguments[0].Expression;

    // Extract property assignments from the lambda
    ImmutableArray<PropertyAssignment> properties = ExtractPropertyAssignments(argExpr, semanticModel);

    return new InterfaceImplementationDefinition(filterTypeName, properties);
  }

  /// <summary>
  /// Extracts the generic type argument from Implements&lt;T&gt;().
  /// </summary>
  private static string? ExtractGenericTypeArgument(
    InvocationExpressionSyntax invocation,
    SemanticModel semanticModel)
  {
    // The invocation expression should be a member access like "builder.Implements<T>"
    if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
      return null;

    // The name should be a generic name like "Implements<IRequireAuthorization>"
    if (memberAccess.Name is not GenericNameSyntax genericName)
      return null;

    TypeArgumentListSyntax? typeArgs = genericName.TypeArgumentList;
    if (typeArgs is null || typeArgs.Arguments.Count == 0)
      return null;

    TypeSyntax typeArg = typeArgs.Arguments[0];
    TypeInfo typeInfo = semanticModel.GetTypeInfo(typeArg);

    if (typeInfo.Type is null)
      return null;

    return typeInfo.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
  }

  /// <summary>
  /// Extracts property assignments from a lambda expression.
  /// Supports both simple assignment and block body.
  /// </summary>
  private static ImmutableArray<PropertyAssignment> ExtractPropertyAssignments(
    ExpressionSyntax expression,
    SemanticModel semanticModel)
  {
    ImmutableArray<PropertyAssignment>.Builder properties = ImmutableArray.CreateBuilder<PropertyAssignment>();

    switch (expression)
    {
      // Simple lambda: x => x.Property = value
      case SimpleLambdaExpressionSyntax simpleLambda:
        ExtractFromBody(simpleLambda.Body, semanticModel, properties);
        break;

      // Parenthesized lambda: (x) => x.Property = value
      case ParenthesizedLambdaExpressionSyntax parenLambda:
        ExtractFromBody(parenLambda.Body, semanticModel, properties);
        break;
    }

    return properties.ToImmutable();
  }

  /// <summary>
  /// Extracts property assignments from a lambda body (expression or block).
  /// </summary>
  private static void ExtractFromBody(
    RoslynSyntaxNode body,
    SemanticModel semanticModel,
    ImmutableArray<PropertyAssignment>.Builder properties)
  {
    switch (body)
    {
      // Single assignment expression: x.Property = value
      case AssignmentExpressionSyntax assignment:
        PropertyAssignment? prop = ExtractSingleAssignment(assignment, semanticModel);
        if (prop is not null)
          properties.Add(prop);

        break;

      // Block body: { x.Property1 = value1; x.Property2 = value2; }
      case BlockSyntax block:
        foreach (StatementSyntax statement in block.Statements)
        {
          if (statement is ExpressionStatementSyntax exprStmt &&
              exprStmt.Expression is AssignmentExpressionSyntax blockAssignment)
          {
            PropertyAssignment? blockProp = ExtractSingleAssignment(blockAssignment, semanticModel);
            if (blockProp is not null)
              properties.Add(blockProp);
          }
        }

        break;
    }
  }

  /// <summary>
  /// Extracts a single property assignment from an assignment expression.
  /// </summary>
  private static PropertyAssignment? ExtractSingleAssignment(
    AssignmentExpressionSyntax assignment,
    SemanticModel semanticModel)
  {
    // Left side should be x.PropertyName
    if (assignment.Left is not MemberAccessExpressionSyntax memberAccess)
      return null;

    string propertyName = memberAccess.Name.Identifier.Text;

    // Get the property type from semantic model
    SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(memberAccess);
    string propertyType = symbolInfo.Symbol switch
    {
      IPropertySymbol prop => prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
      _ => "object"
    };

    // Right side is the value expression - get as source text
    string valueExpression = assignment.Right.ToFullString().Trim();

    return new PropertyAssignment(propertyName, propertyType, valueExpression);
  }
}
