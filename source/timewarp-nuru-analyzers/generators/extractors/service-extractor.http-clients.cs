#region Purpose
// ServiceExtractor partial: AddHttpClient configuration extraction.
#endregion

namespace TimeWarp.Nuru.Generators;

internal static partial class ServiceExtractor
{
  /// <summary>
  /// Extracts HttpClient configurations from a ConfigureServices() invocation.
  /// Looks for AddHttpClient<TService, TImplementation>(...) calls and captures type info and lambda body.
  /// </summary>
  /// <param name="configureServicesInvocation">The .ConfigureServices(...) invocation.</param>
  /// <param name="semanticModel">Semantic model for type resolution.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>Array of HttpClientConfiguration for each AddHttpClient() call found.</returns>
  public static ImmutableArray<HttpClientConfiguration> ExtractHttpClientConfigurations
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
      return ExtractHttpClientsFromLambdaBody(lambda.Body, semanticModel, cancellationToken);
    }

    // Handle method group references
    if (configureExpression is IdentifierNameSyntax or MemberAccessExpressionSyntax)
    {
      return ExtractHttpClientsFromMethodGroup(configureExpression, semanticModel, cancellationToken);
    }

    return [];
  }

  /// <summary>
  /// Extracts HttpClient configurations from a lambda body.
  /// </summary>
  private static ImmutableArray<HttpClientConfiguration> ExtractHttpClientsFromLambdaBody
  (
    CSharpSyntaxNode body,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    ImmutableArray<HttpClientConfiguration>.Builder configs = ImmutableArray.CreateBuilder<HttpClientConfiguration>();

    // Find AddHttpClient invocations in the body
    IEnumerable<InvocationExpressionSyntax> invocations = body.DescendantNodesAndSelf()
      .OfType<InvocationExpressionSyntax>();

    foreach (InvocationExpressionSyntax invocation in invocations)
    {
      string? methodName = GetMethodName(invocation);
      if (methodName == "AddHttpClient")
      {
        HttpClientConfiguration? config = ExtractHttpClientConfiguration(invocation, semanticModel, cancellationToken);
        if (config is not null)
        {
          configs.Add(config);
        }
      }
    }

    return configs.ToImmutable();
  }

  /// <summary>
  /// Extracts HttpClient configurations from a method group reference.
  /// </summary>
  private static ImmutableArray<HttpClientConfiguration> ExtractHttpClientsFromMethodGroup
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
    CSharpSyntaxNode? methodBody = methodSyntax switch
    {
      MethodDeclarationSyntax method => (CSharpSyntaxNode?)method.Body ?? method.ExpressionBody?.Expression,
      LocalFunctionStatementSyntax localFunc => (CSharpSyntaxNode?)localFunc.Body ?? localFunc.ExpressionBody?.Expression,
      _ => null
    };

    if (methodBody is null)
      return [];

    // Get semantic model for the method's syntax tree (may be different from current)
    SemanticModel methodSemanticModel = methodSyntax.SyntaxTree == semanticModel.SyntaxTree
      ? semanticModel
      : semanticModel.Compilation.GetSemanticModel(methodSyntax.SyntaxTree);

    return ExtractHttpClientsFromLambdaBody(methodBody, methodSemanticModel, cancellationToken);
  }

  /// <summary>
  /// Extracts HttpClient configuration from an AddHttpClient() invocation.
  /// Handles: AddHttpClient<TService, TImplementation>(Action<HttpClient> configure)
  /// </summary>
  private static HttpClientConfiguration? ExtractHttpClientConfiguration
  (
    InvocationExpressionSyntax addHttpClientInvocation,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    // Get type arguments for typed clients (AddHttpClient<TService, TImplementation>)
    string? serviceTypeName = null;
    string? implementationTypeName = null;

    SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(addHttpClientInvocation, cancellationToken);
    if (symbolInfo.Symbol is IMethodSymbol methodSymbol && methodSymbol.IsGenericMethod)
    {
      if (methodSymbol.TypeArguments.Length >= 1)
      {
        serviceTypeName = methodSymbol.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
      }

      if (methodSymbol.TypeArguments.Length >= 2)
      {
        implementationTypeName = methodSymbol.TypeArguments[1].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
      }
    }

    // Fallback: extract syntactically if semantic resolution fails
    if (serviceTypeName is null)
    {
      (serviceTypeName, implementationTypeName) = ExtractHttpClientTypesSyntactically(addHttpClientInvocation, semanticModel, cancellationToken);
    }

    // Extract configuration lambda if present
    string? lambdaBody = null;
    string lambdaParamName = "client";

    ArgumentListSyntax? args = addHttpClientInvocation.ArgumentList;
    if (args?.Arguments.Count > 0)
    {
      ExpressionSyntax lastArg = args.Arguments[^1].Expression;

      // Handle lambda: AddHttpClient<IService, Impl>(client => { ... })
      if (lastArg is LambdaExpressionSyntax lambda)
      {
        lambdaBody = lambda.Body.ToFullString().Trim();

        // Extract the lambda parameter name
        lambdaParamName = lambda switch
        {
          SimpleLambdaExpressionSyntax simple => simple.Parameter.Identifier.Text,
          ParenthesizedLambdaExpressionSyntax paren when paren.ParameterList.Parameters.Count == 1
            => paren.ParameterList.Parameters[0].Identifier.Text,
          _ => "client"
        };
      }
    }

    // Only return configuration if we have at least a service type (typed client)
    if (serviceTypeName is null)
      return null;

    return new HttpClientConfiguration(
      ClientName: null, // Named clients not yet supported in this overload
      ServiceTypeName: serviceTypeName,
      ImplementationTypeName: implementationTypeName,
      ConfigurationLambdaBody: lambdaBody,
      LambdaParameterName: lambdaParamName
    );
  }

  /// <summary>
  /// Extracts service and implementation type names syntactically from AddHttpClient generic arguments.
  /// </summary>
  private static (string? ServiceType, string? ImplementationType) ExtractHttpClientTypesSyntactically
  (
    InvocationExpressionSyntax invocation,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    // Get the generic name from the member access: s.AddHttpClient<IFoo, Foo>()
    GenericNameSyntax? genericName = invocation.Expression switch
    {
      MemberAccessExpressionSyntax memberAccess => memberAccess.Name as GenericNameSyntax,
      GenericNameSyntax g => g,
      _ => null
    };

    if (genericName?.TypeArgumentList.Arguments.Count is null or 0)
      return (null, null);

    TypeArgumentListSyntax typeArgs = genericName.TypeArgumentList;

    // Get service type from first type argument
    TypeSyntax serviceTypeSyntax = typeArgs.Arguments[0];
    ITypeSymbol? serviceTypeSymbol = TypeSyntaxResolver.ResolveType(semanticModel, serviceTypeSyntax, cancellationToken);
    string? serviceTypeName = serviceTypeSymbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    // Get implementation type from second type argument (if present)
    string? implTypeName = null;
    if (typeArgs.Arguments.Count > 1)
    {
      TypeSyntax implTypeSyntax = typeArgs.Arguments[1];
      ITypeSymbol? implTypeSymbol = TypeSyntaxResolver.ResolveType(semanticModel, implTypeSyntax, cancellationToken);
      implTypeName = implTypeSymbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    return (serviceTypeName, implTypeName);
  }
}
