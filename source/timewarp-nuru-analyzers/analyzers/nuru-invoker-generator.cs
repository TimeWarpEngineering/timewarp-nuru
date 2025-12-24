namespace TimeWarp.Nuru;

/// <summary>
/// Source generator that creates typed invoker methods for route handlers.
/// Eliminates the need for DynamicInvoke and enables AOT compatibility.
/// </summary>
/// <remarks>
/// This generator is split into multiple partial files for maintainability:
/// <list type="bullet">
///   <item><description><c>nuru-invoker-generator.cs</c> - Main file with Initialize(), syntax detection, and fluent chain navigation</description></item>
///   <item><description><c>nuru-invoker-generator.extraction.cs</c> - Signature extraction methods (ExtractSignatureFromHandler, ExtractFromLambda, ExtractFromMethodGroup, ExtractFromDelegateCreation, ExtractFromExpression, CreateSignatureFromMethod)</description></item>
///   <item><description><c>nuru-invoker-generator.codegen.cs</c> - Code generation methods (GenerateInvokersClass, GenerateModuleInitializer, GenerateInvokerMethod, GenerateLookupDictionary, helper methods)</description></item>
/// </list>
/// </remarks>
[Generator]
public partial class NuruInvokerGenerator : IIncrementalGenerator
{
  private const string SuppressAttributeName = "TimeWarp.Nuru.SuppressNuruInvokerGenerationAttribute";

  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    // Step 1: Check for [assembly: SuppressNuruInvokerGeneration] attribute
    IncrementalValueProvider<bool> hasSuppressAttribute = context.CompilationProvider
      .Select(static (compilation, _) =>
      {
        foreach (AttributeData attribute in compilation.Assembly.GetAttributes())
        {
          if (attribute.AttributeClass?.ToDisplayString() == SuppressAttributeName)
            return true;
        }

        return false;
      });

    // Step 1b: Check for UseNewGen property
    IncrementalValueProvider<bool> useNewGen = GeneratorHelpers.GetUseNewGenProvider(context);

    // Combine both suppression conditions
    IncrementalValueProvider<bool> shouldSuppress = hasSuppressAttribute
      .Combine(useNewGen)
      .Select(static (pair, _) => pair.Left || pair.Right);

    // Step 2: Find all Map invocations with their delegate signatures
    IncrementalValuesProvider<RouteWithSignature?> routeSignatures = context.SyntaxProvider
      .CreateSyntaxProvider(
        predicate: static (node, _) => IsMapInvocation(node),
        transform: static (ctx, ct) => GetRouteWithSignature(ctx, ct))
      .Where(static info => info?.Signature is not null);

    // Step 3: Collect all unique signatures
    IncrementalValueProvider<ImmutableArray<RouteWithSignature?>> collectedSignatures =
      routeSignatures.Collect();

    // Step 4: Combine signatures with suppress flag
    IncrementalValueProvider<(ImmutableArray<RouteWithSignature?> Routes, bool Suppress)> combined =
      collectedSignatures.Combine(shouldSuppress);

    // Step 5: Generate source code (unless suppressed)
    context.RegisterSourceOutput(combined, static (ctx, data) =>
    {
      // Skip generation if assembly has [SuppressNuruInvokerGeneration] or UseNewGen=true
      if (data.Suppress)
        return;

      ImmutableArray<RouteWithSignature?> routes = data.Routes;

      if (routes.IsDefaultOrEmpty)
        return;

      // Get unique signatures (by identifier)
      HashSet<string> seenIdentifiers = [];
      List<DelegateSignature> uniqueSignatures = [];

      foreach (RouteWithSignature? route in routes)
      {
        if (route?.Signature is null)
          continue;

        if (seenIdentifiers.Add(route.Signature.UniqueIdentifier))
        {
          uniqueSignatures.Add(route.Signature);
        }
      }

      if (uniqueSignatures.Count == 0)
        return;

      string source = GenerateInvokersClass(uniqueSignatures);
      ctx.AddSource("GeneratedRouteInvokers.g.cs", source);
    });
  }

  /// <summary>
  /// Detects WithHandler() invocations in the new fluent API pattern:
  /// app.Map("pattern").WithHandler(handler).Done()
  /// </summary>
  private static bool IsMapInvocation(Microsoft.CodeAnalysis.SyntaxNode node)
  {
    if (node is not InvocationExpressionSyntax invocation)
      return false;

    if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
      return false;

    // Look for WithHandler() calls - this is where the delegate is specified
    return memberAccess.Name.Identifier.Text == "WithHandler";
  }

  /// <summary>
  /// Extracts route information from a WithHandler() invocation in the fluent API pattern:
  /// app.Map("pattern").WithHandler(handler).Done()
  /// </summary>
  private static RouteWithSignature? GetRouteWithSignature(
    GeneratorSyntaxContext context,
    CancellationToken cancellationToken)
  {
    if (context.Node is not InvocationExpressionSyntax withHandlerInvocation)
      return null;

    ArgumentListSyntax? argumentList = withHandlerInvocation.ArgumentList;
    if (argumentList is null || argumentList.Arguments.Count < 1)
      return null;

    // Extract the handler from WithHandler(handler) - it's the first argument
    ArgumentSyntax handlerArgument = argumentList.Arguments[0];

    // Walk back the fluent chain to find Map(pattern)
    string? pattern = FindPatternFromFluentChain(withHandlerInvocation);
    Location location = withHandlerInvocation.GetLocation();

    // If we couldn't find a pattern, use empty string (equivalent to default route)
    pattern ??= string.Empty;

    // Extract delegate signature from the handler argument
    DelegateSignature? signature = ExtractSignatureFromHandler(
      handlerArgument.Expression,
      context.SemanticModel,
      cancellationToken);

    return new RouteWithSignature(pattern, location, signature);
  }

  /// <summary>
  /// Walks back the fluent chain from WithHandler() to find the Map(pattern) call.
  /// Example chain: app.Map("deploy {env}").WithHandler(handler).AsCommand().Done()
  /// We start at WithHandler and walk back to find Map.
  /// </summary>
  private static string? FindPatternFromFluentChain(InvocationExpressionSyntax withHandlerInvocation)
  {
    // WithHandler is called on something like: app.Map("pattern")
    // The syntax tree looks like:
    // InvocationExpression (WithHandler)
    //   - MemberAccessExpression (.WithHandler)
    //     - InvocationExpression (Map("pattern"))
    //       - MemberAccessExpression (.Map)
    //       - ArgumentList ("pattern")

    if (withHandlerInvocation.Expression is not MemberAccessExpressionSyntax memberAccess)
      return null;

    // The expression being accessed is the result of Map("pattern")
    if (memberAccess.Expression is not InvocationExpressionSyntax mapInvocation)
      return null;

    // Verify this is a Map call
    if (mapInvocation.Expression is not MemberAccessExpressionSyntax mapMemberAccess)
      return null;

    if (mapMemberAccess.Name.Identifier.Text != "Map")
      return null;

    // Get the pattern from Map's arguments
    ArgumentListSyntax? mapArgs = mapInvocation.ArgumentList;
    if (mapArgs is null || mapArgs.Arguments.Count < 1)
      return null;

    // First argument should be the pattern
    ArgumentSyntax patternArg = mapArgs.Arguments[0];
    if (patternArg.Expression is LiteralExpressionSyntax literal &&
        literal.IsKind(SyntaxKind.StringLiteralExpression))
    {
      return literal.Token.ValueText;
    }

    return null;
  }
}
