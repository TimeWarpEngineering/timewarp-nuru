// Extracts service registration information from ConfigureServices() calls.
//
// Handles:
// - .ConfigureServices(services => { ... })           - inline lambda
// - .ConfigureServices(ConfigureServices)             - method group reference
// - Services registered via AddTransient, AddScoped, AddSingleton, TryAdd*
// - In-project AddX via syntax and referenced AddX via decompile (task 395)
//
// Also detects:
// - Factory delegate registrations (NURU053)
// - Constructor dependencies of implementation types (NURU051)
// - Internal types (NURU054)
// - Opaque extension method calls (NURU052) after lowering fails
// - Multiple constructors (chooses one with most parameters)
// - Optional parameters with default values. String and char defaults are
//   SymbolDisplay.FormatLiteral so quotes, backslashes, and newlines compile.
//   Other primitives use SymbolDisplay.FormatPrimitive plus the C# type suffix
//   FormatPrimitive omits. Enum defaults are fully-qualified members.

namespace TimeWarp.Nuru.Generators;

using System.Globalization;

/// <summary>
/// Extracts service registration information from ConfigureServices() calls.
/// </summary>
internal static class ServiceExtractor
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
    "TryAddTransient",
    "TryAddScoped",
    "TryAddSingleton"
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
      extensionMethods.Add(new ExtensionMethodCall(methodName, invocation.GetLocation()));
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

      extensionMethods.Add(new ExtensionMethodCall(methodName, invocation.GetLocation()));
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
      isInternalType = IsInternalType(implementationSymbol);
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

  /// <summary>
  /// Extracts constructor dependencies from an implementation type.
  /// Supports multiple constructors - chooses the one with most parameters.
  /// </summary>
  internal static ImmutableArray<string> GetConstructorDependencyTypes(INamedTypeSymbol implementationType)
    => ExtractConstructorDependencies(implementationType);

  /// <summary>
  /// Extracts constructor dependencies from an implementation type.
  /// Supports multiple constructors - chooses the one with most parameters.
  /// </summary>
  private static ImmutableArray<string> ExtractConstructorDependencies(INamedTypeSymbol implementationType)
  {
    // Find the best constructor (most parameters)
    IMethodSymbol? constructor = FindBestConstructor(implementationType);

    if (constructor is null || constructor.Parameters.Length == 0)
      return [];

    return [.. constructor.Parameters
      .Select(p => p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))];
  }

  /// <summary>
  /// Extracts detailed constructor parameters from an implementation type.
  /// Supports multiple constructors, optional parameters, and built-in type detection.
  /// </summary>
  internal static ImmutableArray<ConstructorParameter> GetConstructorParameters(INamedTypeSymbol implementationType)
    => ExtractConstructorParameters(implementationType);

  /// <summary>
  /// Extracts detailed constructor parameters from an implementation type.
  /// Supports multiple constructors, optional parameters, and built-in type detection.
  /// </summary>
  private static ImmutableArray<ConstructorParameter> ExtractConstructorParameters(INamedTypeSymbol implementationType)
  {
    // Find the best constructor (most parameters)
    IMethodSymbol? constructor = FindBestConstructor(implementationType);

    if (constructor is null || constructor.Parameters.Length == 0)
      return [];

    ImmutableArray<ConstructorParameter>.Builder parameters = ImmutableArray.CreateBuilder<ConstructorParameter>();

    foreach (IParameterSymbol param in constructor.Parameters)
    {
      string typeName = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
      bool hasDefaultValue = param.HasExplicitDefaultValue;
      string? defaultValue = null;

      if (hasDefaultValue)
      {
        defaultValue = GetDefaultValueExpression(param);
      }

      parameters.Add(new ConstructorParameter(
        ParameterName: param.Name,
        TypeName: typeName,
        HasDefaultValue: hasDefaultValue,
        DefaultValue: defaultValue));
    }

    return parameters.ToImmutable();
  }

  /// <summary>
  /// Finds the best constructor for DI resolution.
  /// MS DI behavior: choose the constructor with the most parameters.
  /// </summary>
  private static IMethodSymbol? FindBestConstructor(INamedTypeSymbol implementationType)
  {
    // Get all public non-static constructors
    // Choose the constructor with the most parameters
    // This matches MS DI behavior for constructor selection
    return implementationType.InstanceConstructors
      .Where(c => c.DeclaredAccessibility == Accessibility.Public && !c.IsImplicitlyDeclared)
      .OrderByDescending(c => c.Parameters.Length)
      .FirstOrDefault();
  }

  /// <summary>
  /// Gets the default value expression for a parameter.
  /// </summary>
  private static string? GetDefaultValueExpression(IParameterSymbol param)
  {
    if (!param.HasExplicitDefaultValue)
      return null;

    object? defaultValue = param.ExplicitDefaultValue;
    if (defaultValue is null)
      return "null";

    ITypeSymbol valueType = UnwrapNullable(param.Type);
    if (valueType.TypeKind == TypeKind.Enum && valueType is INamedTypeSymbol enumType)
      return FormatEnumDefault(enumType, defaultValue);

    return FormatPrimitiveDefault(defaultValue);
  }

  private static ITypeSymbol UnwrapNullable(ITypeSymbol type)
  {
    if (type is INamedTypeSymbol named
        && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
        && named.TypeArguments.Length == 1)
    {
      return named.TypeArguments[0];
    }

    return type;
  }

  /// <summary>
  /// Emits <c>global::Namespace.Enum.Member</c>, or a cast of the underlying
  /// literal when the constant is not a single named member.
  /// </summary>
  private static string FormatEnumDefault(INamedTypeSymbol enumType, object defaultValue)
  {
    foreach (IFieldSymbol field in enumType.GetMembers().OfType<IFieldSymbol>())
    {
      if (field.HasConstantValue
          && field.ConstantValue is not null
          && EnumConstantEquals(field.ConstantValue, defaultValue))
      {
        return field.ToDisplayString(FullyQualifiedMemberFormat);
      }
    }

    string typeName = enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    string literal = FormatPrimitiveDefault(defaultValue);
    return $"({typeName}){literal}";
  }

  /// <summary>
  /// Roslyn boxes an enum default as its underlying integral value. Field
  /// constants use that same box. Compare bits so a boxed enum still matches.
  /// </summary>
  private static bool EnumConstantEquals(object fieldValue, object defaultValue)
  {
    if (fieldValue.Equals(defaultValue))
      return true;

    return TryIntegralBits(fieldValue, out ulong fieldBits)
      && TryIntegralBits(defaultValue, out ulong defaultBits)
      && fieldBits == defaultBits;
  }

  private static bool TryIntegralBits(object value, out ulong bits)
  {
    if (value is Enum enumValue)
    {
      value = Convert.ChangeType(
        enumValue,
        Enum.GetUnderlyingType(enumValue.GetType()),
        CultureInfo.InvariantCulture);
    }

    switch (value)
    {
      case byte v:
        bits = v;
        return true;
      case sbyte v:
        bits = unchecked((ulong)v);
        return true;
      case short v:
        bits = unchecked((ulong)v);
        return true;
      case ushort v:
        bits = v;
        return true;
      case int v:
        bits = unchecked((ulong)v);
        return true;
      case uint v:
        bits = v;
        return true;
      case long v:
        bits = unchecked((ulong)v);
        return true;
      case ulong v:
        bits = v;
        return true;
      default:
        bits = 0;
        return false;
    }
  }

  /// <summary>
  /// String and char use FormatLiteral. Other primitives use FormatPrimitive.
  /// FormatPrimitive does not emit F/M/U/L/UL suffixes or non-finite names,
  /// which are required for the expression to compile as that parameter type.
  /// </summary>
  private static string FormatPrimitiveDefault(object defaultValue)
  {
    switch (defaultValue)
    {
      case string s:
        return SymbolDisplay.FormatLiteral(s, quote: true);
      case char c:
        return SymbolDisplay.FormatLiteral(c, quote: true);
      case float f when float.IsNaN(f):
        return "float.NaN";
      case float f when float.IsPositiveInfinity(f):
        return "float.PositiveInfinity";
      case float f when float.IsNegativeInfinity(f):
        return "float.NegativeInfinity";
      case double d when double.IsNaN(d):
        return "double.NaN";
      case double d when double.IsPositiveInfinity(d):
        return "double.PositiveInfinity";
      case double d when double.IsNegativeInfinity(d):
        return "double.NegativeInfinity";
    }

    string? literal = SymbolDisplay.FormatPrimitive(defaultValue, quoteStrings: true, useHexadecimalNumbers: false);
    if (literal is null)
      return "default";

    return defaultValue switch
    {
      float => literal + "F",
      decimal => literal + "M",
      uint => literal + "U",
      long => literal + "L",
      ulong => literal + "UL",
      _ => literal
    };
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
      {
        return true;
      }

      current = current.ContainingType;
    }

    return false;
  }

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
