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
        return ValueReturnType(namedType.TypeArguments[0], isTask: true);

      return HandlerReturnType.Task;
    }

    return ValueReturnType(returnType, isTask: false);
  }

  /// <summary>
  /// Builds the return type for a result value (<paramref name="valueType"/> is T for Task&lt;T&gt;).
  /// A type that is or contains an anonymous type is named <c>object</c>, because generated code
  /// cannot name it (kanban 585); its <see cref="ResultValueKind.Anonymous"/> kind prints it through ToString().
  /// </summary>
  private static HandlerReturnType ValueReturnType(ITypeSymbol valueType, bool isTask)
  {
    ResultValueKind valueKind = ResultValueKinds.Of(valueType);
    (string fullTypeName, string shortTypeName) = valueKind == ResultValueKind.Anonymous
      ? ("global::System.Object", "object")
      : (valueType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
         valueType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));

    return isTask
      ? HandlerReturnType.TaskOf(fullTypeName, shortTypeName, valueKind)
      : HandlerReturnType.Of(fullTypeName, shortTypeName, valueKind);
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
        return ValueReturnType(typeInfo.Type, isAsync);
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
          return ValueReturnType(typeInfo.Type, isAsync);
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
