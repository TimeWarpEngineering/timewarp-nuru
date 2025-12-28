// Main orchestrator extractor that builds AppModel from all DSL sources.
//
// This extractor:
// 1. Receives GeneratorSyntaxContext from the generator
// 2. Finds the containing block (method body) with the DSL code
// 3. Uses DslInterpreter to semantically interpret the DSL
// 4. Returns complete AppModel
//
// Phase 5: Uses DslInterpreter instead of FluentChainExtractor for robust
// handling of nested groups and fragmented code styles.

namespace TimeWarp.Nuru.Generators;

using RoslynSyntaxNode = Microsoft.CodeAnalysis.SyntaxNode;

/// <summary>
/// Main orchestrator that extracts a complete AppModel from source code.
/// Uses DslInterpreter for semantic interpretation of DSL code.
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
    // 1. Get the invocation expression (RunAsync call)
    if (context.Node is not InvocationExpressionSyntax runAsyncInvocation)
      return null;

    // 2. Find the containing block (method body)
    BlockSyntax? block = FindContainingBlock(runAsyncInvocation);
    if (block is null)
      return null;

    // 3. Use DslInterpreter to interpret the block
    DslInterpreter interpreter = new(context.SemanticModel, cancellationToken);
    IReadOnlyList<AppModel> models = interpreter.Interpret(block);

    // 4. Return the first (and typically only) app model
    // The interpreter already handles RunAsync intercept site extraction
    return models.Count > 0 ? models[0] : null;
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
  /// Finds the containing block (method body) for a syntax node.
  /// Walks up the syntax tree until a BlockSyntax is found.
  /// </summary>
  private static BlockSyntax? FindContainingBlock(RoslynSyntaxNode node)
  {
    RoslynSyntaxNode? current = node.Parent;
    while (current is not null)
    {
      if (current is BlockSyntax block)
        return block;

      current = current.Parent;
    }

    return null;
  }
}
