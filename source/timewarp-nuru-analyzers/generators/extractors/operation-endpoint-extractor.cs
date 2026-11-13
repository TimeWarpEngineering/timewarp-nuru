#region Purpose
// Derives a Nuru endpoint from TimeWarp.Contracts.OperationAttribute plus a mediator kind.
#endregion

#region Design
// The route literal is the operation name. Each public settable member is an option,
// including init members and members inherited from a base that is not a [NuruRouteGroup].
// A derived declaration hides the base property of the same name.
// [NuruRoute], [Parameter], and [Option] stay CLI overrides.
// An [Operation] type with no mediator kind is not an endpoint.
// A type with no parameterless constructor is built from the constructor whose
// parameters match property names. A type with neither is NURU_H009 and is skipped.
// JSON capability types omit global:: inside type arguments.
// Group path: a non-blank [Operation(Group)] wins. It is metadata only: it becomes the catalog
// groupPath, split on '.' or space, and adds no route or command prefix. It also replaces any
// [NuruRouteGroup] prefix and [NuruCliBinding(Group)]; the group type hierarchy stays for
// DiscoverEndpoints(typeof(...)) filtering. Otherwise a containing [NuruRouteGroup] prefix wins,
// then [NuruCliBinding(Group)], otherwise the endpoint is ungrouped. CliOperationBinding has
// Pattern only.
// Handler and renderer lookups are cached per compilation and skip framework assemblies.
// A registered IResponseRenderer<T> wins; otherwise one visible parameterless renderer.
// A problem pair looks up IResponseRenderer<SharedProblemDetails> the same way.
// An absent [NuruRoute] description keeps the [Operation] description. Empty clears it.
// A referenced-assembly constructor or nested Handler counts only when this compilation can access it.
// Title is OperationAttribute.Title. Blank or whitespace is no title. [DisplayName] is not a title
// source on an operation. A [NuruRoute] class with no [Operation] still reads [DisplayName].
// Destructive and OpenWorld are TimeWarp.Contracts attributes on every endpoint class. Route
// endpoints use them too, so the short names exist once. Fluent Map() routes have no attribute target.
#endregion

namespace TimeWarp.Nuru.Generators;

internal static partial class EndpointExtractor
{
  private const string OperationAttributeMetadataName = "TimeWarp.Contracts.OperationAttribute";
  private const string NuruRouteAttributeMetadataName = "TimeWarp.Nuru.NuruRouteAttribute";
  private const string NuruRouteGroupAttributeMetadataName = "TimeWarp.Nuru.NuruRouteGroupAttribute";
  private const string NuruCliBindingAttributeMetadataName = "TimeWarp.Nuru.NuruCliBindingAttribute";
  private const string ParameterAttributeMetadataName = "TimeWarp.Nuru.ParameterAttribute";
  private const string OptionAttributeMetadataName = "TimeWarp.Nuru.OptionAttribute";
  private const string GroupOptionAttributeMetadataName = "TimeWarp.Nuru.GroupOptionAttribute";
  private const string DescriptionAttributeMetadataName = "System.ComponentModel.DescriptionAttribute";
  private const string ProblemDetailsMetadataTypeName = "global::TimeWarp.Foundation.Types.SharedProblemDetails";

  private readonly record struct RouteIdentity(
    string? Pattern,
    string? Description,
    Location Location,
    bool HasNuruRoute,
    bool HasOperation,
    string? OperationGroup = null);

  private readonly record struct ResponseShape(
    bool ReturnsProblemPair,
    string? SuccessTypeName,
    string? RendererTypeName,
    string? HandlerInterfaceName,
    string? ProblemRendererTypeName);

  private static bool IsMetadataAttribute(AttributeData attribute, string metadataName) =>
    attribute.AttributeClass?.ToDisplayString() == metadataName;

  /// <summary>
  /// Reads [NuruRoute] and [Operation] by metadata name.
  /// A [NuruRoute] pattern and description override the operation when both are present.
  /// </summary>
  private static RouteIdentity ResolveRouteIdentity(INamedTypeSymbol classSymbol, Compilation compilation, Location fallbackLocation)
  {
    string? routePattern = null;
    string? routeDescription = null;
    Location routeLocation = Location.None;
    bool hasNuruRoute = false;
    string? operationName = null;
    string? operationDescription = null;
    string? operationGroup = null;
    Location operationLocation = Location.None;
    bool hasOperation = false;

    foreach (AttributeData attribute in classSymbol.GetAttributes())
    {
      if (IsMetadataAttribute(attribute, NuruRouteAttributeMetadataName))
      {
        hasNuruRoute = true;
        if (attribute.ConstructorArguments.Length > 0)
          routePattern = attribute.ConstructorArguments[0].Value as string;
        routeDescription = NamedString(attribute, "Description");
        routeLocation = AttributeLocation(attribute, compilation, fallbackLocation);
      }
      else if (IsMetadataAttribute(attribute, OperationAttributeMetadataName))
      {
        hasOperation = true;
        if (attribute.ConstructorArguments.Length > 0)
          operationName = attribute.ConstructorArguments[0].Value as string;
        operationDescription = NamedString(attribute, "Description");
        operationGroup = NamedString(attribute, "Group");
        operationLocation = AttributeLocation(attribute, compilation, fallbackLocation);
      }
    }

    if (hasNuruRoute)
    {
      // An absent [NuruRoute] description keeps the operation description.
      // An explicit empty description still clears it.
      return new RouteIdentity(routePattern, routeDescription ?? operationDescription, routeLocation, true, hasOperation, operationGroup);
    }

    if (hasOperation)
      return new RouteIdentity(operationName, operationDescription, operationLocation, false, true, operationGroup);

    return new RouteIdentity(null, null, Location.None, false, false);
  }

  /// <summary>
  /// Splits [Operation(Group)] into groupPath segments on '.' or space. Blank is ungrouped.
  /// </summary>
  private static EquatableArray<string> SplitOperationGroup(string? group)
  {
    if (string.IsNullOrWhiteSpace(group))
      return [];

    return ImmutableArray.Create(group.Split(['.', ' ', '\t'], StringSplitOptions.RemoveEmptyEntries));
  }

  /// <summary>
  /// Drops the [NuruRouteGroup] route prefix and its aliases so an [Operation(Group)] path is the only group.
  /// </summary>
  private static GroupInfo WithoutRoutePrefix(GroupInfo groupInfo) =>
    groupInfo with { FullPrefix = null, GroupAliases = [], GroupPrefixes = [] };

  /// <summary>
  /// Applies [NuruCliBinding.Group] when no [NuruRouteGroup] prefix is present.
  /// </summary>
  private static GroupInfo ApplyCliBindingGroup(GroupInfo groupInfo, INamedTypeSymbol classSymbol)
  {
    if (!string.IsNullOrEmpty(groupInfo.FullPrefix))
      return groupInfo;

    foreach (AttributeData attribute in classSymbol.GetAttributes())
    {
      if (!IsMetadataAttribute(attribute, NuruCliBindingAttributeMetadataName))
        continue;

      string? group = NamedString(attribute, "Group");
      if (string.IsNullOrWhiteSpace(group))
        return groupInfo;

      return groupInfo with { FullPrefix = group.Trim() };
    }

    return groupInfo;
  }

  private static readonly global::System.Runtime.CompilerServices.ConditionalWeakTable<Compilation, MediatorScan> MediatorScans = [];

  private static readonly SymbolDisplayFormat OmittedGlobalFormat =
    SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted);
}
