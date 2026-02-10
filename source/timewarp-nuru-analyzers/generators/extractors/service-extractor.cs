// Extracts service registration information from ConfigureServices() calls.
//
// Handles:
// - .ConfigureServices(services => { ... })           - inline lambda
// - .ConfigureServices(ConfigureServices)             - method group reference
// - Services registered via AddTransient, AddScoped, AddSingleton
//
// Also detects:
// - Factory delegate registrations (NURU053)
// - Constructor dependencies of implementation types (NURU051)
// - Internal types (NURU054)
// - Extension method calls like AddLogging (NURU052)

namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Extracts service registration information from ConfigureServices() calls.
/// </summary>
internal static class ServiceExtractor
{
  /// <summary>
  /// Standard service registration methods that we can fully analyze.
  /// Other method calls are tracked as extension methods.
  /// </summary>
  private static readonly HashSet<string> AnalyzableMethods =
    ["AddTransient", "AddScoped", "AddSingleton"];

  /// <summary>
  /// Extracts service definitions from a ConfigureServices() invocation.
  /// </summary>
  /// <param name="configureServicesInvocation">The .ConfigureServices(...) invocation.</param>
  /// <param name="semanticModel">Semantic model for type resolution.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Extraction result with services and detected extension methods.</returns>
  public static ServiceExtractionResult Extract
  (
    InvocationExpressionSyntax configureServicesInvocation,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    ArgumentListSyntax? args = configureServicesInvocation.ArgumentList;
    if (args is null || args.Arguments.Count == 0)
      return ServiceExtractionResult.Empty;

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

    return ServiceExtractionResult.Empty;
  }

  /// <summary>
  /// Extracts services from a lambda expression.
  /// </summary>
  private static ServiceExtractionResult ExtractFromLambda
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
  private static ServiceExtractionResult ExtractFromMethodGroup
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
      return ServiceExtractionResult.Empty;

    // Get the method's syntax declaration
    SyntaxReference? syntaxRef = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault();
    if (syntaxRef is null)
      return ServiceExtractionResult.Empty;

    Microsoft.CodeAnalysis.SyntaxNode? methodSyntax = syntaxRef.GetSyntax(cancellationToken);

    // Extract the body based on method syntax type
    CSharpSyntaxNode? body = methodSyntax switch
    {
      MethodDeclarationSyntax method => (CSharpSyntaxNode?)method.Body ?? method.ExpressionBody?.Expression,
      LocalFunctionStatementSyntax localFunc => (CSharpSyntaxNode?)localFunc.Body ?? localFunc.ExpressionBody?.Expression,
      _ => null
    };

    if (body is null)
      return ServiceExtractionResult.Empty;

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
  private static ServiceExtractionResult ExtractFromBody
  (
    CSharpSyntaxNode body,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    ImmutableArray<ServiceDefinition>.Builder services = ImmutableArray.CreateBuilder<ServiceDefinition>();
    ImmutableArray<ExtensionMethodCall>.Builder extensionMethods = ImmutableArray.CreateBuilder<ExtensionMethodCall>();

    // Handle expression body
    if (body is ExpressionSyntax expression)
    {
      (ServiceDefinition? service, ExtensionMethodCall? extMethod) = ExtractFromExpression(expression, semanticModel, cancellationToken);
      if (service is not null)
        services.Add(service);
      if (extMethod is not null)
        extensionMethods.Add(extMethod);

      return new ServiceExtractionResult(services.ToImmutable(), extensionMethods.ToImmutable());
    }

    // Handle block body
    if (body is BlockSyntax block)
    {
      foreach (StatementSyntax statement in block.Statements)
      {
        if (statement is ExpressionStatementSyntax expressionStatement)
        {
          (ServiceDefinition? service, ExtensionMethodCall? extMethod) = ExtractFromExpression(expressionStatement.Expression, semanticModel, cancellationToken);
          if (service is not null)
            services.Add(service);
          if (extMethod is not null)
            extensionMethods.Add(extMethod);
        }
      }
    }

    return new ServiceExtractionResult(services.ToImmutable(), extensionMethods.ToImmutable());
  }

  /// <summary>
  /// Extracts a service definition from an expression.
  /// Returns either a ServiceDefinition (for analyzable registrations) or
  /// an ExtensionMethodCall (for opaque extension methods).
  /// </summary>
  private static (ServiceDefinition? Service, ExtensionMethodCall? ExtensionMethod) ExtractFromExpression
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

    return (null, null);
  }

  /// <summary>
  /// Extracts a service definition from an invocation expression.
  /// Returns either a ServiceDefinition (for analyzable registrations) or
  /// an ExtensionMethodCall (for opaque extension methods).
  /// </summary>
  private static (ServiceDefinition? Service, ExtensionMethodCall? ExtensionMethod) ExtractFromInvocation
  (
    InvocationExpressionSyntax invocation,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    string? methodName = GetMethodName(invocation);
    if (methodName is null)
      return (null, null);

    // Check if this is an extension method we can't analyze
    if (!AnalyzableMethods.Contains(methodName))
    {
      // Track as extension method call for NURU052 warning
      return (null, new ExtensionMethodCall(methodName, invocation.GetLocation()));
    }

    // Determine lifetime from method name
    ServiceLifetime lifetime = methodName switch
    {
      "AddTransient" => ServiceLifetime.Transient,
      "AddScoped" => ServiceLifetime.Scoped,
      "AddSingleton" => ServiceLifetime.Singleton,
      _ => ServiceLifetime.Transient // Should not happen due to AnalyzableMethods check
    };

    // Check for factory delegate registration (NURU053)
    bool isFactoryRegistration = IsFactoryRegistration(invocation);

    // Get type arguments or arguments
    (string? serviceTypeName, string? implementationTypeName, INamedTypeSymbol? implementationSymbol) =
      ExtractServiceTypesWithSymbol(invocation, semanticModel, cancellationToken);

    if (serviceTypeName is null)
      return (null, null);

    // Extract constructor dependencies (NURU051)
    ImmutableArray<string> constructorDeps = [];
    bool isInternalType = false;

    if (implementationSymbol is not null && !isFactoryRegistration)
    {
      constructorDeps = ExtractConstructorDependencies(implementationSymbol);
      isInternalType = IsInternalType(implementationSymbol);
    }

    ServiceDefinition service = new(
      ServiceTypeName: serviceTypeName,
      ImplementationTypeName: implementationTypeName ?? serviceTypeName,
      Lifetime: lifetime,
      ConstructorDependencyTypes: constructorDeps,
      IsFactoryRegistration: isFactoryRegistration,
      IsInternalType: isInternalType,
      RegistrationLocation: invocation.GetLocation());

    return (service, null);
  }

  /// <summary>
  /// Detects if an invocation uses a factory delegate.
  /// Factory delegates: services.AddSingleton&lt;IFoo&gt;(sp => new Foo(...))
  /// </summary>
  private static bool IsFactoryRegistration(InvocationExpressionSyntax invocation)
  {
    ArgumentListSyntax? args = invocation.ArgumentList;
    if (args?.Arguments.Count > 0)
    {
      foreach (ArgumentSyntax arg in args.Arguments)
      {
        if (arg.Expression is LambdaExpressionSyntax or AnonymousFunctionExpressionSyntax)
          return true;
      }
    }

    return false;
  }

  /// <summary>
  /// Extracts constructor dependencies from an implementation type.
  /// </summary>
  private static ImmutableArray<string> ExtractConstructorDependencies(INamedTypeSymbol implementationType)
  {
    // Find the first public non-static constructor
    IMethodSymbol? constructor = implementationType.InstanceConstructors
      .FirstOrDefault(c => c.DeclaredAccessibility == Accessibility.Public && !c.IsImplicitlyDeclared);

    if (constructor is null || constructor.Parameters.Length == 0)
      return [];

    return [.. constructor.Parameters
      .Select(p => p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))];
  }

  /// <summary>
  /// Checks if an implementation type is internal (not accessible from generated code).
  /// </summary>
  private static bool IsInternalType(INamedTypeSymbol type)
  {
    // Check the type and all containing types
    INamedTypeSymbol? current = type;
    while (current is not null)
    {
      if (current.DeclaredAccessibility is Accessibility.Internal
          or Accessibility.Private
          or Accessibility.ProtectedAndInternal)
        return true;
      current = current.ContainingType;
    }

    return false;
  }

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
    TypeInfo serviceTypeInfo = semanticModel.GetTypeInfo(serviceTypeSyntax, cancellationToken);

    string? serviceType = serviceTypeInfo.Type?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    if (serviceType is null)
      return (null, null, null);

    // Get implementation type from second type argument (if present)
    string? implType = null;
    INamedTypeSymbol? implSymbol = null;

    if (typeArgs.Arguments.Count > 1)
    {
      TypeSyntax implTypeSyntax = typeArgs.Arguments[1];
      TypeInfo implTypeInfo = semanticModel.GetTypeInfo(implTypeSyntax, cancellationToken);
      implType = implTypeInfo.Type?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
      implSymbol = implTypeInfo.Type as INamedTypeSymbol;
    }
    else
    {
      // Single type argument: service and impl are the same
      implSymbol = serviceTypeInfo.Type as INamedTypeSymbol;
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
  /// <returns>Extraction result with all services and extension methods.</returns>
  public static ServiceExtractionResult ExtractFromChain
  (
    IEnumerable<InvocationExpressionSyntax> builderChain,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    ServiceExtractionResult result = ServiceExtractionResult.Empty;

    foreach (InvocationExpressionSyntax invocation in builderChain)
    {
      string? methodName = GetMethodName(invocation);
      if (methodName == "ConfigureServices")
      {
        ServiceExtractionResult extracted = Extract(invocation, semanticModel, cancellationToken);
        result = result.Merge(extracted);
      }
    }

    return result;
  }

  /// <summary>
  /// Extracts logging configuration from a ConfigureServices() invocation.
  /// Looks for AddLogging(...) calls and captures the lambda body.
  /// </summary>
  /// <param name="configureServicesInvocation">The .ConfigureServices(...) invocation.</param>
  /// <param name="semanticModel">Semantic model for type resolution.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>LoggingConfiguration if AddLogging() is found, otherwise null.</returns>
  public static LoggingConfiguration? ExtractLoggingConfiguration
  (
    InvocationExpressionSyntax configureServicesInvocation,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    ArgumentListSyntax? args = configureServicesInvocation.ArgumentList;
    if (args is null || args.Arguments.Count == 0)
      return null;

    ExpressionSyntax configureExpression = args.Arguments[0].Expression;

    // Handle lambda expressions
    if (configureExpression is LambdaExpressionSyntax lambda)
    {
      return ExtractLoggingFromLambdaBody(lambda.Body, semanticModel, cancellationToken);
    }

    // Handle method group references
    if (configureExpression is IdentifierNameSyntax or MemberAccessExpressionSyntax)
    {
      return ExtractLoggingFromMethodGroup(configureExpression, semanticModel, cancellationToken);
    }

    return null;
  }

  /// <summary>
  /// Extracts logging configuration from a lambda body.
  /// </summary>
  private static LoggingConfiguration? ExtractLoggingFromLambdaBody
  (
    CSharpSyntaxNode body,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    // Find AddLogging invocations in the body
    IEnumerable<InvocationExpressionSyntax> invocations = body.DescendantNodesAndSelf()
      .OfType<InvocationExpressionSyntax>();

    foreach (InvocationExpressionSyntax invocation in invocations)
    {
      string? methodName = GetMethodName(invocation);
      if (methodName == "AddLogging")
      {
        return ExtractLoggingLambdaBody(invocation);
      }
    }

    return null;
  }

  /// <summary>
  /// Extracts logging configuration from a method group reference.
  /// </summary>
  private static LoggingConfiguration? ExtractLoggingFromMethodGroup
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
      return null;

    // Get the method's syntax declaration
    SyntaxReference? syntaxRef = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault();
    if (syntaxRef is null)
      return null;

    Microsoft.CodeAnalysis.SyntaxNode? methodSyntax = syntaxRef.GetSyntax(cancellationToken);

    // Extract the body based on method syntax type
    CSharpSyntaxNode? methodBody = methodSyntax switch
    {
      MethodDeclarationSyntax method => (CSharpSyntaxNode?)method.Body ?? method.ExpressionBody?.Expression,
      LocalFunctionStatementSyntax localFunc => (CSharpSyntaxNode?)localFunc.Body ?? localFunc.ExpressionBody?.Expression,
      _ => null
    };

    if (methodBody is null)
      return null;

    // Get semantic model for the method's syntax tree (may be different from current)
    SemanticModel methodSemanticModel = methodSyntax.SyntaxTree == semanticModel.SyntaxTree
      ? semanticModel
      : semanticModel.Compilation.GetSemanticModel(methodSyntax.SyntaxTree);

    return ExtractLoggingFromLambdaBody(methodBody, methodSemanticModel, cancellationToken);
  }

  /// <summary>
  /// Extracts the lambda body text from an AddLogging() invocation.
  /// </summary>
  private static LoggingConfiguration? ExtractLoggingLambdaBody(InvocationExpressionSyntax addLoggingInvocation)
  {
    ArgumentListSyntax? args = addLoggingInvocation.ArgumentList;
    if (args is null || args.Arguments.Count == 0)
      return null;

    ExpressionSyntax firstArg = args.Arguments[0].Expression;

    // Handle lambda: AddLogging(builder => builder.AddConsole())
    if (firstArg is LambdaExpressionSyntax lambda)
    {
      // Get the lambda body as text
      string bodyText = lambda.Body.ToFullString().Trim();

      // Extract the lambda parameter name (e.g., "b" from "b => b.AddConsole()")
      string parameterName = lambda switch
      {
        SimpleLambdaExpressionSyntax simple => simple.Parameter.Identifier.Text,
        ParenthesizedLambdaExpressionSyntax paren when paren.ParameterList.Parameters.Count == 1
          => paren.ParameterList.Parameters[0].Identifier.Text,
        _ => "builder"
      };

      return new LoggingConfiguration(bodyText, parameterName);
    }

    // Handle method group: AddLogging(ConfigureLogging)
    // For now, we don't support method groups in AddLogging - they would need more complex resolution
    // The user would need to inline the configuration

    return null;
  }
}
