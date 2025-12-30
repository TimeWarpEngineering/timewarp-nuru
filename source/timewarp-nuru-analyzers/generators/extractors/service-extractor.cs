// Extracts service registration information from ConfigureServices() calls.
//
// Handles:
// - .ConfigureServices(services => { ... })           - inline lambda
// - .ConfigureServices(ConfigureServices)             - method group reference
// - Services registered via AddTransient, AddScoped, AddSingleton

namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Extracts service registration information from ConfigureServices() calls.
/// </summary>
internal static class ServiceExtractor
{
  /// <summary>
  /// Extracts service definitions from a ConfigureServices() invocation.
  /// </summary>
  /// <param name="configureServicesInvocation">The .ConfigureServices(...) invocation.</param>
  /// <param name="semanticModel">Semantic model for type resolution.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Array of extracted service definitions.</returns>
  public static ImmutableArray<ServiceDefinition> Extract
  (
    InvocationExpressionSyntax configureServicesInvocation,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    ArgumentListSyntax? args = configureServicesInvocation.ArgumentList;
    if (args is null || args.Arguments.Count == 0)
      return [];

    ExpressionSyntax configureExpression = args.Arguments[0].Expression;

    // Handle lambda expressions
    if (configureExpression is LambdaExpressionSyntax lambda)
    {
      return ExtractFromLambda(lambda, semanticModel, cancellationToken);
    }

    // Handle method group references (e.g., ConfigureServices(MyMethod))
    if (configureExpression is IdentifierNameSyntax or MemberAccessExpressionSyntax)
    {
      return ExtractFromMethodGroup(configureExpression, semanticModel, cancellationToken);
    }

    return [];
  }

  /// <summary>
  /// Extracts services from a lambda expression.
  /// </summary>
  private static ImmutableArray<ServiceDefinition> ExtractFromLambda
  (
    LambdaExpressionSyntax lambda,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    return ExtractFromBody(lambda.Body, semanticModel, cancellationToken);
  }

  /// <summary>
  /// Extracts services from a method group reference (e.g., ConfigureServices(MyMethod)).
  /// Resolves the method symbol and analyzes its body.
  /// </summary>
  private static ImmutableArray<ServiceDefinition> ExtractFromMethodGroup
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
      return [];

    // Get the method's syntax declaration
    SyntaxReference? syntaxRef = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault();
    if (syntaxRef is null)
      return [];

    Microsoft.CodeAnalysis.SyntaxNode? methodSyntax = syntaxRef.GetSyntax(cancellationToken);

    // Extract the body based on method syntax type
    CSharpSyntaxNode? body = methodSyntax switch
    {
      MethodDeclarationSyntax method => (CSharpSyntaxNode?)method.Body ?? method.ExpressionBody?.Expression,
      LocalFunctionStatementSyntax localFunc => (CSharpSyntaxNode?)localFunc.Body ?? localFunc.ExpressionBody?.Expression,
      _ => null
    };

    if (body is null)
      return [];

    // Get semantic model for the method's syntax tree (may be different from current)
    SemanticModel methodSemanticModel = methodSyntax.SyntaxTree == semanticModel.SyntaxTree
      ? semanticModel
      : semanticModel.Compilation.GetSemanticModel(methodSyntax.SyntaxTree);

    return ExtractFromBody(body, methodSemanticModel, cancellationToken);
  }

  /// <summary>
  /// Extracts services from a method body (block or expression).
  /// Shared between lambda and method group extraction.
  /// </summary>
  private static ImmutableArray<ServiceDefinition> ExtractFromBody
  (
    CSharpSyntaxNode body,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    ImmutableArray<ServiceDefinition>.Builder services = ImmutableArray.CreateBuilder<ServiceDefinition>();

    // Handle expression body
    if (body is ExpressionSyntax expression)
    {
      ServiceDefinition? service = ExtractFromExpression(expression, semanticModel, cancellationToken);
      if (service is not null)
        services.Add(service);

      return services.ToImmutable();
    }

    // Handle block body
    if (body is BlockSyntax block)
    {
      foreach (StatementSyntax statement in block.Statements)
      {
        if (statement is ExpressionStatementSyntax expressionStatement)
        {
          ServiceDefinition? service = ExtractFromExpression(expressionStatement.Expression, semanticModel, cancellationToken);
          if (service is not null)
            services.Add(service);
        }
      }
    }

    return services.ToImmutable();
  }

  /// <summary>
  /// Extracts a service definition from an expression.
  /// </summary>
  private static ServiceDefinition? ExtractFromExpression
  (
    ExpressionSyntax expression,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    // Handle chained method calls by walking up to the first registration
    if (expression is InvocationExpressionSyntax invocation)
    {
      return ExtractFromInvocation(invocation, semanticModel, cancellationToken);
    }

    return null;
  }

  /// <summary>
  /// Extracts a service definition from an invocation expression.
  /// </summary>
  private static ServiceDefinition? ExtractFromInvocation
  (
    InvocationExpressionSyntax invocation,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    string? methodName = GetMethodName(invocation);
    if (methodName is null)
      return null;

    // Determine lifetime from method name
    ServiceLifetime? lifetime = methodName switch
    {
      "AddTransient" => ServiceLifetime.Transient,
      "AddScoped" => ServiceLifetime.Scoped,
      "AddSingleton" => ServiceLifetime.Singleton,
      _ => null
    };

    if (lifetime is null)
      return null;

    // Get type arguments or arguments
    (string? serviceTypeName, string? implementationTypeName) = ExtractServiceTypes(invocation, semanticModel, cancellationToken);

    if (serviceTypeName is null)
      return null;

    return new ServiceDefinition(
      ServiceTypeName: serviceTypeName,
      ImplementationTypeName: implementationTypeName ?? serviceTypeName,
      Lifetime: lifetime.Value);
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
    SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(invocation, cancellationToken);

    if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
      return (null, null);

    // Handle generic method: AddTransient<TService, TImplementation>()
    if (methodSymbol.IsGenericMethod && methodSymbol.TypeArguments.Length >= 1)
    {
      string serviceType = methodSymbol.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
      string? implType = methodSymbol.TypeArguments.Length > 1
        ? methodSymbol.TypeArguments[1].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
        : null;

      return (serviceType, implType);
    }

    // Handle non-generic: AddTransient(typeof(IFoo), typeof(Foo))
    ArgumentListSyntax? args = invocation.ArgumentList;
    if (args?.Arguments.Count >= 1)
    {
      string? serviceType = ExtractTypeOfArgument(args.Arguments[0].Expression, semanticModel, cancellationToken);
      string? implType = args.Arguments.Count > 1
        ? ExtractTypeOfArgument(args.Arguments[1].Expression, semanticModel, cancellationToken)
        : null;

      return (serviceType, implType);
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
    if (expression is TypeOfExpressionSyntax typeOfExpression)
    {
      TypeInfo typeInfo = semanticModel.GetTypeInfo(typeOfExpression.Type, cancellationToken);
      if (typeInfo.Type is not null)
      {
        return typeInfo.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
      }
    }

    return null;
  }

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

  /// <summary>
  /// Extracts all services from a builder chain, walking ConfigureServices calls.
  /// </summary>
  /// <param name="builderChain">The builder chain to analyze.</param>
  /// <param name="semanticModel">Semantic model for type resolution.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>All extracted service definitions.</returns>
  public static ImmutableArray<ServiceDefinition> ExtractFromChain
  (
    IEnumerable<InvocationExpressionSyntax> builderChain,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    ImmutableArray<ServiceDefinition>.Builder allServices = ImmutableArray.CreateBuilder<ServiceDefinition>();

    foreach (InvocationExpressionSyntax invocation in builderChain)
    {
      string? methodName = GetMethodName(invocation);
      if (methodName == "ConfigureServices")
      {
        ImmutableArray<ServiceDefinition> services = Extract(invocation, semanticModel, cancellationToken);
        allServices.AddRange(services);
      }
    }

    return allServices.ToImmutable();
  }
}
