// Locator for .Build() calls on the app builder.

namespace TimeWarp.Nuru.Generators;

using RoslynSyntaxNode = Microsoft.CodeAnalysis.SyntaxNode;

/// <summary>
/// Locates .Build() calls that terminate the builder chain.
/// </summary>
internal static class BuildLocator
{
  private const string MethodName = "Build";

  public static bool IsPotentialMatch(RoslynSyntaxNode node)
  {
    if (node is not InvocationExpressionSyntax invocation)
      return false;

    if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
      return false;

    return memberAccess.Name.Identifier.ValueText == MethodName
        && invocation.ArgumentList.Arguments.Count == 0;
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

    if (methodSymbol.Name != MethodName)
      return null;

    if (methodSymbol.ReturnType.Name != "NuruApp")
      return null;

    return invocation;
  }

  /// <summary>
  /// Confirms that an invocation is a valid Build() call on a NuruAppBuilder.
  /// </summary>
  public static bool IsConfirmedBuildCall
  (
    InvocationExpressionSyntax invocation,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    if (!IsPotentialMatch(invocation))
      return false;

    SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(invocation, cancellationToken);

    if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
      return false;

    if (methodSymbol.Name != MethodName)
      return false;

    // Check that the return type is NuruApp
    if (methodSymbol.ReturnType.Name != "NuruApp")
      return false;

    return true;
  }
}
