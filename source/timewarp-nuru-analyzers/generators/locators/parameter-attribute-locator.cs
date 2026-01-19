// Locator for [Parameter] attributed properties.

namespace TimeWarp.Nuru.Generators;

using RoslynSyntaxNode = Microsoft.CodeAnalysis.SyntaxNode;

/// <summary>
/// Locates properties with the [Parameter] attribute.
/// </summary>
internal static class ParameterAttributeLocator
{
  private const string AttributeName = "Parameter";
  private const string AttributeFullName = "ParameterAttribute";

  public static bool IsPotentialMatch(RoslynSyntaxNode node)
  {
    if (node is not PropertyDeclarationSyntax propDecl)
      return false;

    if (propDecl.AttributeLists.Count == 0)
      return false;

    foreach (AttributeListSyntax attrList in propDecl.AttributeLists)
    {
      foreach (AttributeSyntax attr in attrList.Attributes)
      {
        string? name = attr.Name switch
        {
          IdentifierNameSyntax id => id.Identifier.ValueText,
          QualifiedNameSyntax qn => qn.Right.Identifier.ValueText,
          _ => null
        };

        if (name == AttributeName || name == AttributeFullName)
          return true;
      }
    }

    return false;
  }

  public static PropertyDeclarationSyntax? Extract
  (
    GeneratorSyntaxContext context,
    CancellationToken cancellationToken
  )
  {
    if (context.Node is not PropertyDeclarationSyntax propDecl)
      return null;

    foreach (AttributeListSyntax attrList in propDecl.AttributeLists)
    {
      foreach (AttributeSyntax attr in attrList.Attributes)
      {
        SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(attr, cancellationToken);

        if (symbolInfo.Symbol is IMethodSymbol ctorSymbol)
        {
          if (ctorSymbol.ContainingType.Name == AttributeFullName)
            return propDecl;
        }
      }
    }

    return null;
  }
}
