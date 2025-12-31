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
    string propertyType = GetPropertyType(symbolInfo, memberAccess, semanticModel);

    // Right side is the value expression - get as source text
    string valueExpression = assignment.Right.ToFullString().Trim();

    return new PropertyAssignment(propertyName, propertyType, valueExpression);
  }

  /// <summary>
  /// Gets the property type from semantic analysis.
  /// Falls back to inferring from the right-hand expression type if needed.
  /// </summary>
  private static string GetPropertyType(
    SymbolInfo symbolInfo,
    MemberAccessExpressionSyntax memberAccess,
    SemanticModel semanticModel)
  {
    // Try to get from property symbol directly
    if (symbolInfo.Symbol is IPropertySymbol prop)
      return prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    // Try candidate symbols (may be ambiguous but first is usually correct)
    if (symbolInfo.CandidateSymbols.Length > 0 &&
        symbolInfo.CandidateSymbols[0] is IPropertySymbol candidateProp)
      return candidateProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    // Try to infer from the expression type on the left side
    // This works for lambda parameters where the type is known
    TypeInfo typeInfo = semanticModel.GetTypeInfo(memberAccess);
    if (typeInfo.Type is not null)
      return typeInfo.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    // Try the parameter type - walk up to find the lambda and its interface type
    // The memberAccess.Expression should be the lambda parameter (x)
    // and we can resolve the interface type from the Implements<T> context
    if (memberAccess.Expression is IdentifierNameSyntax)
    {
      // Walk up the syntax tree to find the InvocationExpression
      RoslynSyntaxNode? current = memberAccess.Parent;
      while (current is not null)
      {
        if (current is InvocationExpressionSyntax invocation)
        {
          // Check if this is an Implements<T> call
          if (invocation.Expression is MemberAccessExpressionSyntax methodAccess &&
              methodAccess.Name is GenericNameSyntax genericName &&
              genericName.Identifier.Text == "Implements" &&
              genericName.TypeArgumentList.Arguments.Count > 0)
          {
            // Get the interface type
            TypeSyntax interfaceTypeSyntax = genericName.TypeArgumentList.Arguments[0];
            TypeInfo interfaceTypeInfo = semanticModel.GetTypeInfo(interfaceTypeSyntax);

            if (interfaceTypeInfo.Type is INamedTypeSymbol interfaceType)
            {
              // Find the property on the interface
              string propertyName = memberAccess.Name.Identifier.Text;
              foreach (ISymbol member in interfaceType.GetMembers(propertyName))
              {
                if (member is IPropertySymbol interfaceProp)
                  return interfaceProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
              }
            }
          }

          break;
        }

        current = current.Parent;
      }
    }

    return "object";
  }
}
