#region Purpose
// Endpoint class discovery and extraction, including referenced-assembly endpoints (NuruGenerator partial).
#endregion

namespace TimeWarp.Nuru.Generators;

using RoslynSyntaxNode = Microsoft.CodeAnalysis.SyntaxNode;

public sealed partial class NuruGenerator : IIncrementalGenerator
{
  /// <summary>
  /// Classes and record classes are endpoints. Record structs are not.
  /// </summary>
  private static bool IsRouteTypeDeclaration(RoslynSyntaxNode node) =>
    node is ClassDeclarationSyntax
    || (node is RecordDeclarationSyntax record
        && !record.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword));

  /// <summary>
  /// The route pipeline already extracts a type that also has [NuruRoute].
  /// Skipping it here keeps diagnostics from being reported twice.
  /// </summary>
  private static bool HasNuruRouteAttribute(GeneratorAttributeSyntaxContext context)
  {
    if (context.TargetSymbol is not INamedTypeSymbol type)
      return false;

    foreach (AttributeData attribute in type.GetAttributes())
    {
      if (attribute.AttributeClass?.ToDisplayString() == NuruRouteAttributeFullName)
        return true;
    }

    return false;
  }

  /// <summary>
  /// Extracts a RouteDefinition and location from a class or record with [NuruRoute] or [Operation].
  /// Also captures any diagnostics from the extraction process.
  /// </summary>
  private static EndpointWithLocationAndDiagnostics? ExtractEndpointWithLocation
  (
    GeneratorAttributeSyntaxContext context,
    CancellationToken cancellationToken
  )
  {
    if (context.TargetNode is not TypeDeclarationSyntax typeDeclaration || !IsRouteTypeDeclaration(typeDeclaration))
      return null;

    EndpointExtractionResult result = EndpointExtractor.Extract(
      typeDeclaration,
      context.SemanticModel,
      cancellationToken);

    // Get location from the [NuruRoute] attribute
    Location location = Location.None;
    foreach (AttributeListSyntax attributeList in typeDeclaration.AttributeLists)
    {
      foreach (AttributeSyntax attribute in attributeList.Attributes)
      {
        string? name = attribute.Name switch
        {
          IdentifierNameSyntax id => id.Identifier.Text,
          QualifiedNameSyntax q => q.Right.Identifier.Text,
          _ => null
        };

        if (name is "NuruRoute" or "NuruRouteAttribute")
        {
          location = attribute.GetLocation();
          break;
        }

        if (name is "Operation" or "OperationAttribute" && location == Location.None)
          location = attribute.GetLocation();
      }

      if (location != Location.None)
        break;
    }

    // If extraction failed with diagnostics but no route, still return the diagnostics
    if (result.Route is null && result.HasDiagnostics)
    {
      return new EndpointWithLocationAndDiagnostics(
        Pattern: null,
        Location: location,
        Route: null,
        Diagnostics: result.Diagnostics);
    }

    // If extraction failed with no route and no diagnostics (e.g., missing Handler), skip
    if (result.Route is null)
      return null;

    // Use EffectivePattern for endpoints to avoid collisions
    // (OriginalPattern is just the attribute string, EffectivePattern includes all segments)
    return new EndpointWithLocationAndDiagnostics(
      result.Route.EffectivePattern,
      location,
      result.Route,
      result.Diagnostics);
  }

  /// <summary>
  /// Extracts every endpoint type in the assemblies an <c>[assembly: NuruEndpointAssembly]</c> names.
  /// </summary>
  private static ImmutableArray<EndpointWithLocationAndDiagnostics?> ExtractAssemblyEndpoints
  (
    GeneratorAttributeSyntaxContext context,
    CancellationToken cancellationToken
  )
  {
    Compilation compilation = context.SemanticModel.Compilation;
    ImmutableArray<EndpointWithLocationAndDiagnostics?>.Builder endpoints =
      ImmutableArray.CreateBuilder<EndpointWithLocationAndDiagnostics?>();

    foreach ((INamedTypeSymbol type, Location location) in
             ReferencedEndpointLocator.FromAssemblyAttributes(compilation, context.Attributes, cancellationToken))
    {
      endpoints.Add(ExtractReferencedEndpoint(type, compilation, location, isExplicitOnly: false, cancellationToken));
    }

    return endpoints.ToImmutable();
  }

  /// <summary>
  /// Extracts the referenced-assembly endpoint type of a <c>Map&lt;T&gt;()</c> call.
  /// </summary>
  private static EndpointWithLocationAndDiagnostics? ExtractMappedReferencedEndpoint
  (
    GeneratorSyntaxContext context,
    CancellationToken cancellationToken
  )
  {
    if (context.Node is not InvocationExpressionSyntax invocation)
      return null;

    INamedTypeSymbol? type = ReferencedEndpointLocator.MapTypeArgument(invocation, context.SemanticModel, cancellationToken);
    if (type is null)
      return null;

    return ExtractReferencedEndpoint(
      type,
      context.SemanticModel.Compilation,
      invocation.GetLocation(),
      isExplicitOnly: true,
      cancellationToken);
  }

  /// <summary>
  /// Extracts a referenced-assembly endpoint type. Diagnostics point at the opt-in site.
  /// </summary>
  private static EndpointWithLocationAndDiagnostics? ExtractReferencedEndpoint
  (
    INamedTypeSymbol type,
    Compilation compilation,
    Location location,
    bool isExplicitOnly,
    CancellationToken cancellationToken
  )
  {
    EndpointExtractionResult result = EndpointExtractor.Extract(type, compilation, location, cancellationToken);
    if (result.Route is null)
    {
      return result.HasDiagnostics
        ? new EndpointWithLocationAndDiagnostics(null, location, null, result.Diagnostics)
        : null;
    }

    RouteDefinition route = result.Route with { IsExplicitOnly = isExplicitOnly };
    return new EndpointWithLocationAndDiagnostics(route.EffectivePattern, location, route, result.Diagnostics);
  }
}
