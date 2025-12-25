// Locator for .Map("pattern") calls.

namespace TimeWarp.Nuru.Generators;

using RoslynSyntaxNode = Microsoft.CodeAnalysis.SyntaxNode;

/// <summary>
/// Locates .Map("pattern") calls that define routes.
/// </summary>
internal static class MapLocator
{
  private const string MethodName = "Map";

  public static bool IsPotentialMatch(RoslynSyntaxNode node)
  {
    if (node is not InvocationExpressionSyntax invocation)
      return false;

    if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
      return false;

    if (memberAccess.Name.Identifier.ValueText != MethodName)
      return false;

    return invocation.ArgumentList.Arguments.Count >= 1;
  }

  public static (InvocationExpressionSyntax Invocation, string Pattern)? Extract
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

    if (methodSymbol.Name != MethodName)
      return null;

    ArgumentSyntax? patternArg = invocation.ArgumentList.Arguments.FirstOrDefault();
    if (patternArg?.Expression is not LiteralExpressionSyntax literal)
      return null;

    if (literal.Token.Value is not string pattern)
      return null;

    return (invocation, pattern);
  }
}
