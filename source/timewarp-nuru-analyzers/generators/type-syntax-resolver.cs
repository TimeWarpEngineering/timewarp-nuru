#region Purpose
// Resolves a TypeSyntax to an ITypeSymbol for DSL type arguments.
#endregion

#region Design
// GetSymbolInfo is tried first: referenced-project types can bind there while
// GetTypeInfo().Type is null. GetTypeInfo is the second attempt. TypeKind.Error
// symbols from error recovery are rejected so callers do not emit an unusable
// display string. Same order as AddTypeConverter (kanban 454-012).
#endregion

namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Resolves <see cref="TypeSyntax"/> nodes to symbols for generator extraction.
/// </summary>
internal static class TypeSyntaxResolver
{
  /// <summary>
  /// Returns the bound type, or null when both symbol and type info miss or the symbol is an error type.
  /// </summary>
  internal static ITypeSymbol? ResolveType
  (
    SemanticModel semanticModel,
    TypeSyntax typeSyntax,
    CancellationToken cancellationToken = default
  )
  {
    ITypeSymbol? resolved = semanticModel.GetSymbolInfo(typeSyntax, cancellationToken).Symbol as ITypeSymbol;
    resolved ??= semanticModel.GetTypeInfo(typeSyntax, cancellationToken).Type;

    if (resolved is null || resolved.TypeKind == TypeKind.Error)
      return null;

    return resolved;
  }
}
