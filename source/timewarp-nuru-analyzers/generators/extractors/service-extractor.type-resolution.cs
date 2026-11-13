#region Purpose
// ServiceExtractor partial: service and implementation type resolution from registration invocations.
#endregion

namespace TimeWarp.Nuru.Generators;

internal static partial class ServiceExtractor
{
  /// <summary>
  /// Checks if an implementation type is not accessible from generated code. The generated
  /// code compiles into the same assembly, so an internal type declared in this compilation
  /// (or exposed by [InternalsVisibleTo]) is accessible; a private nested type or an internal
  /// type from another assembly is not. Same rule as ExtensionMethodLowerer (kanban task 560).
  /// </summary>
  private static bool IsInternalType(INamedTypeSymbol type, Compilation compilation) =>
    !compilation.IsSymbolAccessibleWithin(type, compilation.Assembly);

  /// <summary>
  /// Extracts service and implementation type names and symbol from an invocation.
  /// The symbol is needed to extract constructor dependencies and check accessibility.
  /// Falls back to syntactic extraction if semantic resolution fails.
  /// </summary>
  internal static (string? ServiceType, string? ImplementationType, INamedTypeSymbol? ImplementationSymbol) GetServiceTypesWithSymbol
  (
    InvocationExpressionSyntax invocation,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  ) => ExtractServiceTypesWithSymbol(invocation, semanticModel, cancellationToken);

  /// <summary>
  /// Extracts service and implementation type names and symbol from an invocation.
  /// The symbol is needed to extract constructor dependencies and check accessibility.
  /// Falls back to syntactic extraction if semantic resolution fails.
  /// </summary>
  private static (string? ServiceType, string? ImplementationType, INamedTypeSymbol? ImplementationSymbol) ExtractServiceTypesWithSymbol
  (
    InvocationExpressionSyntax invocation,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(invocation, cancellationToken);

    if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
    {
      // Handle generic method: AddTransient<TService, TImplementation>()
      if (methodSymbol.IsGenericMethod && methodSymbol.TypeArguments.Length >= 1)
      {
        ITypeSymbol serviceTypeSymbol = methodSymbol.TypeArguments[0];
        string serviceType = serviceTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        INamedTypeSymbol? implSymbol = null;
        string? implType = null;

        if (methodSymbol.TypeArguments.Length > 1)
        {
          ITypeSymbol implTypeSymbol = methodSymbol.TypeArguments[1];
          implType = implTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
          implSymbol = implTypeSymbol as INamedTypeSymbol;
        }
        else
        {
          // Single type argument: AddTransient<Foo>() - service and impl are same
          implSymbol = serviceTypeSymbol as INamedTypeSymbol;
        }

        return (serviceType, implType, implSymbol);
      }

      // Handle non-generic: AddTransient(typeof(IFoo), typeof(Foo))
      ArgumentListSyntax? args = invocation.ArgumentList;
      if (args?.Arguments.Count >= 1)
      {
        (string? serviceType, INamedTypeSymbol? _) = ExtractTypeOfArgumentWithSymbol(args.Arguments[0].Expression, semanticModel, cancellationToken);
        (string? implType, INamedTypeSymbol? implSymbol) = args.Arguments.Count > 1
          ? ExtractTypeOfArgumentWithSymbol(args.Arguments[1].Expression, semanticModel, cancellationToken)
          : (null, null);

        return (serviceType, implType, implSymbol);
      }
    }

    // Fallback: syntactic extraction when semantic resolution fails
    // This handles cases where extension methods can't be resolved (e.g., missing using directives)
    return ExtractServiceTypesSyntactically(invocation, semanticModel, cancellationToken);
  }

  /// <summary>
  /// Extracts service types syntactically from generic type arguments.
  /// Used as fallback when semantic model can't resolve extension methods.
  /// </summary>
  private static (string? ServiceType, string? ImplementationType, INamedTypeSymbol? ImplementationSymbol) ExtractServiceTypesSyntactically
  (
    InvocationExpressionSyntax invocation,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    // Get the generic name from the member access: s.AddSingleton<IFoo, Foo>()
    GenericNameSyntax? genericName = invocation.Expression switch
    {
      MemberAccessExpressionSyntax memberAccess => memberAccess.Name as GenericNameSyntax,
      GenericNameSyntax g => g,
      _ => null
    };

    if (genericName?.TypeArgumentList.Arguments.Count is null or 0)
      return (null, null, null);

    TypeArgumentListSyntax typeArgs = genericName.TypeArgumentList;

    // Get service type from first type argument
    TypeSyntax serviceTypeSyntax = typeArgs.Arguments[0];
    ITypeSymbol? serviceTypeSymbol = TypeSyntaxResolver.ResolveType(semanticModel, serviceTypeSyntax, cancellationToken);

    string? serviceType = serviceTypeSymbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    if (serviceType is null)
      return (null, null, null);

    // Get implementation type from second type argument (if present)
    string? implType = null;
    INamedTypeSymbol? implSymbol = null;

    if (typeArgs.Arguments.Count > 1)
    {
      TypeSyntax implTypeSyntax = typeArgs.Arguments[1];
      ITypeSymbol? implTypeSymbol = TypeSyntaxResolver.ResolveType(semanticModel, implTypeSyntax, cancellationToken);
      implType = implTypeSymbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
      implSymbol = implTypeSymbol as INamedTypeSymbol;
    }
    else
    {
      // Single type argument: service and impl are the same
      implSymbol = serviceTypeSymbol as INamedTypeSymbol;
    }

    return (serviceType, implType, implSymbol);
  }

  /// <summary>
  /// Extracts service and implementation type names from an invocation.
  /// </summary>
  private static (string? ServiceType, string? ImplementationType) ExtractServiceTypes
  (
    InvocationExpressionSyntax invocation,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    (string? serviceType, string? implType, INamedTypeSymbol? _) = ExtractServiceTypesWithSymbol(invocation, semanticModel, cancellationToken);
    return (serviceType, implType);
  }

  /// <summary>
  /// Extracts a type name and symbol from a typeof() expression.
  /// </summary>
  private static (string? TypeName, INamedTypeSymbol? TypeSymbol) ExtractTypeOfArgumentWithSymbol
  (
    ExpressionSyntax expression,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    if (expression is TypeOfExpressionSyntax typeOfExpression)
    {
      TypeInfo typeInfo = semanticModel.GetTypeInfo(typeOfExpression.Type, cancellationToken);
      if (typeInfo.Type is not null)
      {
        return (
          typeInfo.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
          typeInfo.Type as INamedTypeSymbol);
      }
    }

    return (null, null);
  }

  /// <summary>
  /// Extracts a type name from a typeof() expression.
  /// </summary>
  private static string? ExtractTypeOfArgument
  (
    ExpressionSyntax expression,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    (string? typeName, INamedTypeSymbol? _) = ExtractTypeOfArgumentWithSymbol(expression, semanticModel, cancellationToken);
    return typeName;
  }

  /// <summary>
  /// Gets the method name from an invocation expression.
  /// </summary>
  internal static string? GetInvocationMethodName(InvocationExpressionSyntax invocation)
    => GetMethodName(invocation);

  /// <summary>
  /// Gets the method name from an invocation expression.
  /// </summary>
  private static string? GetMethodName(InvocationExpressionSyntax invocation)
  {
    return invocation.Expression switch
    {
      MemberAccessExpressionSyntax memberAccess => memberAccess.Name switch
      {
        GenericNameSyntax generic => generic.Identifier.Text,
        IdentifierNameSyntax identifier => identifier.Identifier.Text,
        _ => null
      },
      IdentifierNameSyntax identifier => identifier.Identifier.Text,
      GenericNameSyntax generic => generic.Identifier.Text,
      _ => null
    };
  }
}
