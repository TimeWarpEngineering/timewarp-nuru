#region Purpose
// Lifetime registration lowering and service/implementation type resolution (ExtensionMethodLowerer partial).
#endregion

namespace TimeWarp.Nuru.Generators;

internal static partial class ExtensionMethodLowerer
{
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
    int lastDot = name.LastIndexOf('.', StringComparison.Ordinal);
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
}
