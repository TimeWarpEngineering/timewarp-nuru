#region Purpose
// Reads IRouteTypeConverter.ConstraintAlias and TargetType from a converter's source at compile time.
#endregion

#region Design
// The generator cannot run a converter, so it reads the property bodies instead: an expression body
// (`=> "point"`), an auto-property initializer (`{ get; } = "point"`), or a getter that is a single
// expression or a single `return`. ConstraintAlias must be a compile-time constant string (a literal or
// a const); TargetType must be `typeof(X)`. Anything else reads as unknown, so the alias stays null and
// a route that uses it reports NURU_S010 instead of emitting code that does not compile (task 584).
// The property is looked up on the converter type and its base types, including an explicit
// IRouteTypeConverter implementation. A converter from a referenced assembly has no syntax and reads
// as unknown.
#endregion

namespace TimeWarp.Nuru.Generators;

public sealed partial class DslInterpreter
{
  private const string ConstraintAliasPropertyName = "ConstraintAlias";
  private const string TargetTypePropertyName = "TargetType";

  /// <summary>
  /// Returns the converter's <c>ConstraintAlias</c> when its property body is a constant string, otherwise null.
  /// </summary>
  private string? ReadConverterConstraintAlias(INamedTypeSymbol converterType)
  {
    if (FindConverterPropertyExpression(converterType, ConstraintAliasPropertyName) is not { } found)
      return null;

    Optional<object?> constant = found.Model.GetConstantValue(found.Expression, CancellationToken);
    return constant.HasValue && constant.Value is string alias && alias.Length > 0 ? alias : null;
  }

  /// <summary>
  /// Returns the fully qualified type named by the converter's <c>TargetType => typeof(X)</c>, otherwise null.
  /// </summary>
  private string? ReadConverterTargetTypeName(INamedTypeSymbol converterType)
  {
    if (FindConverterPropertyExpression(converterType, TargetTypePropertyName) is not { } found)
      return null;

    if (found.Expression is not TypeOfExpressionSyntax typeOf)
      return null;

    ITypeSymbol? targetType = TypeSyntaxResolver.ResolveType(found.Model, typeOf.Type, CancellationToken);
    if (targetType is null || targetType.TypeKind == TypeKind.TypeParameter)
      return null;

    return targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
  }

  /// <summary>
  /// Finds the value expression of a converter property declared in source on the type or a base type.
  /// </summary>
  private (ExpressionSyntax Expression, SemanticModel Model)? FindConverterPropertyExpression
  (
    INamedTypeSymbol converterType,
    string propertyName
  )
  {
    for (INamedTypeSymbol? type = converterType; type is not null; type = type.BaseType)
    {
      foreach (ISymbol member in type.GetMembers())
      {
        if (member is not IPropertySymbol property || !IsConverterProperty(property, propertyName))
          continue;

        foreach (SyntaxReference reference in property.DeclaringSyntaxReferences)
        {
          if (reference.GetSyntax(CancellationToken) is not PropertyDeclarationSyntax declaration)
            continue;

          ExpressionSyntax? expression = GetPropertyValueExpression(declaration);
          if (expression is null)
            continue;

          SemanticModel model = declaration.SyntaxTree == SemanticModel.SyntaxTree
            ? SemanticModel
            : SemanticModel.Compilation.GetSemanticModel(declaration.SyntaxTree);
          return (expression, model);
        }

        // Found the property but could not read it: do not fall through to a base type's value.
        return null;
      }
    }

    return null;
  }

  private static bool IsConverterProperty(IPropertySymbol property, string propertyName)
  {
    if (string.Equals(property.Name, propertyName, StringComparison.Ordinal))
      return true;

    foreach (IPropertySymbol implemented in property.ExplicitInterfaceImplementations)
    {
      if (string.Equals(implemented.Name, propertyName, StringComparison.Ordinal))
        return true;
    }

    return false;
  }

  /// <summary>
  /// Returns the single expression a property yields: expression body, initializer, or a one-statement getter.
  /// </summary>
  private static ExpressionSyntax? GetPropertyValueExpression(PropertyDeclarationSyntax declaration)
  {
    if (declaration.ExpressionBody is { } expressionBody)
      return expressionBody.Expression;

    if (declaration.Initializer is { } initializer)
      return initializer.Value;

    AccessorDeclarationSyntax? getter = declaration.AccessorList?.Accessors
      .FirstOrDefault(static a => a.IsKind(SyntaxKind.GetAccessorDeclaration));
    if (getter is null)
      return null;

    if (getter.ExpressionBody is { } getterExpression)
      return getterExpression.Expression;

    if (getter.Body is { Statements: [ReturnStatementSyntax { Expression: { } returned }] })
      return returned;

    return null;
  }
}
