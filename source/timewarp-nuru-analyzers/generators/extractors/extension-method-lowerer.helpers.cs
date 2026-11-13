#region Purpose
// Argument checks, chain flattening and method symbol helpers (ExtensionMethodLowerer partial).
#endregion

namespace TimeWarp.Nuru.Generators;

internal static partial class ExtensionMethodLowerer
{
  private static bool HasUnlowerableArguments(InvocationExpressionSyntax invocation, string? collectionParameterName)
  {
    if (ServiceExtractor.IsFactoryDelegate(invocation))
      return true;

    ArgumentListSyntax? args = invocation.ArgumentList;
    if (args is null || args.Arguments.Count == 0)
      return false;

    foreach (ArgumentSyntax argument in args.Arguments)
    {
      if (argument.Expression is TypeOfExpressionSyntax typeOfExpression)
      {
        if (typeOfExpression.Type is GenericNameSyntax genericName && genericName.IsUnboundGenericName)
          return true;
        continue;
      }

      // ILSpy emits extension calls as static methods whose first argument is the collection.
      if (argument.Expression is IdentifierNameSyntax identifier
          && collectionParameterName is not null
          && identifier.Identifier.Text == collectionParameterName)
      {
        continue;
      }

      return true;
    }

    return false;
  }

  private static bool IsOpenGeneric(ITypeSymbol type)
  {
    if (type is ITypeParameterSymbol)
      return true;

    if (type is INamedTypeSymbol named)
    {
      if (named.IsUnboundGenericType)
        return true;

      if (named.IsGenericType)
      {
        foreach (ITypeSymbol argument in named.TypeArguments)
        {
          if (IsOpenGeneric(argument))
            return true;
        }
      }
    }

    return false;
  }

  private static bool AlreadyRegistered(List<ServiceDefinition> services, string serviceTypeName)
  {
    string normalized = NormalizeTypeName(serviceTypeName);
    foreach (ServiceDefinition service in services)
    {
      if (string.Equals(NormalizeTypeName(service.ServiceTypeName), normalized, StringComparison.Ordinal))
        return true;
    }

    return false;
  }

  private static string NormalizeTypeName(string typeName)
  {
    return typeName.StartsWith("global::", StringComparison.Ordinal) ? typeName[8..] : typeName;
  }

  internal static List<InvocationExpressionSyntax> FlattenChain(ExpressionSyntax expression)
  {
    List<InvocationExpressionSyntax> chain = [];
    ExpressionSyntax current = expression;
    while (current is InvocationExpressionSyntax invocation)
    {
      chain.Add(invocation);
      if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        current = memberAccess.Expression;
      else
        break;
    }

    chain.Reverse();
    return chain;
  }

  private static bool ReceiverIsCollectionParameter(InvocationExpressionSyntax innermost, string? collectionParameterName)
  {
    if (collectionParameterName is null)
      return false;

    if (innermost.Expression is MemberAccessExpressionSyntax memberAccess
        && memberAccess.Expression is IdentifierNameSyntax receiver
        && receiver.Identifier.Text == collectionParameterName)
    {
      return true;
    }

    // ILSpy emits `ServiceCollectionServiceExtensions.AddSingleton<T>(services)` rather than
    // `services.AddSingleton<T>()`.
    if (innermost.ArgumentList.Arguments.Count > 0
        && innermost.ArgumentList.Arguments[0].Expression is IdentifierNameSyntax firstArgument
        && firstArgument.Identifier.Text == collectionParameterName)
    {
      return true;
    }

    return false;
  }

  private static IMethodSymbol? GetMethodSymbol
  (
    InvocationExpressionSyntax invocation,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(invocation, cancellationToken);
    return symbolInfo.Symbol as IMethodSymbol
      ?? symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();
  }

  private static ImmutableDictionary<string, ITypeSymbol> MapTypeArguments
  (
    IMethodSymbol identity,
    IMethodSymbol constructed,
    ImmutableDictionary<string, ITypeSymbol> parentTypeArgs
  )
  {
    ImmutableDictionary<string, ITypeSymbol>.Builder builder =
      ImmutableDictionary.CreateBuilder<string, ITypeSymbol>(StringComparer.Ordinal);

    // Prefer the call-site constructed method: ReducedFrom is the unbound definition
    // whose TypeArguments are still type parameters.
    ImmutableArray<ITypeSymbol> typeArguments = constructed.TypeArguments;
    if (typeArguments.IsDefaultOrEmpty && constructed.ReducedFrom is not null)
      typeArguments = constructed.ReducedFrom.TypeArguments;

    for (int i = 0; i < identity.TypeParameters.Length && i < typeArguments.Length; i++)
    {
      ITypeSymbol argument = SubstituteType(typeArguments[i], parentTypeArgs);
      builder[identity.TypeParameters[i].Name] = argument;
    }

    return builder.ToImmutable();
  }

  private static string? GetCollectionParameterName(IMethodSymbol methodSymbol)
  {
    foreach (IParameterSymbol parameter in methodSymbol.Parameters)
    {
      if (IsServiceCollection(parameter.Type))
        return parameter.Name;
    }

    return methodSymbol.Parameters.Length > 0 ? methodSymbol.Parameters[0].Name : null;
  }

  private static bool IsVoidOrServiceCollection(ITypeSymbol type)
  {
    return type.SpecialType == SpecialType.System_Void || IsServiceCollection(type);
  }

  private static bool IsServiceCollection(ITypeSymbol type)
  {
    return type.Name == "IServiceCollection"
      && type.ContainingNamespace.ToDisplayString() == "Microsoft.Extensions.DependencyInjection";
  }
}
