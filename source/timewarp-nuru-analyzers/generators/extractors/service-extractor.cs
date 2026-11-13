#region Purpose
// Extracts service registration information from ConfigureServices() calls.
#endregion
#region Design
// Handles .ConfigureServices(services => { ... }) and .ConfigureServices(ConfigureServices) (method group),
// services registered via AddTransient, AddScoped, AddSingleton, AddSessionScoped, AddCommandScoped, TryAdd*,
// in-project AddX via syntax and referenced AddX via decompile (task 395). Also detects:
// - Factory delegate registrations (NURU053)
// - Constructor dependencies of implementation types (NURU051)
// - Internal types (NURU054)
// - Opaque extension method calls (NURU052) after lowering fails
// - AddLogging calls, matched by method symbol: no delegate, lambda, or anonymous method (task 546)
// - Multiple constructors (chooses one with most parameters)
// - Optional parameters with default values. String and char defaults are
//   SymbolDisplay.FormatLiteral so quotes, backslashes, and newlines compile.
//   Other primitives use SymbolDisplay.FormatPrimitive plus the C# type suffix
//   FormatPrimitive omits. Enum defaults are fully-qualified members.
#endregion

namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Extracts service registration information from ConfigureServices() calls.
/// </summary>
internal static partial class ServiceExtractor
{
  /// <summary>
  /// Standard service registration methods that we can fully analyze without lowering.
  /// Other method calls are lowered or tracked as extension methods (NURU052).
  /// </summary>
  private static readonly HashSet<string> AnalyzableMethods =
  [
    "AddTransient",
    "AddScoped",
    "AddSingleton",
    "AddSessionScoped",
    "AddCommandScoped",
    "TryAddTransient",
    "TryAddScoped",
    "TryAddSingleton",
    "TryAddSessionScoped",
    "TryAddCommandScoped"
  ];

  /// <summary>
  /// Fully qualifies enum members. FullyQualifiedFormat leaves memberOptions empty,
  /// so a field would render as the bare member name.
  /// </summary>
  private static readonly SymbolDisplayFormat FullyQualifiedMemberFormat = new
  (
    globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
    typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
    genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
    memberOptions: SymbolDisplayMemberOptions.IncludeContainingType
  );

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
    List<ServiceDefinition> services = [];
    List<ExtensionMethodCall> extensionMethods = [];

    if (body is ExpressionSyntax expression)
    {
      ExtractFromExpression(expression, semanticModel, services, extensionMethods, cancellationToken);
      return new ServiceExtractionResult([.. services], [.. extensionMethods], []);
    }

    if (body is BlockSyntax block)
    {
      foreach (StatementSyntax statement in block.Statements)
      {
        ExpressionSyntax? statementExpression = statement switch
        {
          ExpressionStatementSyntax expressionStatement => expressionStatement.Expression,
          ReturnStatementSyntax returnStatement => returnStatement.Expression,
          _ => null
        };

        if (statementExpression is not null)
          ExtractFromExpression(statementExpression, semanticModel, services, extensionMethods, cancellationToken);
      }
    }

    return new ServiceExtractionResult([.. services], [.. extensionMethods], []);
  }

  /// <summary>
  /// Extracts registrations from an expression, including chained calls.
  /// </summary>
  private static void ExtractFromExpression
  (
    ExpressionSyntax expression,
    SemanticModel semanticModel,
    List<ServiceDefinition> services,
    List<ExtensionMethodCall> extensionMethods,
    CancellationToken cancellationToken
  )
  {
    if (expression is not InvocationExpressionSyntax)
      return;

    foreach (InvocationExpressionSyntax invocation in ExtensionMethodLowerer.FlattenChain(expression))
      ExtractFromInvocation(invocation, semanticModel, services, extensionMethods, cancellationToken);
  }

  /// <summary>
  /// Extracts a service definition from an invocation expression.
  /// Lifetime Add*/TryAdd* are analyzed directly. Other calls are lowered or
  /// recorded as opaque extension methods (NURU052).
  /// </summary>
  private static void ExtractFromInvocation
  (
    InvocationExpressionSyntax invocation,
    SemanticModel semanticModel,
    List<ServiceDefinition> services,
    List<ExtensionMethodCall> extensionMethods,
    CancellationToken cancellationToken
  )
  {
    string? methodName = GetMethodName(invocation);
    if (methodName is null)
      return;

    if (ServiceRegistrationMethods.IsSpecialCased(methodName))
    {
      extensionMethods.Add(new ExtensionMethodCall(methodName, LocationInfo.CreateFrom(invocation.GetLocation())));
      return;
    }

    if (!AnalyzableMethods.Contains(methodName))
    {
      if (ExtensionMethodLowerer.TryLower(
        invocation,
        semanticModel,
        services,
        invocation.GetLocation(),
        cancellationToken,
        out ImmutableArray<ServiceDefinition> lowered))
      {
        services.AddRange(lowered);
        return;
      }

      extensionMethods.Add(new ExtensionMethodCall(methodName, LocationInfo.CreateFrom(invocation.GetLocation())));
      return;
    }

    if (!ServiceRegistrationMethods.TryGetLifetime(methodName, out ServiceLifetime lifetime, out bool isTryAdd))
      return;

    bool isFactoryRegistration = IsFactoryRegistration(invocation);

    (string? serviceTypeName, string? implementationTypeName, INamedTypeSymbol? implementationSymbol) =
      ExtractServiceTypesWithSymbol(invocation, semanticModel, cancellationToken);

    if (serviceTypeName is null)
      return;

    if (isTryAdd && !isFactoryRegistration && IsServiceTypeRegistered(services, serviceTypeName))
      return;

    ImmutableArray<string> constructorDeps = [];
    ImmutableArray<ConstructorParameter> constructorParams = [];
    bool isInternalType = false;

    if (implementationSymbol is not null && !isFactoryRegistration)
    {
      constructorDeps = ExtractConstructorDependencies(implementationSymbol);
      constructorParams = ExtractConstructorParameters(implementationSymbol);
      isInternalType = IsInternalType(implementationSymbol, semanticModel.Compilation);
    }

    services.Add(new ServiceDefinition(
      ServiceTypeName: serviceTypeName,
      ImplementationTypeName: implementationTypeName ?? serviceTypeName,
      Lifetime: lifetime,
      ConstructorDependencyTypes: constructorDeps,
      ConstructorParameters: constructorParams,
      IsFactoryRegistration: isFactoryRegistration,
      IsInternalType: isInternalType,
      RegistrationLocation: LocationInfo.CreateFrom(invocation.GetLocation())));
  }

  private static bool IsServiceTypeRegistered(List<ServiceDefinition> services, string serviceTypeName)
  {
    string normalized = serviceTypeName.StartsWith("global::", StringComparison.Ordinal)
      ? serviceTypeName[8..]
      : serviceTypeName;

    foreach (ServiceDefinition service in services)
    {
      string existing = service.ServiceTypeName.StartsWith("global::", StringComparison.Ordinal)
        ? service.ServiceTypeName[8..]
        : service.ServiceTypeName;
      if (string.Equals(existing, normalized, StringComparison.Ordinal))
        return true;
    }

    return false;
  }

  /// <summary>
  /// Detects if an invocation uses a factory delegate.
  /// Factory delegates: services.AddSingleton&lt;IFoo&gt;(sp => new Foo(...))
  /// </summary>
  internal static bool IsFactoryDelegate(InvocationExpressionSyntax invocation)
    => IsFactoryRegistration(invocation);

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
}
