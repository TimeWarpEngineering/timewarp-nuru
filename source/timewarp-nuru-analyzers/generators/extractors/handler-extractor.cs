// Extracts handler information from lambda expressions and method references.
//
// Handles:
// - Lambda expressions: (string env, bool force) => Deploy(env, force)
// - Method group expressions: HandleDeploy
// - Returns HandlerDefinition with parameters, return type, async info

namespace TimeWarp.Nuru.Generators;

using RoslynParameterSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.ParameterSyntax;

/// <summary>
/// Extracts handler information from lambda expressions and method references.
/// </summary>
internal static class HandlerExtractor
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
      HandlerDefinition baseDefinition = ExtractFromMethodSymbol(methodSymbol);
      return baseDefinition with
      {
        LambdaBodySource = lambdaBodySource,
        IsExpressionBody = isExpressionBody
      };
    }

    // Fallback to syntax-only analysis
    foreach (RoslynParameterSyntax param in lambda.ParameterList.Parameters)
    {
      string paramName = param.Identifier.Text;
      string typeName = NormalizeTypeName(param.Type?.ToString() ?? "object");

      if (IsCancellationTokenType(typeName))
      {
        hasCancellationToken = true;
        parameters.Add(ParameterBinding.ForCancellationToken(paramName));
      }
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
      RequiresServiceProvider: requiresServiceProvider);
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
      HandlerDefinition baseDefinition = ExtractFromMethodSymbol(methodSymbol);
      return baseDefinition with
      {
        LambdaBodySource = lambdaBodySource,
        IsExpressionBody = isExpressionBody
      };
    }

    // Fallback to syntax-only
    string paramName = lambda.Parameter.Identifier.Text;

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
      RequiresServiceProvider: false);
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

    if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
    {
      return ExtractFromMethodSymbolAsMethod(methodSymbol);
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
      return ExtractFromMethodSymbolAsMethod(methodSymbol);
    }

    // Fallback to delegate since we don't have type info
    return HandlerDefinition.ForDelegate(
      parameters: [],
      returnType: HandlerReturnType.Void,
      isAsync: false);
  }

  /// <summary>
  /// Extracts handler information from a resolved method symbol (for delegates).
  /// </summary>
  private static HandlerDefinition ExtractFromMethodSymbol(IMethodSymbol methodSymbol)
  {
    ImmutableArray<ParameterBinding>.Builder parameters = ImmutableArray.CreateBuilder<ParameterBinding>();
    bool hasCancellationToken = false;
    bool requiresServiceProvider = false;

    foreach (IParameterSymbol param in methodSymbol.Parameters)
    {
      string typeName = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

      if (IsCancellationTokenType(typeName))
      {
        hasCancellationToken = true;
        parameters.Add(ParameterBinding.ForCancellationToken(param.Name));
      }
      else if (IsServiceType(typeName))
      {
        requiresServiceProvider = true;
        parameters.Add(ParameterBinding.FromService(param.Name, typeName));
      }
      else
      {
        bool isOptional = param.IsOptional || param.NullableAnnotation == NullableAnnotation.Annotated;
        string? defaultValue = param.HasExplicitDefaultValue ? param.ExplicitDefaultValue?.ToString() : null;

        parameters.Add(ParameterBinding.FromParameter(
          parameterName: param.Name,
          typeName: typeName,
          segmentName: param.Name.ToLowerInvariant(),
          isOptional: isOptional,
          defaultValue: defaultValue,
          requiresConversion: typeName != "global::System.String"));
      }
    }

    HandlerReturnType returnType = GetReturnType(methodSymbol);
    bool isAsync = methodSymbol.IsAsync || returnType.IsTask;

    return new HandlerDefinition(
      HandlerKind: HandlerKind.Delegate,
      FullTypeName: null,
      MethodName: null,
      LambdaBodySource: null,  // Will be set by caller if lambda
      IsExpressionBody: true,
      Parameters: parameters.ToImmutable(),
      ReturnType: returnType,
      IsAsync: isAsync,
      RequiresCancellationToken: hasCancellationToken,
      RequiresServiceProvider: requiresServiceProvider);
  }

  /// <summary>
  /// Extracts handler information from a resolved method symbol (for method references).
  /// </summary>
  private static HandlerDefinition ExtractFromMethodSymbolAsMethod(IMethodSymbol methodSymbol)
  {
    ImmutableArray<ParameterBinding>.Builder parameters = ImmutableArray.CreateBuilder<ParameterBinding>();
    bool hasCancellationToken = false;
    bool requiresServiceProvider = false;

    foreach (IParameterSymbol param in methodSymbol.Parameters)
    {
      string typeName = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

      if (IsCancellationTokenType(typeName))
      {
        hasCancellationToken = true;
        parameters.Add(ParameterBinding.ForCancellationToken(param.Name));
      }
      else if (IsServiceType(typeName))
      {
        requiresServiceProvider = true;
        parameters.Add(ParameterBinding.FromService(param.Name, typeName));
      }
      else
      {
        bool isOptional = param.IsOptional || param.NullableAnnotation == NullableAnnotation.Annotated;
        string? defaultValue = param.HasExplicitDefaultValue ? param.ExplicitDefaultValue?.ToString() : null;

        parameters.Add(ParameterBinding.FromParameter(
          parameterName: param.Name,
          typeName: typeName,
          segmentName: param.Name.ToLowerInvariant(),
          isOptional: isOptional,
          defaultValue: defaultValue,
          requiresConversion: typeName != "global::System.String"));
      }
    }

    HandlerReturnType returnType = GetReturnType(methodSymbol);
    bool isAsync = methodSymbol.IsAsync || returnType.IsTask;

    string fullTypeName = methodSymbol.ContainingType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
      ?? "global::System.Object";

    return HandlerDefinition.ForMethod(
      fullTypeName: fullTypeName,
      methodName: methodSymbol.Name,
      parameters: parameters.ToImmutable(),
      returnType: returnType,
      isAsync: isAsync,
      requiresCancellationToken: hasCancellationToken,
      requiresServiceProvider: requiresServiceProvider);
  }

  /// <summary>
  /// Gets the return type from a method symbol.
  /// </summary>
  private static HandlerReturnType GetReturnType(IMethodSymbol methodSymbol)
  {
    ITypeSymbol returnType = methodSymbol.ReturnType;
    string fullTypeName = returnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    string shortTypeName = returnType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

    if (returnType.SpecialType == SpecialType.System_Void)
      return HandlerReturnType.Void;

    if (fullTypeName == "global::System.Threading.Tasks.Task")
      return HandlerReturnType.Task;

    if (fullTypeName.StartsWith("global::System.Threading.Tasks.Task<", StringComparison.Ordinal) ||
        fullTypeName.StartsWith("global::System.Threading.Tasks.ValueTask<", StringComparison.Ordinal))
    {
      if (returnType is INamedTypeSymbol namedType && namedType.TypeArguments.Length == 1)
      {
        string innerType = namedType.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string innerShort = namedType.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        return HandlerReturnType.TaskOf(innerType, innerShort);
      }

      return HandlerReturnType.Task;
    }

    return HandlerReturnType.Of(fullTypeName, shortTypeName);
  }

  /// <summary>
  /// Infers the return type from a lambda body.
  /// </summary>
  private static HandlerReturnType InferReturnType
  (
    CSharpSyntaxNode body,
    bool isAsync,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    // For expression bodies, try to get the type
    if (body is ExpressionSyntax expression)
    {
      TypeInfo typeInfo = semanticModel.GetTypeInfo(expression, cancellationToken);
      if (typeInfo.Type is not null && typeInfo.Type.SpecialType != SpecialType.System_Void)
      {
        string fullTypeName = typeInfo.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string shortTypeName = typeInfo.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        if (isAsync)
          return HandlerReturnType.TaskOf(fullTypeName, shortTypeName);

        return HandlerReturnType.Of(fullTypeName, shortTypeName);
      }
    }

    // For block bodies, find return statements and infer type from the returned expression
    if (body is BlockSyntax block)
    {
      // Find the first return statement that has an expression
      ReturnStatementSyntax? returnStatement = block
        .DescendantNodes()
        .OfType<ReturnStatementSyntax>()
        .FirstOrDefault(r => r.Expression is not null);

      if (returnStatement?.Expression is not null)
      {
        TypeInfo typeInfo = semanticModel.GetTypeInfo(returnStatement.Expression, cancellationToken);
        if (typeInfo.Type is not null && typeInfo.Type.SpecialType != SpecialType.System_Void)
        {
          string fullTypeName = typeInfo.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
          string shortTypeName = typeInfo.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

          if (isAsync)
            return HandlerReturnType.TaskOf(fullTypeName, shortTypeName);

          return HandlerReturnType.Of(fullTypeName, shortTypeName);
        }
      }
    }

    return isAsync ? HandlerReturnType.Task : HandlerReturnType.Void;
  }

  /// <summary>
  /// Creates a default handler definition when extraction fails.
  /// </summary>
  private static HandlerDefinition CreateDefaultHandler()
  {
    return HandlerDefinition.ForDelegate(
      parameters: [],
      returnType: HandlerReturnType.Void,
      isAsync: false);
  }

  /// <summary>
  /// Checks if a type name represents CancellationToken.
  /// </summary>
  private static bool IsCancellationTokenType(string typeName)
  {
    return typeName == "global::System.Threading.CancellationToken" ||
           typeName == "CancellationToken" ||
           typeName == "System.Threading.CancellationToken";
  }

  /// <summary>
  /// Checks if a type name appears to be a service (interface or known service types).
  /// </summary>
  private static bool IsServiceType(string typeName)
  {
    // Common service patterns
    if (typeName.StartsWith("global::Microsoft.Extensions.", StringComparison.Ordinal))
      return true;

    if (typeName.Contains("ILogger", StringComparison.Ordinal))
      return true;

    if (typeName.Contains("IServiceProvider", StringComparison.Ordinal))
      return true;

    // Check if it's an interface (starts with I followed by uppercase letter)
    // This handles user-defined service interfaces like IGreeter, IFormatter, etc.
    string shortName = GetShortTypeName(typeName);
    if (shortName.Length >= 2 && shortName[0] == 'I' && char.IsUpper(shortName[1]))
      return true;

    return false;
  }

  /// <summary>
  /// Gets the short type name from a fully qualified type name.
  /// </summary>
  private static string GetShortTypeName(string typeName)
  {
    // Remove global:: prefix
    if (typeName.StartsWith("global::", StringComparison.Ordinal))
      typeName = typeName[8..];

    // Get last segment after the final dot
    int lastDot = typeName.LastIndexOf('.');
    return lastDot >= 0 ? typeName[(lastDot + 1)..] : typeName;
  }

  /// <summary>
  /// Normalizes a type name to fully qualified form.
  /// </summary>
  private static string NormalizeTypeName(string typeName)
  {
    return typeName switch
    {
      "int" => "global::System.Int32",
      "long" => "global::System.Int64",
      "short" => "global::System.Int16",
      "byte" => "global::System.Byte",
      "float" => "global::System.Single",
      "double" => "global::System.Double",
      "decimal" => "global::System.Decimal",
      "bool" => "global::System.Boolean",
      "char" => "global::System.Char",
      "string" => "global::System.String",
      "object" => "global::System.Object",
      "void" => "void",
      _ when typeName.StartsWith("global::", StringComparison.Ordinal) => typeName,
      _ => $"global::{typeName}"
    };
  }
}
