#region Purpose
// Lower pure IServiceCollection AddX scripts (in-project syntax or decompiled referenced bodies) into ServiceDefinitions.
#endregion

#region Design
// Follow DeclaringSyntaxReferences when the method is in this compilation — no decompiler.
// Referenced assemblies go through ILSpy on the lib/ implementation. Fail closed: any statement
// that is not a lowerable collection call (or a same-assembly helper that is also only that)
// aborts the whole user-facing AddX. Never partial-lower. Type arguments from the call site
// substitute into generic AddX<T> bodies. TryAdd* consults the accumulated model so user
// lines and inlined library calls share one order.
#endregion

namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Lowers opaque <c>AddX</c> extension methods into <see cref="ServiceDefinition"/> entries.
/// </summary>
internal static class ExtensionMethodLowerer
{
  /// <summary>
  /// Attempts to lower a user-facing <c>AddX</c> invocation.
  /// On failure the caller must report NURU052 and must not keep any partial services.
  /// </summary>
  internal static bool TryLower
  (
    InvocationExpressionSyntax invocation,
    SemanticModel semanticModel,
    IReadOnlyList<ServiceDefinition> alreadyRegistered,
    Location userFacingLocation,
    CancellationToken cancellationToken,
    out ImmutableArray<ServiceDefinition> services
  )
  {
    services = [];
    ArgumentNullException.ThrowIfNull(invocation);
    ArgumentNullException.ThrowIfNull(semanticModel);
    ArgumentNullException.ThrowIfNull(alreadyRegistered);

    IMethodSymbol? methodSymbol = GetMethodSymbol(invocation, semanticModel, cancellationToken);
    if (methodSymbol is null)
      return false;

    IMethodSymbol unreduced = methodSymbol.ReducedFrom ?? methodSymbol;
    if (!IsVoidOrServiceCollection(unreduced.ReturnType))
      return false;

    List<ServiceDefinition> accumulator = [.. alreadyRegistered];
    int start = accumulator.Count;
    HashSet<IMethodSymbol> visiting = new(SymbolEqualityComparer.Default);

    ImmutableDictionary<string, ITypeSymbol> emptyTypeArgs = [];
    if (!TryLowerMethod(
      unreduced,
      methodSymbol,
      semanticModel.Compilation,
      emptyTypeArgs,
      accumulator,
      visiting,
      userFacingLocation,
      cancellationToken))
    {
      return false;
    }

    services = [.. accumulator.Skip(start)];
    return true;
  }

  private static bool TryLowerMethod
  (
    IMethodSymbol unreduced,
    IMethodSymbol constructed,
    Compilation compilation,
    ImmutableDictionary<string, ITypeSymbol> parentTypeArgs,
    List<ServiceDefinition> accumulator,
    HashSet<IMethodSymbol> visiting,
    Location userFacingLocation,
    CancellationToken cancellationToken
  )
  {
    IMethodSymbol identity = unreduced.OriginalDefinition;
    if (!visiting.Add(identity))
      return false;

    try
    {
      ImmutableDictionary<string, ITypeSymbol> typeArgs = MapTypeArguments(identity, constructed, parentTypeArgs);

      SyntaxReference? syntaxReference = identity.DeclaringSyntaxReferences.FirstOrDefault();
      if (syntaxReference is not null)
      {
        return TryLowerFromSyntax(
          syntaxReference,
          identity,
          compilation,
          typeArgs,
          accumulator,
          visiting,
          userFacingLocation,
          cancellationToken);
      }

      return TryLowerFromDecompile(
        identity,
        compilation,
        typeArgs,
        accumulator,
        visiting,
        userFacingLocation,
        cancellationToken);
    }
    finally
    {
      visiting.Remove(identity);
    }
  }

  private static bool TryLowerFromSyntax
  (
    SyntaxReference syntaxReference,
    IMethodSymbol identity,
    Compilation compilation,
    ImmutableDictionary<string, ITypeSymbol> typeArgs,
    List<ServiceDefinition> accumulator,
    HashSet<IMethodSymbol> visiting,
    Location userFacingLocation,
    CancellationToken cancellationToken
  )
  {
    Microsoft.CodeAnalysis.SyntaxNode methodSyntax = syntaxReference.GetSyntax(cancellationToken);
    CSharpSyntaxNode? body = methodSyntax switch
    {
      MethodDeclarationSyntax methodDeclaration => (CSharpSyntaxNode?)methodDeclaration.Body ?? methodDeclaration.ExpressionBody?.Expression,
      LocalFunctionStatementSyntax localFunction => (CSharpSyntaxNode?)localFunction.Body ?? localFunction.ExpressionBody?.Expression,
      _ => null
    };

    if (body is null)
      return false;

    SemanticModel semanticModel = compilation.GetSemanticModel(methodSyntax.SyntaxTree);
    string? collectionParameterName = GetCollectionParameterName(identity);
    return TryLowerBody(
      body,
      semanticModel,
      compilation,
      identity,
      typeArgs,
      collectionParameterName,
      accumulator,
      visiting,
      userFacingLocation,
      cancellationToken);
  }

  private static bool TryLowerFromDecompile
  (
    IMethodSymbol identity,
    Compilation compilation,
    ImmutableDictionary<string, ITypeSymbol> typeArgs,
    List<ServiceDefinition> accumulator,
    HashSet<IMethodSymbol> visiting,
    Location userFacingLocation,
    CancellationToken cancellationToken
  )
  {
    ReferencedMethodDecompiler.CachedDecompile decompiled = ReferencedMethodDecompiler.Decompile(identity, compilation);
    if (!decompiled.Success || string.IsNullOrWhiteSpace(decompiled.Source))
      return false;

    string containingNamespace = identity.ContainingNamespace.IsGlobalNamespace
      ? string.Empty
      : identity.ContainingNamespace.ToDisplayString();

    string wrapper = WrapDecompiledMethod(decompiled.Source, containingNamespace);
    SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(wrapper, cancellationToken: cancellationToken);
    CSharpCompilation dummyCompilation = CSharpCompilation.Create(
      assemblyName: "NuruDecompiledAddX",
      syntaxTrees: [syntaxTree],
      references: compilation.References,
      options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    SemanticModel dummyModel = dummyCompilation.GetSemanticModel(syntaxTree);
    MethodDeclarationSyntax? methodDeclaration = syntaxTree.GetRoot(cancellationToken)
      .DescendantNodes()
      .OfType<MethodDeclarationSyntax>()
      .FirstOrDefault();

    if (methodDeclaration is null)
      return false;

    CSharpSyntaxNode? body = (CSharpSyntaxNode?)methodDeclaration.Body ?? methodDeclaration.ExpressionBody?.Expression;
    if (body is null)
      return false;

    string? collectionParameterName = methodDeclaration.ParameterList.Parameters
      .Select(static p => p.Identifier.Text)
      .FirstOrDefault();

    if (string.IsNullOrEmpty(collectionParameterName))
      collectionParameterName = GetCollectionParameterName(identity);

    return TryLowerBody(
      body,
      dummyModel,
      compilation,
      identity,
      typeArgs,
      collectionParameterName,
      accumulator,
      visiting,
      userFacingLocation,
      cancellationToken);
  }

  private static string WrapDecompiledMethod(string decompiledSource, string containingNamespace)
  {
    string usingNamespace = string.IsNullOrEmpty(containingNamespace)
      ? string.Empty
      : $"using {containingNamespace};\n";

    return
      "#nullable disable\n"
      + "using System;\n"
      + "using Microsoft.Extensions.DependencyInjection;\n"
      + "using Microsoft.Extensions.DependencyInjection.Extensions;\n"
      + usingNamespace
      + "static class __NuruDecompiledHolder\n"
      + "{\n"
      + decompiledSource + "\n"
      + "}";
  }

  private static bool TryLowerBody
  (
    CSharpSyntaxNode body,
    SemanticModel semanticModel,
    Compilation compilation,
    IMethodSymbol identity,
    ImmutableDictionary<string, ITypeSymbol> typeArgs,
    string? collectionParameterName,
    List<ServiceDefinition> accumulator,
    HashSet<IMethodSymbol> visiting,
    Location userFacingLocation,
    CancellationToken cancellationToken
  )
  {
    if (body is ExpressionSyntax expression)
      return TryLowerExpression(expression, semanticModel, compilation, identity, typeArgs, collectionParameterName, accumulator, visiting, userFacingLocation, cancellationToken);

    if (body is not BlockSyntax block)
      return false;

    foreach (StatementSyntax statement in block.Statements)
    {
      cancellationToken.ThrowIfCancellationRequested();

      switch (statement)
      {
        case EmptyStatementSyntax:
          continue;
        case ReturnStatementSyntax returnStatement:
          if (returnStatement.Expression is null)
            continue;
          if (!TryLowerExpression(returnStatement.Expression, semanticModel, compilation, identity, typeArgs, collectionParameterName, accumulator, visiting, userFacingLocation, cancellationToken))
            return false;
          break;
        case ExpressionStatementSyntax expressionStatement:
          if (!TryLowerExpression(expressionStatement.Expression, semanticModel, compilation, identity, typeArgs, collectionParameterName, accumulator, visiting, userFacingLocation, cancellationToken))
            return false;
          break;
        default:
          return false;
      }
    }

    return true;
  }

  private static bool TryLowerExpression
  (
    ExpressionSyntax expression,
    SemanticModel semanticModel,
    Compilation compilation,
    IMethodSymbol identity,
    ImmutableDictionary<string, ITypeSymbol> typeArgs,
    string? collectionParameterName,
    List<ServiceDefinition> accumulator,
    HashSet<IMethodSymbol> visiting,
    Location userFacingLocation,
    CancellationToken cancellationToken
  )
  {
    if (expression is IdentifierNameSyntax identifier)
      return collectionParameterName is not null && identifier.Identifier.Text == collectionParameterName;

    if (expression is not InvocationExpressionSyntax)
      return false;

    List<InvocationExpressionSyntax> chain = FlattenChain(expression);
    if (chain.Count == 0)
      return false;

    if (!ReceiverIsCollectionParameter(chain[0], collectionParameterName))
      return false;

    foreach (InvocationExpressionSyntax invocation in chain)
    {
      if (!TryLowerInvocation(invocation, semanticModel, compilation, identity, typeArgs, collectionParameterName, accumulator, visiting, userFacingLocation, cancellationToken))
        return false;
    }

    return true;
  }

  private static bool TryLowerInvocation
  (
    InvocationExpressionSyntax invocation,
    SemanticModel semanticModel,
    Compilation compilation,
    IMethodSymbol enclosingMethod,
    ImmutableDictionary<string, ITypeSymbol> typeArgs,
    string? collectionParameterName,
    List<ServiceDefinition> accumulator,
    HashSet<IMethodSymbol> visiting,
    Location userFacingLocation,
    CancellationToken cancellationToken
  )
  {
    string? methodName = ServiceExtractor.GetInvocationMethodName(invocation);
    if (methodName is null)
      return false;

    if (ServiceRegistrationMethods.IsSpecialCased(methodName))
      return false;

    if (ServiceRegistrationMethods.TryGetLifetime(methodName, out ServiceLifetime lifetime, out bool isTryAdd))
    {
      return TryAddLifetimeRegistration(
        invocation,
        semanticModel,
        compilation,
        enclosingMethod,
        typeArgs,
        lifetime,
        isTryAdd,
        collectionParameterName,
        accumulator,
        userFacingLocation,
        cancellationToken);
    }

    IMethodSymbol? helper = GetMethodSymbol(invocation, semanticModel, cancellationToken);
    if (helper is null)
      return false;

    IMethodSymbol unreducedHelper = helper.ReducedFrom ?? helper;
    if (!SymbolEqualityComparer.Default.Equals(unreducedHelper.ContainingAssembly, enclosingMethod.ContainingAssembly))
      return false;

    if (!IsVoidOrServiceCollection(unreducedHelper.ReturnType))
      return false;

    return TryLowerMethod(
      unreducedHelper,
      helper,
      compilation,
      typeArgs,
      accumulator,
      visiting,
      userFacingLocation,
      cancellationToken);
  }

  private static bool TryAddLifetimeRegistration
  (
    InvocationExpressionSyntax invocation,
    SemanticModel semanticModel,
    Compilation compilation,
    IMethodSymbol enclosingMethod,
    ImmutableDictionary<string, ITypeSymbol> typeArgs,
    ServiceLifetime lifetime,
    bool isTryAdd,
    string? collectionParameterName,
    List<ServiceDefinition> accumulator,
    Location userFacingLocation,
    CancellationToken cancellationToken
  )
  {
    if (HasUnlowerableArguments(invocation, collectionParameterName))
      return false;

    (ITypeSymbol? serviceType, ITypeSymbol? implementationType) = ResolveRegistrationTypes(
      invocation,
      semanticModel,
      compilation,
      enclosingMethod.ContainingAssembly,
      typeArgs,
      cancellationToken);

    if (serviceType is null || implementationType is null)
      return false;

    if (IsOpenGeneric(serviceType) || IsOpenGeneric(implementationType))
      return false;

    if (serviceType is not INamedTypeSymbol namedService || implementationType is not INamedTypeSymbol namedImplementation)
      return false;

    string serviceTypeName = namedService.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    string implementationTypeName = namedImplementation.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    if (isTryAdd && AlreadyRegistered(accumulator, serviceTypeName))
      return true;

    ImmutableArray<string> constructorDeps = ServiceExtractor.GetConstructorDependencyTypes(namedImplementation);
    ImmutableArray<ConstructorParameter> constructorParams = ServiceExtractor.GetConstructorParameters(namedImplementation);
    bool isInternalType = !compilation.IsSymbolAccessibleWithin(namedImplementation, compilation.Assembly);

    accumulator.Add(new ServiceDefinition(
      ServiceTypeName: serviceTypeName,
      ImplementationTypeName: implementationTypeName,
      Lifetime: lifetime,
      ConstructorDependencyTypes: constructorDeps,
      ConstructorParameters: constructorParams,
      IsFactoryRegistration: false,
      IsInternalType: isInternalType,
      RegistrationLocation: LocationInfo.CreateFrom(userFacingLocation)));

    return true;
  }

  private static (ITypeSymbol? ServiceType, ITypeSymbol? ImplementationType) ResolveRegistrationTypes
  (
    InvocationExpressionSyntax invocation,
    SemanticModel semanticModel,
    Compilation compilation,
    IAssemblySymbol fallbackAssembly,
    ImmutableDictionary<string, ITypeSymbol> typeArgs,
    CancellationToken cancellationToken
  )
  {
    (string? serviceTypeName, string? implementationTypeName, INamedTypeSymbol? implementationSymbol) =
      ServiceExtractor.GetServiceTypesWithSymbol(invocation, semanticModel, cancellationToken);

    ITypeSymbol? serviceType = ResolveType(
      serviceTypeName,
      GetTypeArgumentSyntax(invocation, 0),
      compilation,
      fallbackAssembly,
      typeArgs);

    ITypeSymbol? implementationType = implementationSymbol as ITypeSymbol;
    if (implementationType is ITypeParameterSymbol or null || implementationType.TypeKind == TypeKind.Error)
    {
      implementationType = ResolveType(
        implementationTypeName ?? serviceTypeName,
        GetTypeArgumentSyntax(invocation, 1) ?? GetTypeArgumentSyntax(invocation, 0),
        compilation,
        fallbackAssembly,
        typeArgs);
    }
    else
    {
      implementationType = SubstituteType(implementationType, typeArgs);
    }

    serviceType = serviceType is null ? null : SubstituteType(serviceType, typeArgs);
    if (implementationType is null && serviceType is not null)
      implementationType = serviceType;

    return (serviceType, implementationType);
  }

  private static ITypeSymbol? ResolveType
  (
    string? typeName,
    TypeSyntax? typeSyntax,
    Compilation compilation,
    IAssemblySymbol fallbackAssembly,
    ImmutableDictionary<string, ITypeSymbol> typeArgs
  )
  {
    if (typeSyntax is IdentifierNameSyntax identifier
        && typeArgs.TryGetValue(identifier.Identifier.Text, out ITypeSymbol? fromMap))
    {
      return fromMap;
    }

    string? metadataName = typeSyntax is not null
      ? ToMetadataName(typeSyntax)
      : NormalizeMetadataName(typeName);

    if (metadataName is not null && typeArgs.TryGetValue(metadataName, out ITypeSymbol? mappedByName))
      return mappedByName;

    if (metadataName is not null)
    {
      INamedTypeSymbol? byMetadata = compilation.GetTypeByMetadataName(metadataName);
      if (byMetadata is not null)
        return byMetadata;

      INamedTypeSymbol? inAssembly = FindTypeByMetadataOrName(fallbackAssembly.GlobalNamespace, metadataName);
      if (inAssembly is not null)
        return inAssembly;

      INamedTypeSymbol? inCompilation = FindTypeByMetadataOrName(compilation.GlobalNamespace, metadataName);
      if (inCompilation is not null)
        return inCompilation;
    }

    return null;
  }

  private static ITypeSymbol SubstituteType(ITypeSymbol type, ImmutableDictionary<string, ITypeSymbol> typeArgs)
  {
    if (type is ITypeParameterSymbol typeParameter
        && typeArgs.TryGetValue(typeParameter.Name, out ITypeSymbol? mapped))
    {
      return mapped;
    }

    return type;
  }

  private static TypeSyntax? GetTypeArgumentSyntax(InvocationExpressionSyntax invocation, int index)
  {
    GenericNameSyntax? genericName = invocation.Expression switch
    {
      MemberAccessExpressionSyntax memberAccess => memberAccess.Name as GenericNameSyntax,
      GenericNameSyntax g => g,
      _ => null
    };

    if (genericName is null || genericName.TypeArgumentList.Arguments.Count <= index)
      return null;

    return genericName.TypeArgumentList.Arguments[index];
  }

  private static string? ToMetadataName(TypeSyntax typeSyntax)
  {
    return typeSyntax switch
    {
      IdentifierNameSyntax identifier => identifier.Identifier.Text,
      QualifiedNameSyntax qualified => $"{ToMetadataName(qualified.Left)}.{qualified.Right.Identifier.Text}",
      AliasQualifiedNameSyntax alias => ToMetadataName(alias.Name),
      GenericNameSyntax => null,
      NullableTypeSyntax nullable => ToMetadataName(nullable.ElementType),
      _ => NormalizeMetadataName(typeSyntax.ToString())
    };
  }

  private static string? NormalizeMetadataName(string? typeName)
  {
    if (string.IsNullOrWhiteSpace(typeName))
      return null;

    string trimmed = typeName.Trim();
    if (trimmed.StartsWith("global::", StringComparison.Ordinal))
      trimmed = trimmed[8..];

    if (trimmed.Contains('<', StringComparison.Ordinal))
      return null;

    return trimmed.Replace(" ", "", StringComparison.Ordinal);
  }

  private static INamedTypeSymbol? FindTypeByMetadataOrName(INamespaceSymbol namespaceSymbol, string name)
  {
    int lastDot = name.LastIndexOf('.');
    if (lastDot < 0)
    {
      ImmutableArray<INamedTypeSymbol> members = namespaceSymbol.GetTypeMembers(name);
      return members.FirstOrDefault(static t => !t.IsGenericType || t.TypeArguments.All(static a => a.TypeKind != TypeKind.TypeParameter));
    }

    foreach (INamedTypeSymbol type in namespaceSymbol.GetTypeMembers())
    {
      string display = type.ToDisplayString();
      if (string.Equals(display, name, StringComparison.Ordinal))
        return type;
    }

    foreach (INamespaceSymbol child in namespaceSymbol.GetNamespaceMembers())
    {
      INamedTypeSymbol? found = FindTypeByMetadataOrName(child, name);
      if (found is not null)
        return found;
    }

    return null;
  }

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
