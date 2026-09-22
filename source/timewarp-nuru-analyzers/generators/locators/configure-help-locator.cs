// Locator for .ConfigureHelp(...) calls.

namespace TimeWarp.Nuru.Generators;

using RoslynSyntaxNode = Microsoft.CodeAnalysis.SyntaxNode;

#region Purpose
// Identifies ConfigureHelp invocations for DSL interpretation.
#endregion

/// <summary>
/// Locates .ConfigureHelp(...) calls that configure help output filtering.
/// </summary>
internal static class ConfigureHelpLocator
{
  private const string MethodName = "ConfigureHelp";

  public static bool IsPotentialMatch(RoslynSyntaxNode node)
  {
    if (node is not InvocationExpressionSyntax invocation)
      return false;

    if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
      return false;

    return memberAccess.Name.Identifier.ValueText == MethodName;
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

    return invocation;
  }
}
