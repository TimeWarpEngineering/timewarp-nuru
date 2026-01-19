// Locator for NuruApp.CreateBuilder(...) calls.

namespace TimeWarp.Nuru.Generators;

using RoslynSyntaxNode = Microsoft.CodeAnalysis.SyntaxNode;

/// <summary>
/// Locates NuruApp.CreateBuilder() call sites.
/// </summary>
internal static class CreateBuilderLocator
{
  private const string MethodName = "CreateBuilder";
  private const string TypeName = "NuruApp";

  public static bool IsPotentialMatch(RoslynSyntaxNode node)
  {
    if (node is not InvocationExpressionSyntax invocation)
      return false;

    if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
      return false;

    if (memberAccess.Name.Identifier.ValueText != MethodName)
      return false;

    if (memberAccess.Expression is IdentifierNameSyntax identifier)
      return identifier.Identifier.ValueText == TypeName;

    return false;
  }

  public static InvocationExpressionSyntax? Extract
  (
    GeneratorSyntaxContext context,
    CancellationToken cancellationToken
  )
  {
    if (context.Node is not InvocationExpressionSyntax invocation)
      return null;

    SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, cancellationToken);

    if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
      return null;

    if (methodSymbol.ContainingType?.Name != TypeName)
      return null;

    if (methodSymbol.Name != MethodName)
      return null;

    return invocation;
  }
}
