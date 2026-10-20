#region Purpose
// Finds [Operation] and [NuruRoute] endpoint types that live in a referenced assembly.
#endregion

#region Design
// Discovery is opt-in, so a referenced assembly never adds a surprise route:
// - [assembly: NuruEndpointAssembly(typeof(X))] adds every endpoint type in X's assembly to the
//   discovery pool, like a type declared in source.
// - Map<T>() on a referenced endpoint type adds that one endpoint, for that app only.
// A type in this compilation is skipped: the source pipeline already extracts it.
// A type must be a class or record class that generated code can name from this compilation.
// The generator and the JSON context MSBuild task share these rules.
#endregion

namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Locates endpoint types declared in referenced assemblies.
/// </summary>
internal static class ReferencedEndpointLocator
{
  internal const string AssemblyAttributeMetadataName = "TimeWarp.Nuru.NuruEndpointAssemblyAttribute";
  private const string OperationAttributeMetadataName = "TimeWarp.Contracts.OperationAttribute";
  private const string NuruRouteAttributeMetadataName = "TimeWarp.Nuru.NuruRouteAttribute";

  /// <summary>
  /// The endpoint types of the assemblies that <paramref name="attributes"/> name.
  /// Each type carries the attribute location for diagnostics.
  /// </summary>
  internal static ImmutableArray<(INamedTypeSymbol Type, Location Location)> FromAssemblyAttributes
  (
    Compilation compilation,
    IEnumerable<AttributeData> attributes,
    CancellationToken cancellationToken
  )
  {
    ImmutableArray<(INamedTypeSymbol Type, Location Location)>.Builder types =
      ImmutableArray.CreateBuilder<(INamedTypeSymbol Type, Location Location)>();
    HashSet<IAssemblySymbol> seen = new(SymbolEqualityComparer.Default);

    foreach (AttributeData attribute in attributes)
    {
      if (attribute.AttributeClass?.ToDisplayString() != AssemblyAttributeMetadataName)
        continue;

      if (attribute.ConstructorArguments.Length != 1
          || attribute.ConstructorArguments[0].Value is not ITypeSymbol marker
          || marker.TypeKind == TypeKind.Error
          || marker.ContainingAssembly is not IAssemblySymbol assembly)
      {
        continue;
      }

      if (EndpointExtractor.IsDeclaredIn(compilation, marker) || !seen.Add(assembly))
        continue;

      Location location = attribute.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation()
        ?? Location.None;
      CollectEndpointTypes(compilation, assembly.GlobalNamespace, location, types, cancellationToken);
    }

    return types.ToImmutable();
  }

  /// <summary>
  /// The endpoint types named by <c>[assembly: NuruEndpointAssembly]</c> in this compilation.
  /// </summary>
  internal static ImmutableArray<(INamedTypeSymbol Type, Location Location)> FromAssemblyAttributes
  (
    Compilation compilation,
    CancellationToken cancellationToken
  ) =>
    FromAssemblyAttributes(compilation, compilation.Assembly.GetAttributes(), cancellationToken);

  /// <summary>
  /// Cheap syntactic test for <c>x.Map&lt;T&gt;()</c>.
  /// </summary>
  internal static bool IsGenericMapCall(Microsoft.CodeAnalysis.SyntaxNode node) =>
    node is InvocationExpressionSyntax
    {
      Expression: MemberAccessExpressionSyntax
      {
        Name: GenericNameSyntax { Identifier.ValueText: "Map", TypeArgumentList.Arguments.Count: 1 }
      },
      ArgumentList.Arguments.Count: 0
    };

  /// <summary>
  /// The type argument of <c>Map&lt;T&gt;()</c> when it is an endpoint type from a referenced assembly.
  /// </summary>
  internal static INamedTypeSymbol? MapTypeArgument
  (
    InvocationExpressionSyntax invocation,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    if (!IsGenericMapCall(invocation))
      return null;

    GenericNameSyntax genericName = (GenericNameSyntax)((MemberAccessExpressionSyntax)invocation.Expression).Name;
    TypeSyntax typeSyntax = genericName.TypeArgumentList.Arguments[0];
    if (TypeSyntaxResolver.ResolveType(semanticModel, typeSyntax, cancellationToken) is not INamedTypeSymbol type)
      return null;

    Compilation compilation = semanticModel.Compilation;
    if (EndpointExtractor.IsDeclaredIn(compilation, type) || IsNamedByAssemblyAttribute(compilation, type.ContainingAssembly))
      return null;

    return IsEndpointType(compilation, type) ? type : null;
  }

  /// <summary>
  /// True when an <c>[assembly: NuruEndpointAssembly]</c> already adds <paramref name="assembly"/>,
  /// so a <c>Map&lt;T&gt;()</c> of one of its types needs no second extraction.
  /// </summary>
  private static bool IsNamedByAssemblyAttribute(Compilation compilation, IAssemblySymbol? assembly)
  {
    if (assembly is null)
      return false;

    foreach (AttributeData attribute in compilation.Assembly.GetAttributes())
    {
      if (attribute.AttributeClass?.ToDisplayString() == AssemblyAttributeMetadataName
          && attribute.ConstructorArguments.Length == 1
          && attribute.ConstructorArguments[0].Value is ITypeSymbol marker
          && SymbolEqualityComparer.Default.Equals(marker.ContainingAssembly, assembly))
      {
        return true;
      }
    }

    return false;
  }

  /// <summary>
  /// True for a class or record class with <c>[Operation]</c> or <c>[NuruRoute]</c> that generated
  /// code in <paramref name="compilation"/> can name.
  /// </summary>
  internal static bool IsEndpointType(Compilation compilation, INamedTypeSymbol type)
  {
    if (type.TypeKind != TypeKind.Class || type.IsStatic || type.IsGenericType)
      return false;

    if (!HasEndpointAttribute(type))
      return false;

    return EndpointExtractor.IsAccessibleFrom(compilation, type);
  }

  private static bool HasEndpointAttribute(INamedTypeSymbol type)
  {
    foreach (AttributeData attribute in type.GetAttributes())
    {
      string? name = attribute.AttributeClass?.ToDisplayString();
      if (name is OperationAttributeMetadataName or NuruRouteAttributeMetadataName)
        return true;
    }

    return false;
  }

  private static void CollectEndpointTypes
  (
    Compilation compilation,
    INamespaceSymbol ns,
    Location location,
    ImmutableArray<(INamedTypeSymbol Type, Location Location)>.Builder types,
    CancellationToken cancellationToken
  )
  {
    cancellationToken.ThrowIfCancellationRequested();
    foreach (INamedTypeSymbol type in ns.GetTypeMembers())
      CollectEndpointType(compilation, type, location, types);

    foreach (INamespaceSymbol child in ns.GetNamespaceMembers())
      CollectEndpointTypes(compilation, child, location, types, cancellationToken);
  }

  private static void CollectEndpointType
  (
    Compilation compilation,
    INamedTypeSymbol type,
    Location location,
    ImmutableArray<(INamedTypeSymbol Type, Location Location)>.Builder types
  )
  {
    if (IsEndpointType(compilation, type))
      types.Add((type, location));

    foreach (INamedTypeSymbol nested in type.GetTypeMembers())
      CollectEndpointType(compilation, nested, location, types);
  }
}
