#region Purpose
// Handler extraction from method symbols and return type inference (HandlerExtractor partial).
#endregion

namespace TimeWarp.Nuru.Generators;

internal static partial class HandlerExtractor
{
  /// <summary>
  /// Extracts handler information from a resolved method symbol (for delegates).
  /// </summary>
  private static HandlerDefinition ExtractFromMethodSymbol(IMethodSymbol methodSymbol, Compilation? compilation = null)
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
      else if (IsServiceType(param.Type))
      {
        requiresServiceProvider = true;
        parameters.Add(CreateServiceBinding(param, typeName, compilation));
      }
      else
      {
        bool isOptional = param.IsOptional || param.NullableAnnotation == NullableAnnotation.Annotated;
        string? defaultValue = param.HasExplicitDefaultValue ? param.ExplicitDefaultValue?.ToString() : null;
        bool isEnumType = IsEnumBindableType(param.Type);

        parameters.Add(ParameterBinding.FromParameter(
          parameterName: param.Name,
          typeName: typeName,
          segmentName: param.Name.ToLowerInvariant(),
          isOptional: isOptional,
          defaultValue: defaultValue,
          requiresConversion: typeName != "global::System.String",
          isEnumType: isEnumType));
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
      RequiresServiceProvider: requiresServiceProvider,
      HasAsyncModifier: methodSymbol.IsAsync);
  }

  /// <summary>
  /// Extracts handler information from a resolved method symbol (for method references).
  /// </summary>
  private static HandlerDefinition ExtractFromMethodSymbolAsMethod(IMethodSymbol methodSymbol, Compilation? compilation = null)
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
      else if (IsServiceType(param.Type))
      {
        requiresServiceProvider = true;
        parameters.Add(CreateServiceBinding(param, typeName, compilation));
      }
      else
      {
        bool isOptional = param.IsOptional || param.NullableAnnotation == NullableAnnotation.Annotated;
        string? defaultValue = param.HasExplicitDefaultValue ? param.ExplicitDefaultValue?.ToString() : null;
        bool isEnumType = IsEnumBindableType(param.Type);

        parameters.Add(ParameterBinding.FromParameter(
          parameterName: param.Name,
          typeName: typeName,
          segmentName: param.Name.ToLowerInvariant(),
          isOptional: isOptional,
          defaultValue: defaultValue,
          requiresConversion: typeName != "global::System.String",
          isEnumType: isEnumType));
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

    if (fullTypeName is "global::System.Threading.Tasks.Task"
        or "global::System.Threading.Tasks.ValueTask")
    {
      return HandlerReturnType.Task;
    }

    if (fullTypeName.StartsWith("global::System.Threading.Tasks.Task<", StringComparison.Ordinal) ||
        fullTypeName.StartsWith("global::System.Threading.Tasks.ValueTask<", StringComparison.Ordinal))
    {
      if (returnType is INamedTypeSymbol namedType && namedType.TypeArguments.Length == 1)
      {
        string innerType = namedType.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string innerShort = namedType.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        return HandlerReturnType.TaskOf(innerType, innerShort, ResultValueKinds.Of(namedType.TypeArguments[0]));
      }

      return HandlerReturnType.Task;
    }

    return HandlerReturnType.Of(fullTypeName, shortTypeName, ResultValueKinds.Of(returnType));
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

        ResultValueKind valueKind = ResultValueKinds.Of(typeInfo.Type);
        if (isAsync)
          return HandlerReturnType.TaskOf(fullTypeName, shortTypeName, valueKind);

        return HandlerReturnType.Of(fullTypeName, shortTypeName, valueKind);
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

          ResultValueKind valueKind = ResultValueKinds.Of(typeInfo.Type);
          if (isAsync)
            return HandlerReturnType.TaskOf(fullTypeName, shortTypeName, valueKind);

          return HandlerReturnType.Of(fullTypeName, shortTypeName, valueKind);
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
}
