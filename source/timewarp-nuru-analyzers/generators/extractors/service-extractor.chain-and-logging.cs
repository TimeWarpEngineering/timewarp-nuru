#region Purpose
// ServiceExtractor partial: builder chain extraction and AddLogging configuration extraction.
#endregion

namespace TimeWarp.Nuru.Generators;

using Microsoft.CodeAnalysis.Operations;

internal static partial class ServiceExtractor
{
  /// <summary>
  /// Extracts all services from a builder chain, walking ConfigureServices calls.
  /// </summary>
  /// <param name="builderChain">The builder chain to analyze.</param>
  /// <param name="semanticModel">Semantic model for type resolution.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Extraction result with all services and extension methods.</returns>
  public static ServiceExtractionResult ExtractFromChain
  (
    IEnumerable<InvocationExpressionSyntax> builderChain,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    ServiceExtractionResult result = ServiceExtractionResult.Empty;

    foreach (InvocationExpressionSyntax invocation in builderChain)
    {
      string? methodName = GetMethodName(invocation);
      if (methodName == "ConfigureServices")
      {
        ServiceExtractionResult extracted = Extract(invocation, semanticModel, cancellationToken);
        result = result.Merge(extracted);
      }
    }

    return result;
  }

  /// <summary>
  /// Extracts logging configuration from a ConfigureServices() invocation.
  /// Looks for AddLogging(...) calls and captures the lambda body.
  /// </summary>
  /// <param name="configureServicesInvocation">The .ConfigureServices(...) invocation.</param>
  /// <param name="semanticModel">Semantic model for type resolution.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>LoggingConfiguration if AddLogging() is found, otherwise null.</returns>
  public static LoggingConfiguration? ExtractLoggingConfiguration
  (
    InvocationExpressionSyntax configureServicesInvocation,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    ArgumentListSyntax? args = configureServicesInvocation.ArgumentList;
    if (args is null || args.Arguments.Count == 0)
      return null;

    ExpressionSyntax configureExpression = args.Arguments[0].Expression;

    // Handle lambda expressions
    if (configureExpression is LambdaExpressionSyntax lambda)
    {
      return ExtractLoggingFromLambdaBody(lambda.Body, semanticModel, cancellationToken);
    }

    // Handle method group references
    if (configureExpression is IdentifierNameSyntax or MemberAccessExpressionSyntax)
    {
      return ExtractLoggingFromMethodGroup(configureExpression, semanticModel, cancellationToken);
    }

    return null;
  }

  /// <summary>
  /// Extracts logging configuration from a lambda body.
  /// The first Microsoft.Extensions.Logging AddLogging call in the body wins (task 546).
  /// </summary>
  private static LoggingConfiguration? ExtractLoggingFromLambdaBody
  (
    CSharpSyntaxNode body,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    IEnumerable<InvocationExpressionSyntax> invocations = body.DescendantNodesAndSelf()
      .OfType<InvocationExpressionSyntax>();

    foreach (InvocationExpressionSyntax invocation in invocations)
    {
      if (semanticModel.GetOperation(invocation, cancellationToken) is not IInvocationOperation operation
        || !IsAddLoggingMethod(operation.TargetMethod))
      {
        continue;
      }

      return ExtractLoggingLambdaBody(operation);
    }

    return null;
  }

  /// <summary>
  /// True for <c>LoggingServiceCollectionExtensions.AddLogging</c> from Microsoft.Extensions.DependencyInjection,
  /// both the parameterless overload and the one that takes a configure delegate.
  /// </summary>
  private static bool IsAddLoggingMethod(IMethodSymbol method)
  {
    IMethodSymbol target = method.ReducedFrom ?? method;
    return target.Name == "AddLogging"
      && target.ContainingType?.ToDisplayString() == "Microsoft.Extensions.DependencyInjection.LoggingServiceCollectionExtensions";
  }

  /// <summary>
  /// Extracts logging configuration from a method group reference.
  /// </summary>
  private static LoggingConfiguration? ExtractLoggingFromMethodGroup
  (
    ExpressionSyntax methodGroupExpression,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    // Resolve the method symbol
    SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(methodGroupExpression, cancellationToken);

    IMethodSymbol? methodSymbol = symbolInfo.Symbol as IMethodSymbol
      ?? symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();

    if (methodSymbol is null)
      return null;

    // Get the method's syntax declaration
    SyntaxReference? syntaxRef = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault();
    if (syntaxRef is null)
      return null;

    Microsoft.CodeAnalysis.SyntaxNode? methodSyntax = syntaxRef.GetSyntax(cancellationToken);

    // Extract the body based on method syntax type
    CSharpSyntaxNode? methodBody = methodSyntax switch
    {
      MethodDeclarationSyntax method => (CSharpSyntaxNode?)method.Body ?? method.ExpressionBody?.Expression,
      LocalFunctionStatementSyntax localFunc => (CSharpSyntaxNode?)localFunc.Body ?? localFunc.ExpressionBody?.Expression,
      _ => null
    };

    if (methodBody is null)
      return null;

    // Get semantic model for the method's syntax tree (may be different from current)
    SemanticModel methodSemanticModel = methodSyntax.SyntaxTree == semanticModel.SyntaxTree
      ? semanticModel
      : semanticModel.Compilation.GetSemanticModel(methodSyntax.SyntaxTree);

    return ExtractLoggingFromLambdaBody(methodBody, methodSemanticModel, cancellationToken);
  }

  /// <summary>
  /// Builds the logging configuration from an AddLogging invocation.
  /// AddLogging() with no configure delegate registers a factory with no providers (task 546),
  /// so it is an empty configuration, not "no logging".
  /// </summary>
  private static LoggingConfiguration? ExtractLoggingLambdaBody(IInvocationOperation addLoggingOperation)
  {
    // Extension calls carry the IServiceCollection receiver as an argument in both the reduced
    // (services.AddLogging(...)) and static (LoggingServiceCollectionExtensions.AddLogging(services, ...))
    // forms, so the configure delegate is found by its parameter type, which also covers named arguments.
    IArgumentOperation? configureArgument = addLoggingOperation.Arguments.FirstOrDefault(argument =>
      argument.Parameter?.Type is INamedTypeSymbol { Name: "Action", TypeArguments: [{ Name: "ILoggingBuilder" }] });

    if (configureArgument is null)
      return new LoggingConfiguration(string.Empty);

    Microsoft.CodeAnalysis.SyntaxNode configureSyntax = configureArgument.Value.Syntax;

    // Handle lambda: AddLogging(builder => builder.AddConsole())
    if (configureSyntax is LambdaExpressionSyntax lambda)
    {
      // Get the lambda body as text
      string bodyText = lambda.Body.ToFullString().Trim();

      // Extract the lambda parameter name (e.g., "b" from "b => b.AddConsole()")
      string parameterName = lambda switch
      {
        SimpleLambdaExpressionSyntax simple => simple.Parameter.Identifier.Text,
        ParenthesizedLambdaExpressionSyntax paren when paren.ParameterList.Parameters.Count == 1
          => paren.ParameterList.Parameters[0].Identifier.Text,
        _ => "builder"
      };

      return new LoggingConfiguration(bodyText, parameterName);
    }

    // Handle anonymous method: AddLogging(delegate (ILoggingBuilder builder) { ... })
    if (configureSyntax is AnonymousMethodExpressionSyntax { ParameterList.Parameters.Count: 1 } anonymousMethod)
    {
      return new LoggingConfiguration(
        anonymousMethod.Block.ToFullString().Trim(),
        anonymousMethod.ParameterList.Parameters[0].Identifier.Text);
    }

    // Method groups and delegate variables (AddLogging(ConfigureLogging)) are not supported: the
    // delegate may not be reachable from the generated class, and an empty factory would silently
    // drop the user's providers. Leaving logging unconfigured keeps NURU_H007 warning about it.
    return null;
  }
}
