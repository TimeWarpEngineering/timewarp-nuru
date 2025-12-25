namespace TimeWarp.Nuru;

/// <summary>
/// Delegate signature extraction methods for the delegate command generator.
/// </summary>
public partial class NuruDelegateCommandGenerator
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
      LambdaExpressionSyntax lambda => ExtractFromLambda(lambda, semanticModel, cancellationToken),
      IdentifierNameSyntax identifier => ExtractFromMethodGroup(identifier, semanticModel, cancellationToken),
      MemberAccessExpressionSyntax memberAccess => ExtractFromMethodGroup(memberAccess, semanticModel, cancellationToken),
      ObjectCreationExpressionSyntax creation => ExtractFromDelegateCreation(creation, semanticModel, cancellationToken),
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
