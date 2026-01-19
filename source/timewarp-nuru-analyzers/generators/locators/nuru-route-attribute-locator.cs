// Locator for [NuruRoute] attributed classes.

namespace TimeWarp.Nuru.Generators;

using RoslynSyntaxNode = Microsoft.CodeAnalysis.SyntaxNode;

/// <summary>
/// Locates classes with the [NuruRoute] attribute.
/// </summary>
internal static class NuruRouteAttributeLocator
{
  private const string AttributeName = "NuruRoute";
  private const string AttributeFullName = "NuruRouteAttribute";

  public static bool IsPotentialMatch(RoslynSyntaxNode node)
  {
    if (node is not ClassDeclarationSyntax classDecl)
      return false;

    // Check if class has any attributes
    if (classDecl.AttributeLists.Count == 0)
      return false;

    // Quick syntactic check for attribute name
    foreach (AttributeListSyntax attrList in classDecl.AttributeLists)
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

  public static ClassDeclarationSyntax? Extract
  (
    GeneratorSyntaxContext context,
    CancellationToken cancellationToken
  )
  {
    if (context.Node is not ClassDeclarationSyntax classDecl)
      return null;

    // Verify semantically that the attribute is NuruRouteAttribute
    foreach (AttributeListSyntax attrList in classDecl.AttributeLists)
    {
      foreach (AttributeSyntax attr in attrList.Attributes)
      {
        SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(attr, cancellationToken);

        if (symbolInfo.Symbol is IMethodSymbol ctorSymbol)
        {
          if (ctorSymbol.ContainingType.Name == AttributeFullName)
            return classDecl;
        }
      }
    }

    return null;
  }
}
