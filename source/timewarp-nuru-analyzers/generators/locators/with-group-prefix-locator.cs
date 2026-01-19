// Locator for .WithGroupPrefix(...) calls.

namespace TimeWarp.Nuru.Generators;

using RoslynSyntaxNode = Microsoft.CodeAnalysis.SyntaxNode;

/// <summary>
/// Locates .WithGroupPrefix(...) calls that define route group prefixes.
/// </summary>
internal static class WithGroupPrefixLocator
{
  private const string MethodName = "WithGroupPrefix";

  public static bool IsPotentialMatch(RoslynSyntaxNode node)
  {
    if (node is not InvocationExpressionSyntax invocation)
      return false;

    if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
      return false;

    return memberAccess.Name.Identifier.ValueText == MethodName
        && invocation.ArgumentList.Arguments.Count >= 1;
  }

  public static (InvocationExpressionSyntax Invocation, string Prefix)? Extract
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

    ArgumentSyntax? arg = invocation.ArgumentList.Arguments.FirstOrDefault();
    if (arg?.Expression is not LiteralExpressionSyntax literal)
      return null;

    if (literal.Token.Value is not string prefix)
      return null;

    return (invocation, prefix);
  }
}
