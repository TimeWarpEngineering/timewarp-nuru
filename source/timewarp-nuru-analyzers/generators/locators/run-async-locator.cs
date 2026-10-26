// Locator for app.RunAsync(...) call sites.
// This is the entry point for the V2 generator - we intercept this method.

namespace TimeWarp.Nuru.Generators;

using RoslynSyntaxNode = Microsoft.CodeAnalysis.SyntaxNode;

/// <summary>
/// Locates RunAsync() call sites on NuruApp instances.
/// These are the interception targets for the generated code.
/// </summary>
internal static class RunAsyncLocator
{
  private const string MethodName = "RunAsync";

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
  /// Extracts the intercept site from a confirmed RunAsync call.
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
    if (containingType?.Name != "NuruApp")
      return null;

    // Use the new Roslyn API to get an InterceptableLocation
    return InterceptSiteExtractor.Extract(context.SemanticModel, invocation);
  }

  /// <summary>
  /// Locates a confirmed NuruApp.RunAsync() call for the NURU_I001 check (task 547).
  /// </summary>
  /// <returns>The call, with a null site when it cannot be intercepted, or null when the call is not NuruApp.RunAsync().</returns>
  public static RunAsyncCall? LocateCall
  (
    GeneratorSyntaxContext context,
    CancellationToken cancellationToken
  )
  {
    if (context.Node is not InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax memberAccess } invocation)
      return null;

    if (context.SemanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol { ContainingType.Name: "NuruApp" })
      return null;

    return new RunAsyncCall(
      InterceptSiteExtractor.Extract(context.SemanticModel, invocation),
      memberAccess.Name.GetLocation(),
      DescribeReceiver(memberAccess.Expression));
  }

  /// <summary>
  /// The receiver on one line, shortened, for the diagnostic message.
  /// </summary>
  private static string DescribeReceiver(ExpressionSyntax receiver)
  {
    const int MaxLength = 60;
    string text = string.Join(" ", receiver.ToString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    return text.Length <= MaxLength ? text : text[..(MaxLength - 3)] + "...";
  }
}

/// <summary>
/// A NuruApp.RunAsync() call site, for reporting calls no app intercepts.
/// </summary>
/// <param name="Site">The intercept site, or null when Roslyn cannot intercept the call.</param>
/// <param name="Location">Location of the RunAsync name, where NURU_I001 is reported.</param>
/// <param name="ReceiverText">The receiver as written, for the diagnostic message.</param>
internal sealed record RunAsyncCall(InterceptSiteModel? Site, Location Location, string ReceiverText);
