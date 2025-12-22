namespace TimeWarp.Nuru;

/// <summary>
/// Signature extraction methods for extracting delegate signatures from handler expressions
/// including lambdas, method groups, and delegate creations.
/// </summary>
public partial class NuruInvokerGenerator
{
  /// <summary>
  /// Extracts delegate signature from a handler expression.
  /// </summary>
  private static DelegateSignature? ExtractSignatureFromHandler(
    ExpressionSyntax handlerExpression,
    SemanticModel semanticModel,
    CancellationToken cancellationToken)
  {
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
    ISymbol? symbol = semanticModel.GetSymbolInfo(lambda, cancellationToken).Symbol;
    if (symbol is IMethodSymbol methodSymbol)
      return CreateSignatureFromMethod(methodSymbol);

    // Try to get the converted type for implicitly typed lambdas
    Microsoft.CodeAnalysis.TypeInfo typeInfo = semanticModel.GetTypeInfo(lambda, cancellationToken);
    if (typeInfo.ConvertedType is INamedTypeSymbol delegateType &&
        delegateType.DelegateInvokeMethod is IMethodSymbol invokeMethod)
    {
      return CreateSignatureFromMethod(invokeMethod);
    }

    return null;
  }

  private static DelegateSignature? ExtractFromMethodGroup(
    ExpressionSyntax expression,
    SemanticModel semanticModel,
    CancellationToken cancellationToken)
  {
    SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(expression, cancellationToken);

    if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
      return CreateSignatureFromMethod(methodSymbol);

    if (symbolInfo.CandidateSymbols.Length > 0 &&
        symbolInfo.CandidateSymbols[0] is IMethodSymbol candidateMethod)
    {
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

    if (typeInfo.Type is INamedTypeSymbol namedType &&
        namedType.DelegateInvokeMethod is IMethodSymbol invokeMethod)
    {
      return CreateSignatureFromMethod(invokeMethod);
    }

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
    DelegateTypeInfo returnType = DelegateTypeInfo.FromSymbol(method.ReturnType);
    bool isAsync = returnType.IsTask;
    string identifier = DelegateSignature.CreateIdentifier(paramArray, returnType);

    return new DelegateSignature(paramArray, returnType, isAsync, identifier);
  }

  private static bool IsNullableValueType(ITypeSymbol type) =>
    type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
}
