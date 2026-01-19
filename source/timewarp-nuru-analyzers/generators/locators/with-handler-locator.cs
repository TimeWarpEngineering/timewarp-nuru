// Locator for .WithHandler(...) calls.

namespace TimeWarp.Nuru.Generators;

using RoslynSyntaxNode = Microsoft.CodeAnalysis.SyntaxNode;

/// <summary>
/// Locates .WithHandler(...) calls that define route handlers.
/// </summary>
internal static class WithHandlerLocator
{
  private const string MethodName = "WithHandler";

  public static bool IsPotentialMatch(RoslynSyntaxNode node)
  {
    if (node is not InvocationExpressionSyntax invocation)
      return false;

    if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
      return false;

    if (memberAccess.Name.Identifier.ValueText != MethodName)
      return false;

    return invocation.ArgumentList.Arguments.Count == 1;
  }

  public static (InvocationExpressionSyntax Invocation, ExpressionSyntax Handler)? Extract
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

    ArgumentSyntax? handlerArg = invocation.ArgumentList.Arguments.FirstOrDefault();
    if (handlerArg is null)
      return null;

    return (invocation, handlerArg.Expression);
  }
}
