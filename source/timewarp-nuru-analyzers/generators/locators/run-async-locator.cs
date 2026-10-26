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

    // app.RunAsync(...) or app?.RunAsync(...)
    SimpleNameSyntax? name = invocation.Expression switch
    {
      MemberAccessExpressionSyntax memberAccess => memberAccess.Name,
      MemberBindingExpressionSyntax memberBinding => memberBinding.Name,
      _ => null
    };

    return name?.Identifier.ValueText == MethodName;
  }

  /// <summary>
  /// The method name and receiver of <c>x.M(...)</c> or <c>x?.M(...)</c> (task 547).
  /// </summary>
  /// <returns>False for any other invocation, such as a static or unqualified call.</returns>
  public static bool TryGetMemberCall(InvocationExpressionSyntax invocation, out SimpleNameSyntax name, out ExpressionSyntax receiver)
  {
    switch (invocation.Expression)
    {
      case MemberAccessExpressionSyntax memberAccess:
        name = memberAccess.Name;
        receiver = memberAccess.Expression;
        return true;

      // x?.M(...): the receiver is the conditional access whose WhenNotNull holds the call
      case MemberBindingExpressionSyntax memberBinding when FindConditionalAccess(invocation) is { } conditionalAccess:
        name = memberBinding.Name;
        receiver = conditionalAccess.Expression;
        return true;

      default:
        name = null!;
        receiver = null!;
        return false;
    }
  }

  /// <summary>
  /// The node NURU_I001 suppression compares (task 547): the method, accessor, local function's
  /// enclosing member or other member that holds <paramref name="node"/>, or the compilation unit for
  /// top-level statements and for nodes outside any member.
  /// </summary>
  public static RoslynSyntaxNode EntryPointScope(RoslynSyntaxNode node)
  {
    MemberDeclarationSyntax? member = node.FirstAncestorOrSelf<MemberDeclarationSyntax>();
    return member is null or GlobalStatementSyntax ? node.SyntaxTree.GetRoot() : member;
  }

  private static ConditionalAccessExpressionSyntax? FindConditionalAccess(InvocationExpressionSyntax invocation)
  {
    for (RoslynSyntaxNode node = invocation; node.Parent is not null; node = node.Parent)
    {
      if (node.Parent is ConditionalAccessExpressionSyntax conditionalAccess && conditionalAccess.WhenNotNull == node)
        return conditionalAccess;
    }

    return null;
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
    if (context.Node is not InvocationExpressionSyntax invocation)
      return null;

    if (!TryGetMemberCall(invocation, out SimpleNameSyntax name, out ExpressionSyntax receiver))
      return null;

    if (context.SemanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol { ContainingType.Name: "NuruApp" })
      return null;

    return new RunAsyncCall(
      InterceptSiteExtractor.Extract(context.SemanticModel, invocation),
      name.GetLocation(),
      EntryPointScope(invocation).GetLocation(),
      DescribeReceiver(receiver));
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
/// <param name="Scope">Location of the call's <see cref="RunAsyncLocator.EntryPointScope"/>.</param>
/// <param name="ReceiverText">The receiver as written, for the diagnostic message.</param>
internal sealed record RunAsyncCall(InterceptSiteModel? Site, Location Location, Location Scope, string ReceiverText);
