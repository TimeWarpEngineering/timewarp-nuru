// Locator for app.RunReplAsync(...) call sites.
// This is used by the V2 generator to intercept RunReplAsync method calls.

namespace TimeWarp.Nuru.Generators;

using RoslynSyntaxNode = Microsoft.CodeAnalysis.SyntaxNode;

/// <summary>
/// Locates RunReplAsync() call sites on NuruCoreApp instances.
/// These are the interception targets for the generated code.
/// </summary>
internal static class RunReplAsyncLocator
{
  private const string MethodName = "RunReplAsync";

  /// <summary>
  /// Fast syntactic check to filter candidates.
  /// </summary>
  public static bool IsPotentialMatch(RoslynSyntaxNode node)
  {
    if (node is not InvocationExpressionSyntax invocation)
      return false;

    if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
      return false;

    return memberAccess.Name.Identifier.ValueText == MethodName;
  }

  /// <summary>
  /// Extracts the intercept site from a confirmed RunReplAsync call.
  /// Uses the new .NET 10 / C# 14 InterceptableLocation API.
  /// </summary>
  public static InterceptSiteModel? Extract
  (
    GeneratorSyntaxContext context,
    CancellationToken cancellationToken
  )
  {
    if (context.Node is not InvocationExpressionSyntax invocation)
      return null;

    if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
      return null;

    SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, cancellationToken);

    if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
      return null;

    INamedTypeSymbol? containingType = methodSymbol.ContainingType;
    if (containingType?.Name != "NuruCoreApp")
      return null;

    // Use the new Roslyn API to get an InterceptableLocation
    return InterceptSiteExtractor.Extract(context.SemanticModel, invocation);
  }
}
