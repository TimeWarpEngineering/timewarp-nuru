namespace TimeWarp.Nuru;

/// <summary>
/// Extracts delegate signatures from Map() invocations for source generation.
/// </summary>
internal static class DelegateSignatureExtractor
{
  /// <summary>
  /// Attempts to extract a delegate signature from a Map() invocation.
  /// </summary>
  /// <param name="invocation">The Map() invocation expression</param>
  /// <param name="semanticModel">The semantic model for type resolution</param>
  /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>The extracted signature, or null if extraction fails</returns>
  public static DelegateSignature? ExtractSignature(
    InvocationExpressionSyntax invocation,
    SemanticModel semanticModel,
    CancellationToken cancellationToken)
  {
    // Map() should have at least 2 arguments: pattern and handler
    ArgumentListSyntax? argumentList = invocation.ArgumentList;
    if (argumentList is null || argumentList.Arguments.Count < 2)
      return null;

    // The second argument is the delegate/handler
    ArgumentSyntax handlerArgument = argumentList.Arguments[1];
    ExpressionSyntax handlerExpression = handlerArgument.Expression;

    // Extract type information based on the expression type
    return handlerExpression switch
    {
      // Lambda expression: (int x, string y) => ...
      LambdaExpressionSyntax lambda => ExtractFromLambda(lambda, semanticModel, cancellationToken),

      // Method group: MyHandler (referencing a method)
      IdentifierNameSyntax identifier => ExtractFromMethodGroup(identifier, semanticModel, cancellationToken),

      // Qualified method group: SomeClass.MyHandler
      MemberAccessExpressionSyntax memberAccess => ExtractFromMethodGroup(memberAccess, semanticModel, cancellationToken),

      // Delegate creation: new Action<int>(...)
      ObjectCreationExpressionSyntax creation => ExtractFromDelegateCreation(creation, semanticModel, cancellationToken),

      // Anything else - try to get the delegate type from the symbol
      _ => ExtractFromExpression(handlerExpression, semanticModel, cancellationToken)
    };
  }

  private static DelegateSignature? ExtractFromLambda(
    LambdaExpressionSyntax lambda,
    SemanticModel semanticModel,
    CancellationToken cancellationToken)
  {
    // Get the symbol for the lambda
    ISymbol? symbol = semanticModel.GetSymbolInfo(lambda, cancellationToken).Symbol;
    if (symbol is not IMethodSymbol methodSymbol)
    {
      // Try to get the converted type for implicitly typed lambdas
      Microsoft.CodeAnalysis.TypeInfo typeInfo = semanticModel.GetTypeInfo(lambda, cancellationToken);
      if (typeInfo.ConvertedType is INamedTypeSymbol delegateType &&
          delegateType.DelegateInvokeMethod is IMethodSymbol invokeMethod)
      {
        return CreateSignatureFromMethod(invokeMethod);
      }

      return null;
    }

    return CreateSignatureFromMethod(methodSymbol);
  }

  private static DelegateSignature? ExtractFromMethodGroup(
    ExpressionSyntax expression,
    SemanticModel semanticModel,
    CancellationToken cancellationToken)
  {
    // For method groups, we need to get the method symbol
    SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(expression, cancellationToken);

    if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
      return CreateSignatureFromMethod(methodSymbol);

    // If there are multiple candidates, try to find the right one
    if (symbolInfo.CandidateSymbols.Length > 0)
    {
      // For now, just use the first candidate
      // TODO: Better resolution based on expected delegate type
      if (symbolInfo.CandidateSymbols[0] is IMethodSymbol candidateMethod)
        return CreateSignatureFromMethod(candidateMethod);
    }

    return null;
  }

  private static DelegateSignature? ExtractFromDelegateCreation(
    ObjectCreationExpressionSyntax creation,
    SemanticModel semanticModel,
    CancellationToken cancellationToken)
  {
    Microsoft.CodeAnalysis.TypeInfo typeInfo = semanticModel.GetTypeInfo(creation, cancellationToken);

    if (typeInfo.Type is INamedTypeSymbol delegateType &&
        delegateType.DelegateInvokeMethod is IMethodSymbol invokeMethod)
    {
      return CreateSignatureFromMethod(invokeMethod);
    }

    return null;
  }

  private static DelegateSignature? ExtractFromExpression(
    ExpressionSyntax expression,
    SemanticModel semanticModel,
    CancellationToken cancellationToken)
  {
    Microsoft.CodeAnalysis.TypeInfo typeInfo = semanticModel.GetTypeInfo(expression, cancellationToken);

    // Try to get the delegate's invoke method
    if (typeInfo.Type is INamedTypeSymbol namedType &&
        namedType.DelegateInvokeMethod is IMethodSymbol invokeMethod)
    {
      return CreateSignatureFromMethod(invokeMethod);
    }

    // Try converted type (for implicit conversions)
    if (typeInfo.ConvertedType is INamedTypeSymbol convertedType &&
        convertedType.DelegateInvokeMethod is IMethodSymbol convertedInvokeMethod)
    {
      return CreateSignatureFromMethod(convertedInvokeMethod);
    }

    return null;
  }

  /// <summary>
  /// Creates a DelegateSignature from a method symbol.
  /// </summary>
  private static DelegateSignature CreateSignatureFromMethod(IMethodSymbol method)
  {
    // Extract parameters
    ImmutableArray<DelegateParameterInfo>.Builder parameters =
      ImmutableArray.CreateBuilder<DelegateParameterInfo>(method.Parameters.Length);

    foreach (IParameterSymbol param in method.Parameters)
    {
      ITypeSymbol paramType = param.Type;
      bool isArray = paramType is IArrayTypeSymbol;
      bool isNullable = param.NullableAnnotation == NullableAnnotation.Annotated ||
                        IsNullableValueType(paramType);

      DelegateTypeInfo typeInfo = DelegateTypeInfo.FromSymbol(paramType);

      parameters.Add(new DelegateParameterInfo(
        param.Name,
        typeInfo,
        isArray,
        isNullable));
    }

    ImmutableArray<DelegateParameterInfo> paramArray = parameters.ToImmutable();

    // Extract return type
    DelegateTypeInfo returnType = DelegateTypeInfo.FromSymbol(method.ReturnType);
    bool isAsync = returnType.IsTask;

    // Generate unique identifier
    string identifier = DelegateSignature.CreateIdentifier(paramArray, returnType);

    return new DelegateSignature(paramArray, returnType, isAsync, identifier);
  }

  private static bool IsNullableValueType(ITypeSymbol type) =>
    type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
}
