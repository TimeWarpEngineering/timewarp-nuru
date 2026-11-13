#region Purpose
// Extracts handler information from lambda expressions and method groups (e.g. HandleDeploy) into a
// HandlerDefinition with parameters, return type, and async info.
#endregion

namespace TimeWarp.Nuru.Generators;

using RoslynParameterSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.ParameterSyntax;

/// <summary>
/// Extracts handler information from lambda expressions and method references.
/// </summary>
internal static partial class HandlerExtractor
{
  /// <summary>
  /// Extracts a HandlerDefinition from a WithHandler() invocation.
  /// </summary>
  /// <param name="withHandlerInvocation">The .WithHandler(...) invocation expression.</param>
  /// <param name="semanticModel">Semantic model for type resolution.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>The extracted handler definition, or null if extraction fails.</returns>
  public static HandlerDefinition? Extract
  (
    InvocationExpressionSyntax withHandlerInvocation,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    ArgumentListSyntax? args = withHandlerInvocation.ArgumentList;
    if (args is null || args.Arguments.Count == 0)
      return null;

    ExpressionSyntax handlerExpression = args.Arguments[0].Expression;

    return handlerExpression switch
    {
      ParenthesizedLambdaExpressionSyntax lambda => ExtractFromLambda(lambda, semanticModel, cancellationToken),
      SimpleLambdaExpressionSyntax simpleLambda => ExtractFromSimpleLambda(simpleLambda, semanticModel, cancellationToken),
      AnonymousMethodExpressionSyntax anonymousMethod => ExtractFromAnonymousMethod(anonymousMethod, semanticModel, cancellationToken),
      IdentifierNameSyntax methodGroup => ExtractFromMethodGroup(methodGroup, semanticModel, cancellationToken),
      MemberAccessExpressionSyntax memberAccess => ExtractFromMemberAccess(memberAccess, semanticModel, cancellationToken),
      _ => CreateDefaultHandler()
    };
  }

  /// <summary>
  /// Extracts handler information from a parenthesized lambda expression.
  /// </summary>
  private static HandlerDefinition? ExtractFromLambda
  (
    ParenthesizedLambdaExpressionSyntax lambda,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    ImmutableArray<ParameterBinding>.Builder parameters = ImmutableArray.CreateBuilder<ParameterBinding>();
    bool hasCancellationToken = false;
    bool requiresServiceProvider = false;

    // Capture lambda body source
    string? lambdaBodySource = null;
    bool isExpressionBody = false;

    if (lambda.Body is ExpressionSyntax expr)
    {
      lambdaBodySource = expr.ToFullString().Trim();
      isExpressionBody = true;
    }
    else if (lambda.Body is BlockSyntax block)
    {
      lambdaBodySource = block.ToFullString();
      isExpressionBody = false;
    }

    // Try to get symbol info for accurate type resolution
    SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(lambda, cancellationToken);

    if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
    {
      // Use method symbol for accurate parameter/return type info
      // but also capture the lambda body source
      HandlerDefinition baseDefinition = ExtractFromMethodSymbol(methodSymbol, semanticModel.Compilation);
      return baseDefinition with
      {
        LambdaBodySource = lambdaBodySource,
        IsExpressionBody = isExpressionBody
      };
    }

    // Fallback to syntax-only analysis
    foreach (RoslynParameterSyntax param in lambda.ParameterList.Parameters)
    {
      // ValueText strips a leading @ so ParameterName never stores the escape prefix
      string paramName = param.Identifier.ValueText;
      string typeName = NormalizeTypeName(param.Type?.ToString() ?? "object");

      if (IsCancellationTokenType(typeName))
      {
        hasCancellationToken = true;
        parameters.Add(ParameterBinding.ForCancellationToken(paramName));
      }
      // No symbol was available for this lambda (partial/uncompilable code only —
      // valid code always resolves via ExtractFromMethodSymbol above), so classification
      // falls back to the string heuristic. See IsServiceType(string) for rationale.
      else if (IsServiceType(typeName))
      {
        requiresServiceProvider = true;
        parameters.Add(ParameterBinding.FromService(paramName, typeName));
      }
      else
      {
        // Assume it's a route parameter - will be matched later
        parameters.Add(ParameterBinding.FromParameter(
          parameterName: paramName,
          typeName: typeName,
          segmentName: paramName.ToLowerInvariant(),
          isOptional: param.Default is not null,
          defaultValue: param.Default?.Value.ToString(),
          requiresConversion: typeName != "global::System.String"));
      }
    }

    // Determine return type and async status
    bool isAsync = lambda.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword);
    HandlerReturnType returnType = isAsync ? HandlerReturnType.Task : HandlerReturnType.Void;

    // Try to infer return type from body
    if (lambda.Body is not null)
    {
      returnType = InferReturnType(lambda.Body, isAsync, semanticModel, cancellationToken);
    }

    return new HandlerDefinition(
      HandlerKind: HandlerKind.Delegate,
      FullTypeName: null,
      MethodName: null,
      LambdaBodySource: lambdaBodySource,
      IsExpressionBody: isExpressionBody,
      Parameters: parameters.ToImmutable(),
      ReturnType: returnType,
      IsAsync: isAsync,
      RequiresCancellationToken: hasCancellationToken,
      RequiresServiceProvider: requiresServiceProvider,
      HasAsyncModifier: isAsync);
  }

  /// <summary>
  /// Extracts handler information from a simple lambda expression (single parameter).
  /// </summary>
  private static HandlerDefinition? ExtractFromSimpleLambda
  (
    SimpleLambdaExpressionSyntax lambda,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    // Capture lambda body source
    string? lambdaBodySource = null;
    bool isExpressionBody = false;

    if (lambda.Body is ExpressionSyntax expr)
    {
      lambdaBodySource = expr.ToFullString().Trim();
      isExpressionBody = true;
    }
    else if (lambda.Body is BlockSyntax block)
    {
      lambdaBodySource = block.ToFullString();
      isExpressionBody = false;
    }

    SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(lambda, cancellationToken);

    if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
    {
      // Use method symbol for accurate parameter/return type info
      // but also capture the lambda body source
      HandlerDefinition baseDefinition = ExtractFromMethodSymbol(methodSymbol, semanticModel.Compilation);
      return baseDefinition with
      {
        LambdaBodySource = lambdaBodySource,
        IsExpressionBody = isExpressionBody
      };
    }

    // Fallback to syntax-only — ValueText strips leading @ from escaped identifiers
    string paramName = lambda.Parameter.Identifier.ValueText;

    ImmutableArray<ParameterBinding> parameters =
    [
      ParameterBinding.FromParameter(
        parameterName: paramName,
        typeName: "global::System.Object",
        segmentName: paramName.ToLowerInvariant())
    ];

    bool isAsync = lambda.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword);
    HandlerReturnType returnType = isAsync ? HandlerReturnType.Task : HandlerReturnType.Void;

    // Try to infer return type from body
    if (lambda.Body is not null)
    {
      returnType = InferReturnType(lambda.Body, isAsync, semanticModel, cancellationToken);
    }

    return new HandlerDefinition(
      HandlerKind: HandlerKind.Delegate,
      FullTypeName: null,
      MethodName: null,
      LambdaBodySource: lambdaBodySource,
      IsExpressionBody: isExpressionBody,
      Parameters: parameters,
      ReturnType: returnType,
      IsAsync: isAsync,
      RequiresCancellationToken: false,
      RequiresServiceProvider: false,
      HasAsyncModifier: isAsync);
  }

  /// <summary>
  /// Extracts handler information from an anonymous method expression (C# 2.0 delegate syntax).
  /// Example: delegate(string name) { return name.ToUpper(); }
  /// </summary>
  private static HandlerDefinition? ExtractFromAnonymousMethod
  (
    AnonymousMethodExpressionSyntax anonymousMethod,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    ImmutableArray<ParameterBinding>.Builder parameters = ImmutableArray.CreateBuilder<ParameterBinding>();
    bool hasCancellationToken = false;
    bool requiresServiceProvider = false;

    // Anonymous methods always have block bodies (no expression bodies)
    string? lambdaBodySource = anonymousMethod.Block?.ToFullString();
    const bool IsExpressionBody = false;

    // Try to get symbol info for accurate type resolution
    SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(anonymousMethod, cancellationToken);

    if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
    {
      // Use method symbol for accurate parameter/return type info
      HandlerDefinition baseDefinition = ExtractFromMethodSymbol(methodSymbol, semanticModel.Compilation);
      return baseDefinition with
      {
        LambdaBodySource = lambdaBodySource,
        IsExpressionBody = IsExpressionBody
      };
    }

    // Fallback to syntax-only analysis
    // Anonymous methods can omit the parameter list entirely: delegate { ... }
    if (anonymousMethod.ParameterList is not null)
    {
      foreach (RoslynParameterSyntax param in anonymousMethod.ParameterList.Parameters)
      {
        // ValueText strips a leading @ so ParameterName never stores the escape prefix
        string paramName = param.Identifier.ValueText;
        string typeName = NormalizeTypeName(param.Type?.ToString() ?? "object");

        if (IsCancellationTokenType(typeName))
        {
          hasCancellationToken = true;
          parameters.Add(ParameterBinding.ForCancellationToken(paramName));
        }
        // No symbol was available for this anonymous method (partial/uncompilable code
        // only — valid code always resolves via ExtractFromMethodSymbol above), so
        // classification falls back to the string heuristic.
        else if (IsServiceType(typeName))
        {
          requiresServiceProvider = true;
          parameters.Add(ParameterBinding.FromService(paramName, typeName));
        }
        else
        {
          // Assume it's a route parameter
          parameters.Add(ParameterBinding.FromParameter(
            parameterName: paramName,
            typeName: typeName,
            segmentName: paramName.ToLowerInvariant(),
            isOptional: param.Default is not null,
            defaultValue: param.Default?.Value.ToString(),
            requiresConversion: typeName != "global::System.String"));
        }
      }
    }

    // Determine return type and async status
    bool isAsync = anonymousMethod.AsyncKeyword.IsKind(SyntaxKind.AsyncKeyword);
    HandlerReturnType returnType = isAsync ? HandlerReturnType.Task : HandlerReturnType.Void;

    // Try to infer return type from body
    if (anonymousMethod.Block is not null)
    {
      returnType = InferReturnType(anonymousMethod.Block, isAsync, semanticModel, cancellationToken);
    }

    return new HandlerDefinition(
      HandlerKind: HandlerKind.Delegate,
      FullTypeName: null,
      MethodName: null,
      LambdaBodySource: lambdaBodySource,
      IsExpressionBody: IsExpressionBody,
      Parameters: parameters.ToImmutable(),
      ReturnType: returnType,
      IsAsync: isAsync,
      RequiresCancellationToken: hasCancellationToken,
      RequiresServiceProvider: requiresServiceProvider,
      HasAsyncModifier: isAsync);
  }

  /// <summary>
  /// Extracts handler information from a method group expression.
  /// </summary>
  private static HandlerDefinition? ExtractFromMethodGroup
  (
    IdentifierNameSyntax methodGroup,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(methodGroup, cancellationToken);

    // For method groups, Symbol may be null but CandidateSymbols contains the method(s)
    if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
    {
      return ExtractFromMethodSymbolAsMethod(methodSymbol, semanticModel.Compilation);
    }

    // Check CandidateSymbols - method groups often have the method here
    if (symbolInfo.CandidateSymbols.Length > 0 &&
        symbolInfo.CandidateSymbols[0] is IMethodSymbol candidateMethod)
    {
      return ExtractFromMethodSymbolAsMethod(candidateMethod, semanticModel.Compilation);
    }

    // If we can't resolve, create a minimal handler as delegate (not method)
    return HandlerDefinition.ForDelegate(
      parameters: [],
      returnType: HandlerReturnType.Void,
      isAsync: false);
  }

  /// <summary>
  /// Extracts handler information from a member access expression (e.g., obj.Method).
  /// </summary>
  private static HandlerDefinition? ExtractFromMemberAccess
  (
    MemberAccessExpressionSyntax memberAccess,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(memberAccess, cancellationToken);

    if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
    {
      return ExtractFromMethodSymbolAsMethod(methodSymbol, semanticModel.Compilation);
    }

    // Check CandidateSymbols - method groups often have the method here
    if (symbolInfo.CandidateSymbols.Length > 0 &&
        symbolInfo.CandidateSymbols[0] is IMethodSymbol candidateMethod)
    {
      return ExtractFromMethodSymbolAsMethod(candidateMethod, semanticModel.Compilation);
    }

    // Fallback to delegate since we don't have type info
    return HandlerDefinition.ForDelegate(
      parameters: [],
      returnType: HandlerReturnType.Void,
      isAsync: false);
  }
}
