// Main orchestrator extractor that builds AppModel from all DSL sources.
//
// This extractor:
// 1. Receives GeneratorSyntaxContext from the generator
// 2. Uses RunAsyncLocator to find entry point and get InterceptSiteModel
// 3. Traces back through syntax tree to find builder chain
// 4. Uses FluentChainExtractor to extract fluent routes
// 5. Uses AttributedRouteExtractor for [NuruRoute] classes (if any)
// 6. Merges routes from all sources, checks for conflicts
// 7. Returns complete AppModel via AppModelBuilder

namespace TimeWarp.Nuru.Generators;

using RoslynSyntaxNode = Microsoft.CodeAnalysis.SyntaxNode;

/// <summary>
/// Main orchestrator that extracts a complete AppModel from source code.
/// Coordinates all other extractors and merges results from all DSL sources.
/// </summary>
internal static class AppExtractor
{
  /// <summary>
  /// Extracts an AppModel from a RunAsync call site.
  /// </summary>
  /// <param name="context">The generator syntax context containing the RunAsync invocation.</param>
  /// <param name="cancellationToken">Cancellation token for the operation.</param>
  /// <returns>The extracted AppModel, or null if extraction fails.</returns>
  public static AppModel? Extract
  (
    GeneratorSyntaxContext context,
    CancellationToken cancellationToken
  )
  {
    // 1. Extract intercept site from RunAsync location
    InterceptSiteModel? interceptSite = RunAsyncLocator.Extract(context, cancellationToken);
    if (interceptSite is null)
      return null;

    // 2. Get the invocation expression
    if (context.Node is not InvocationExpressionSyntax runAsyncInvocation)
      return null;

    // 3. Trace back to find the app variable and builder chain
    ExpressionSyntax? appExpression = GetAppExpression(runAsyncInvocation);
    if (appExpression is null)
      return null;

    // 4. Find the Build() call that created the app
    InvocationExpressionSyntax? buildCall = FindBuildCall(appExpression, context.SemanticModel, cancellationToken);

    // 5. Start building the AppModel
    AppModelBuilder builder = new();
    builder.WithInterceptSite(interceptSite);

    // 6. If we found a build call, extract from the fluent chain
    if (buildCall is not null)
    {
      FluentChainExtractor.ExtractToBuilder(buildCall, builder, context.SemanticModel, cancellationToken);
    }

    // 7. Build and return the model
    return builder.Build();
  }

  /// <summary>
  /// Extracts an AppModel from a RunAsync call site with additional attributed routes.
  /// </summary>
  /// <param name="context">The generator syntax context containing the RunAsync invocation.</param>
  /// <param name="attributedRoutes">Routes extracted from [NuruRoute] classes.</param>
  /// <param name="cancellationToken">Cancellation token for the operation.</param>
  /// <returns>The extracted AppModel, or null if extraction fails.</returns>
  public static AppModel? ExtractWithAttributedRoutes
  (
    GeneratorSyntaxContext context,
    ImmutableArray<RouteDefinition> attributedRoutes,
    CancellationToken cancellationToken
  )
  {
    AppModel? baseModel = Extract(context, cancellationToken);
    if (baseModel is null)
      return null;

    // Merge attributed routes with fluent routes
    if (attributedRoutes.Length == 0)
      return baseModel;

    // Combine routes (fluent routes first, then attributed)
    ImmutableArray<RouteDefinition> mergedRoutes = baseModel.Routes.AddRange(attributedRoutes);

    return baseModel with { Routes = mergedRoutes };
  }

  /// <summary>
  /// Gets the expression representing the app variable from a RunAsync call.
  /// For app.RunAsync(args), returns the 'app' expression.
  /// </summary>
  private static ExpressionSyntax? GetAppExpression(InvocationExpressionSyntax runAsyncInvocation)
  {
    if (runAsyncInvocation.Expression is not MemberAccessExpressionSyntax memberAccess)
      return null;

    return memberAccess.Expression;
  }

  /// <summary>
  /// Finds the Build() call that created the app variable.
  /// Traces back from the app variable to its assignment.
  /// </summary>
  private static InvocationExpressionSyntax? FindBuildCall
  (
    ExpressionSyntax appExpression,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    // If the app expression is directly a method call ending in .Build(), use it
    if (appExpression is InvocationExpressionSyntax directBuildCall)
    {
      if (IsBuildCall(directBuildCall, semanticModel, cancellationToken))
        return directBuildCall;
    }

    // If it's an identifier (variable), trace back to its declaration
    if (appExpression is IdentifierNameSyntax identifier)
    {
      return FindBuildCallFromVariable(identifier, semanticModel, cancellationToken);
    }

    return null;
  }

  /// <summary>
  /// Traces a variable back to find the Build() call in its initializer.
  /// </summary>
  private static InvocationExpressionSyntax? FindBuildCallFromVariable
  (
    IdentifierNameSyntax identifier,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(identifier, cancellationToken);

    if (symbolInfo.Symbol is not ILocalSymbol localSymbol)
      return null;

    // Find the variable declaration
    foreach (SyntaxReference syntaxRef in localSymbol.DeclaringSyntaxReferences)
    {
      RoslynSyntaxNode declarationNode = syntaxRef.GetSyntax(cancellationToken);

      if (declarationNode is VariableDeclaratorSyntax variableDeclarator)
      {
        // Check the initializer
        EqualsValueClauseSyntax? initializer = variableDeclarator.Initializer;
        if (initializer?.Value is InvocationExpressionSyntax invocation)
        {
          if (IsBuildCall(invocation, semanticModel, cancellationToken))
            return invocation;
        }
      }
    }

    return null;
  }

  /// <summary>
  /// Checks if an invocation is a Build() call on a NuruAppBuilder.
  /// </summary>
  private static bool IsBuildCall
  (
    InvocationExpressionSyntax invocation,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    return BuildLocator.IsPotentialMatch(invocation) &&
           BuildLocator.IsConfirmedBuildCall(invocation, semanticModel, cancellationToken);
  }
}
