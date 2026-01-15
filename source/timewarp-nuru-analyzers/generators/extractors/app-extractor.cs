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
  // Default usings already emitted by the generator - filter these out to avoid duplicates
  private static readonly HashSet<string> DefaultUsings =
  [
    "System.Linq",
    "System.Net.Http",
    "System.Reflection",
    "System.Runtime.CompilerServices",
    "System.Text.Json",
    "System.Text.Json.Serialization",
    "System.Text.RegularExpressions",
    "System.Threading.Tasks",
    "Microsoft.Extensions.Configuration",
    "Microsoft.Extensions.Configuration.Json",
    "Microsoft.Extensions.Configuration.EnvironmentVariables",
    "Microsoft.Extensions.Configuration.UserSecrets",
    "TimeWarp.Nuru",
    "TimeWarp.Terminal"
  ];

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

    // 2. Find the CompilationUnit (needed for both usings extraction and top-level statements)
    CompilationUnitSyntax? compilationUnit = FindCompilationUnit(runAsyncInvocation);
    if (compilationUnit is null)
      return null;

    // 3. Find the containing block (method body) or use top-level statements
    DslInterpreter interpreter = new(context.SemanticModel, cancellationToken);
    IReadOnlyList<AppModel> models;

    BlockSyntax? block = FindContainingBlock(runAsyncInvocation);
    if (block is not null)
    {
      // Traditional method body
      models = interpreter.Interpret(block);
    }
    else
    {
      // Top-level statements
      models = interpreter.InterpretTopLevelStatements(compilationUnit);
    }

    // 4. Find the model that contains THIS specific RunAsync call's intercept site
    if (models.Count == 0)
      return null;

    // Extract user's using directives
    ImmutableArray<string> userUsings = ExtractUserUsings(compilationUnit);

    // If only one model, return it (common case)
    if (models.Count == 1)
      return models[0] with { UserUsings = userUsings };

    // Multiple models: find the one that owns this specific RunAsync call
    // Get the intercept site for the current RunAsync invocation
    InterceptSiteModel? currentSite = InterceptSiteExtractor.Extract(context.SemanticModel, runAsyncInvocation);
    if (currentSite is null)
      return models[0] with { UserUsings = userUsings }; // Fallback

    // Find the model whose InterceptSitesByMethod contains this call
    foreach (AppModel model in models)
    {
      if (!model.InterceptSitesByMethod.TryGetValue("RunAsync", out ImmutableArray<InterceptSiteModel> sites))
        continue;

      foreach (InterceptSiteModel site in sites)
      {
        // Match by file path, line, and column
        if (site.FilePath == currentSite.FilePath &&
            site.Line == currentSite.Line &&
            site.Column == currentSite.Column)
        {
          return model with { UserUsings = userUsings };
        }
      }
    }

    // Fallback to first model if no match found (shouldn't happen)
    return models[0] with { UserUsings = userUsings };
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
  /// Extracts an AppModel from a RunAsync call site, returning an ExtractionResult with diagnostics.
  /// This method collects errors as diagnostics instead of throwing exceptions.
  /// </summary>
  /// <param name="context">The generator syntax context containing the RunAsync invocation.</param>
  /// <param name="cancellationToken">Cancellation token for the operation.</param>
  /// <returns>Extraction result containing the model and any diagnostics.</returns>
  public static ExtractionResult ExtractWithDiagnostics
  (
    GeneratorSyntaxContext context,
    CancellationToken cancellationToken
  )
  {
    // 1. Get the invocation expression (RunAsync call)
    if (context.Node is not InvocationExpressionSyntax runAsyncInvocation)
      return ExtractionResult.Empty;

    // 2. Find the CompilationUnit (needed for usings extraction)
    CompilationUnitSyntax? compilationUnit = FindCompilationUnit(runAsyncInvocation);
    if (compilationUnit is null)
      return ExtractionResult.Empty;

    // 3. Trace back from RunAsync to find the app builder using semantic model
    DslInterpreter interpreter = new(context.SemanticModel, cancellationToken);
    ExtractionResult result = interpreter.ExtractFromEntryPointCall(runAsyncInvocation, isReplCall: false);

    // 4. If we have a model, add user usings
    if (result.Model is not null)
    {
      ImmutableArray<string> userUsings = ExtractUserUsings(compilationUnit);
      AppModel modelWithUsings = result.Model with { UserUsings = userUsings };
      return new ExtractionResult(modelWithUsings, result.Diagnostics);
    }

    return result;
  }

  /// <summary>
  /// Extracts an AppModel from a RunReplAsync call site, returning an ExtractionResult with diagnostics.
  /// This is similar to ExtractWithDiagnostics but for RunReplAsync calls.
  /// </summary>
  /// <param name="context">The generator syntax context containing the RunReplAsync invocation.</param>
  /// <param name="cancellationToken">Cancellation token for the operation.</param>
  /// <returns>Extraction result containing the model and any diagnostics.</returns>
  public static ExtractionResult ExtractRunReplAsyncWithDiagnostics
  (
    GeneratorSyntaxContext context,
    CancellationToken cancellationToken
  )
  {
    // 1. Get the invocation expression (RunReplAsync call)
    if (context.Node is not InvocationExpressionSyntax runReplAsyncInvocation)
      return ExtractionResult.Empty;

    // 2. Find the CompilationUnit (needed for usings extraction)
    CompilationUnitSyntax? compilationUnit = FindCompilationUnit(runReplAsyncInvocation);
    if (compilationUnit is null)
      return ExtractionResult.Empty;

    // 3. Trace back from RunReplAsync to find the app builder using semantic model
    DslInterpreter interpreter = new(context.SemanticModel, cancellationToken);
    ExtractionResult result = interpreter.ExtractFromEntryPointCall(runReplAsyncInvocation, isReplCall: true);

    // 4. If we have a model, add user usings
    if (result.Model is not null)
    {
      ImmutableArray<string> userUsings = ExtractUserUsings(compilationUnit);
      AppModel modelWithUsings = result.Model with { UserUsings = userUsings };
      return new ExtractionResult(modelWithUsings, result.Diagnostics);
    }

    return result;
  }

  /// <summary>
  /// Extracts an AppModel from a Build() call site, including all entry points (RunAsync, RunReplAsync).
  /// This is the preferred extraction method as it starts from the app definition rather than entry points,
  /// avoiding duplicate extractions when an app has multiple entry points.
  /// </summary>
  /// <param name="context">The generator syntax context containing the Build() invocation.</param>
  /// <param name="cancellationToken">Cancellation token for the operation.</param>
  /// <returns>Extraction result containing the model and any diagnostics.</returns>
  public static ExtractionResult ExtractFromBuildCall
  (
    GeneratorSyntaxContext context,
    CancellationToken cancellationToken
  )
  {
    // 1. Get the invocation expression (Build() call)
    if (context.Node is not InvocationExpressionSyntax buildInvocation)
      return ExtractionResult.Empty;

    // 2. Verify this is a NuruCoreApp Build() call
    if (!BuildLocator.IsConfirmedBuildCall(buildInvocation, context.SemanticModel, cancellationToken))
      return ExtractionResult.Empty;

    // 3. Find the CompilationUnit (needed for usings extraction)
    CompilationUnitSyntax? compilationUnit = FindCompilationUnit(buildInvocation);
    if (compilationUnit is null)
      return ExtractionResult.Empty;

    // 4. Find the containing block or use top-level statements
    DslInterpreter interpreter = new(context.SemanticModel, cancellationToken);
    ExtractionResult result;

    BlockSyntax? block = FindContainingBlock(buildInvocation);
    if (block is not null)
    {
      // Traditional method body - interpret the whole block
      result = interpreter.InterpretWithDiagnostics(block);
    }
    else
    {
      // Top-level statements
      result = interpreter.InterpretTopLevelStatementsWithDiagnostics(compilationUnit);
    }

    // 5. If we have a model, add user usings and set build location
    if (result.Model is not null)
    {
      ImmutableArray<string> userUsings = ExtractUserUsings(compilationUnit);
      string buildLocation = buildInvocation.GetLocation().GetLineSpan().ToString();
      AppModel modelWithMetadata = result.Model with
      {
        UserUsings = userUsings,
        BuildLocation = buildLocation
      };
      return new ExtractionResult(modelWithMetadata, result.Diagnostics);
    }

    return result;
  }

  /// <summary>
  /// Extracts an AppModel with attributed routes, returning an ExtractionResult with diagnostics.
  /// </summary>
  /// <param name="context">The generator syntax context containing the RunAsync invocation.</param>
  /// <param name="attributedRoutes">Routes extracted from [NuruRoute] classes.</param>
  /// <param name="cancellationToken">Cancellation token for the operation.</param>
  /// <returns>Extraction result containing the model and any diagnostics.</returns>
  public static ExtractionResult ExtractWithAttributedRoutesAndDiagnostics
  (
    GeneratorSyntaxContext context,
    ImmutableArray<RouteDefinition> attributedRoutes,
    CancellationToken cancellationToken
  )
  {
    ExtractionResult baseResult = ExtractWithDiagnostics(context, cancellationToken);
    if (baseResult.Model is null)
      return baseResult;

    // Merge attributed routes with fluent routes
    if (attributedRoutes.Length == 0)
      return baseResult;

    // Combine routes (fluent routes first, then attributed)
    ImmutableArray<RouteDefinition> mergedRoutes = baseResult.Model.Routes.AddRange(attributedRoutes);
    AppModel mergedModel = baseResult.Model with { Routes = mergedRoutes };

    return new ExtractionResult(mergedModel, baseResult.Diagnostics);
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
      {
        // Only return blocks that are method bodies, not nested blocks (try, using, if, etc.)
        if (block.Parent is MethodDeclarationSyntax or
            LocalFunctionStatementSyntax or
            AccessorDeclarationSyntax or
            ConstructorDeclarationSyntax or
            DestructorDeclarationSyntax or
            OperatorDeclarationSyntax or
            ConversionOperatorDeclarationSyntax or
            AnonymousMethodExpressionSyntax or
            ParenthesizedLambdaExpressionSyntax or
            SimpleLambdaExpressionSyntax)
        {
          return block;
        }
        // Keep walking up for nested blocks
      }

      current = current.Parent;
    }

    return null;
  }

  /// <summary>
  /// Finds the containing CompilationUnit for a syntax node.
  /// Used for top-level statement support where there is no BlockSyntax.
  /// </summary>
  private static CompilationUnitSyntax? FindCompilationUnit(RoslynSyntaxNode node)
  {
    RoslynSyntaxNode? current = node.Parent;
    while (current is not null)
    {
      if (current is CompilationUnitSyntax compilationUnit)
        return compilationUnit;

      current = current.Parent;
    }

    return null;
  }

  /// <summary>
  /// Extracts user's using directives from a CompilationUnit and converts them to global form.
  /// Filters out usings that are already included in the generated code's default set.
  /// </summary>
  /// <param name="compilationUnit">The compilation unit containing using directives.</param>
  /// <returns>User's using directives in global form.</returns>
  private static ImmutableArray<string> ExtractUserUsings(CompilationUnitSyntax compilationUnit)
  {
    ImmutableArray<string>.Builder usings = ImmutableArray.CreateBuilder<string>();

    foreach (UsingDirectiveSyntax usingDirective in compilationUnit.Usings)
    {
      // Skip alias usings (using Foo = Bar;)
      if (usingDirective.Alias is not null)
        continue;

      string? namespaceName = usingDirective.Name?.ToString();
      if (string.IsNullOrEmpty(namespaceName))
        continue;

      // Skip usings already in the default set
      if (DefaultUsings.Contains(namespaceName))
        continue;

      // Convert to global form for safety in generated code
      string globalUsing = usingDirective.StaticKeyword != default
        ? $"using static global::{namespaceName};"
        : $"using global::{namespaceName};";

      usings.Add(globalUsing);
    }

    return usings.ToImmutable();
  }
}
