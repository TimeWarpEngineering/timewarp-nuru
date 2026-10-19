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
// An absent [NuruRoute] description keeps the [Operation] description. Empty clears it.
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
    string? HandlerInterfaceName);

  private static bool IsMetadataAttribute(AttributeData attribute, string metadataName) =>
    attribute.AttributeClass?.ToDisplayString() == metadataName;

  /// <summary>
  /// Reads [NuruRoute] and [Operation] by metadata name.
  /// A [NuruRoute] pattern and description override the operation when both are present.
  /// </summary>
  private static RouteIdentity ResolveRouteIdentity(
    TypeDeclarationSyntax classDeclaration,
    SemanticModel semanticModel,
    CancellationToken cancellationToken)
  {
    INamedTypeSymbol? classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken);
    if (classSymbol is null)
      return new RouteIdentity(null, null, Location.None, false, false);

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
        routeLocation = AttributeLocation(attribute, classDeclaration);
      }
      else if (IsMetadataAttribute(attribute, OperationAttributeMetadataName))
      {
        hasOperation = true;
        if (attribute.ConstructorArguments.Length > 0)
          operationName = attribute.ConstructorArguments[0].Value as string;
        operationDescription = NamedString(attribute, "Description");
        operationGroup = NamedString(attribute, "Group");
        operationLocation = AttributeLocation(attribute, classDeclaration);
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
  private static GroupInfo ApplyCliBindingGroup(
    GroupInfo groupInfo,
    TypeDeclarationSyntax classDeclaration,
    SemanticModel semanticModel,
    CancellationToken cancellationToken)
  {
    if (!string.IsNullOrEmpty(groupInfo.FullPrefix))
      return groupInfo;

    INamedTypeSymbol? classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken);
    if (classSymbol is null)
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

  private static ImmutableArray<ParameterBinding> BuildPropertyBindings(
    INamedTypeSymbol classSymbol,
    bool hasOperation,
    Compilation? compilation,
    CancellationToken cancellationToken)
  {
    ImmutableArray<ParameterBinding>.Builder propertyBindings = ImmutableArray.CreateBuilder<ParameterBinding>();

    foreach (IPropertySymbol property in classSymbol.GetMembers().OfType<IPropertySymbol>())
    {
      if (property.SetMethod is null && property.IsReadOnly)
        continue;

      foreach (AttributeData attribute in property.GetAttributes())
      {
        if (IsMetadataAttribute(attribute, ParameterAttributeMetadataName))
        {
          string typeName = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
          bool isCatchAll = attribute.NamedArguments.Any(named =>
            named.Key == "IsCatchAll" && named.Value.Value is true);

          if (isCatchAll)
          {
            propertyBindings.Add(ParameterBinding.FromCatchAll(
              parameterName: property.Name,
              typeName: typeName,
              segmentName: property.Name.ToLowerInvariant()));
          }
          else
          {
            propertyBindings.Add(ParameterBinding.FromParameter(
              parameterName: property.Name,
              typeName: typeName,
              segmentName: property.Name.ToLowerInvariant(),
              isOptional: property.NullableAnnotation == NullableAnnotation.Annotated,
              requiresConversion: typeName != "global::System.String"));
          }

          break;
        }

        if (IsMetadataAttribute(attribute, OptionAttributeMetadataName))
        {
          string typeName = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
          string optionLongForm = ExtractOptionLongForm(attribute, property.Name);
          if (typeName is "bool" or "global::System.Boolean")
          {
            propertyBindings.Add(ParameterBinding.FromFlag(property.Name, optionLongForm));
          }
          else
          {
            propertyBindings.Add(ParameterBinding.FromOption(
              parameterName: property.Name,
              typeName: typeName,
              optionName: optionLongForm,
              isOptional: true,
              isArray: IsRepeatedOptionType(property.Type),
              requiresConversion: typeName != "global::System.String",
              isEnumType: HandlerExtractor.IsEnumBindableType(property.Type)));
          }

          break;
        }
      }
    }

    if (hasOperation)
      AddDerivedPropertyBindings(classSymbol, propertyBindings, compilation, cancellationToken);

    ExtractGroupOptionBindingsFromBaseClasses(classSymbol, propertyBindings);
    return propertyBindings.ToImmutable();
  }

  private static HandlerDefinition CreateMediatorHandler(
    INamedTypeSymbol classSymbol,
    string messageType,
    bool handlerVisible,
    Compilation compilation,
    CancellationToken cancellationToken,
    EquatableArray<string> constructorArguments = default)
  {
    string commandTypeName = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    ImmutableArray<ParameterBinding> bindings = BuildPropertyBindings(classSymbol, hasOperation: true, compilation, cancellationToken);
    HandlerReturnType returnType = InferReturnTypeFromInterfaces(classSymbol);
    ResponseShape shape = DescribeResponse(compilation, classSymbol, messageType);

    return HandlerDefinition.ForMediator(
      commandTypeName: commandTypeName,
      propertyBindings: bindings,
      returnType: returnType,
      handlerVisible: handlerVisible,
      handlerInterfaceName: shape.HandlerInterfaceName,
      returnsProblemPair: shape.ReturnsProblemPair,
      successTypeName: shape.SuccessTypeName,
      rendererTypeName: shape.RendererTypeName,
      constructorArguments: constructorArguments);
  }

  private static void AddDerivedPropertyBindings(
    INamedTypeSymbol classSymbol,
    ImmutableArray<ParameterBinding>.Builder propertyBindings,
    Compilation? compilation,
    CancellationToken cancellationToken)
  {
    for (INamedTypeSymbol? type = classSymbol;
         type is not null && type.SpecialType != SpecialType.System_Object;
         type = type.BaseType)
    {
      // Group options stay on ExtractGroupOptionBindingsFromBaseClasses.
      if (!SymbolEqualityComparer.Default.Equals(type, classSymbol) && HasNuruRouteGroup(type))
        continue;

      foreach (IPropertySymbol property in type.GetMembers().OfType<IPropertySymbol>())
      {
        if (!IsDerivableProperty(property) || HasCliMemberAttribute(property))
          continue;

        if (propertyBindings.Any(binding => binding.ParameterName == property.Name))
          continue;

        ParameterBinding? binding = TryDeriveBinding(property, compilation, cancellationToken);
        if (binding is not null)
          propertyBindings.Add(binding);
      }
    }
  }

  private static bool HasNuruRouteGroup(INamedTypeSymbol type)
  {
    foreach (AttributeData attribute in type.GetAttributes())
    {
      if (IsMetadataAttribute(attribute, NuruRouteGroupAttributeMetadataName))
        return true;
    }

    return false;
  }

  private static OptionDefinition? TryDeriveSegment(
    IPropertySymbol property,
    int position,
    Compilation? compilation,
    CancellationToken cancellationToken)
  {
    if (!IsDerivableProperty(property) || HasCliMemberAttribute(property))
      return null;

    string typeName = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    string? description = DescriptionFromAttribute(property);
    PropertyDefault propertyDefault = ExtractPropertyDefaultValueFromSymbol(property, compilation, cancellationToken);
    string? defaultLiteral = propertyDefault.Code;
    bool initOnly = PropertyIsInitOnly(property);
    bool nullable = property.NullableAnnotation == NullableAnnotation.Annotated;
    bool contractRequired = property.IsRequired || (!nullable && defaultLiteral is null);
    bool isBool = property.Type.SpecialType == SpecialType.System_Boolean;
    bool isNullableBool = IsNullableOf(property.Type, SpecialType.System_Boolean);
    bool json = IsJsonMember(property.Type);

    if (json)
    {
      return new OptionDefinition(
        Position: position,
        LongForm: property.Name,
        ShortForm: null,
        ParameterName: property.Name.ToLowerInvariant(),
        TypeConstraint: null,
        Description: description,
        ExpectsValue: true,
        IsOptional: true,
        IsRepeated: false,
        ParameterIsOptional: nullable,
        ResolvedClrTypeName: typeName,
        DefaultValueLiteral: defaultLiteral,
        IsJsonValue: true,
        CapabilityRequired: contractRequired,
        CapabilityType: JsonCapabilityType(property.Type),
        IsInitOnly: initOnly,
        CapabilityDefaultValue: propertyDefault.Display,
        IsRequiredMember: property.IsRequired);
    }

    if (isBool && defaultLiteral == "true")
    {
      return BoolValueOption(property, position, description, new PropertyDefault("true", "true"), isOptional: true, parameterIsOptional: false);
    }

    if (isBool && defaultLiteral is null && contractRequired)
    {
      return BoolValueOption(property, position, description, default, isOptional: false, parameterIsOptional: false);
    }

    if (isNullableBool)
    {
      return new OptionDefinition(
        Position: position,
        LongForm: property.Name,
        ShortForm: null,
        ParameterName: property.Name.ToLowerInvariant(),
        TypeConstraint: "bool?",
        Description: description,
        ExpectsValue: true,
        IsOptional: true,
        IsRepeated: false,
        ParameterIsOptional: true,
        ResolvedClrTypeName: typeName,
        DefaultValueLiteral: defaultLiteral,
        CapabilityRequired: false,
        IsInitOnly: initOnly,
        CapabilityDefaultValue: propertyDefault.Display,
        IsRequiredMember: property.IsRequired);
    }

    if (isBool)
    {
      string? flagDisplay = defaultLiteral == UnemittableDefaultLiteral
        ? null
        : propertyDefault.Display ?? "false";
      return new OptionDefinition(
        Position: position,
        LongForm: property.Name,
        ShortForm: null,
        ParameterName: null,
        TypeConstraint: null,
        Description: description,
        ExpectsValue: false,
        IsOptional: true,
        IsRepeated: false,
        ParameterIsOptional: false,
        ResolvedClrTypeName: typeName,
        DefaultValueLiteral: defaultLiteral ?? "false",
        IsInitOnly: initOnly,
        CapabilityDefaultValue: flagDisplay,
        IsRequiredMember: property.IsRequired);
    }

    bool repeated = IsRepeatedOptionType(property.Type);
    string? typeConstraint = repeated
      ? RepeatedElementConstraint(property.Type)
      : GetTypeConstraintFromClrType(typeName);
    return new OptionDefinition(
      Position: position,
      LongForm: property.Name,
      ShortForm: null,
      ParameterName: property.Name.ToLowerInvariant(),
      TypeConstraint: typeConstraint,
      Description: description,
      ExpectsValue: true,
      IsOptional: !contractRequired,
      IsRepeated: repeated,
      ParameterIsOptional: nullable,
      ResolvedClrTypeName: typeName,
      DefaultValueLiteral: defaultLiteral,
      CapabilityRequired: contractRequired,
      IsInitOnly: initOnly,
      CapabilityDefaultValue: propertyDefault.Display,
      IsRequiredMember: property.IsRequired);
  }

  private static OptionDefinition BoolValueOption(
    IPropertySymbol property,
    int position,
    string? description,
    PropertyDefault propertyDefault,
    bool isOptional,
    bool parameterIsOptional)
  {
    string typeName = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    return new OptionDefinition(
      Position: position,
      LongForm: property.Name,
      ShortForm: null,
      ParameterName: property.Name.ToLowerInvariant(),
      TypeConstraint: "bool",
      Description: description,
      ExpectsValue: true,
      IsOptional: isOptional,
      IsRepeated: false,
      ParameterIsOptional: parameterIsOptional,
      ResolvedClrTypeName: typeName,
      DefaultValueLiteral: propertyDefault.Code,
      CapabilityRequired: !isOptional,
      IsInitOnly: PropertyIsInitOnly(property),
      CapabilityDefaultValue: propertyDefault.Display,
      IsRequiredMember: property.IsRequired);
  }

  private static ParameterBinding? TryDeriveBinding(
    IPropertySymbol property,
    Compilation? compilation,
    CancellationToken cancellationToken)
  {
    if (TryDeriveSegment(property, 0, compilation, cancellationToken) is not OptionDefinition option)
      return null;

    string typeName = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    if (option.IsFlag)
      return ParameterBinding.FromFlag(property.Name, option.LongForm ?? property.Name);

    return ParameterBinding.FromOption(
      parameterName: property.Name,
      typeName: typeName,
      optionName: option.LongForm ?? property.Name,
      isOptional: option.IsOptional,
      isArray: option.IsRepeated,
      requiresConversion: typeName != "global::System.String",
      isEnumType: HandlerExtractor.IsEnumBindableType(property.Type));
  }

  private static ResponseShape DescribeResponse(Compilation compilation, INamedTypeSymbol requestType, string messageType)
  {
    string commandTypeName = requestType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    ITypeSymbol? responseType = MediatorResponseType(requestType);
    string responseTypeName = responseType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
      ?? "global::TimeWarp.Mediator.Unit";

    bool problem = false;
    string successTypeName = responseTypeName;
    if (responseType is INamedTypeSymbol oneOf
        && oneOf.Name == "OneOf"
        && oneOf.ContainingNamespace?.ToDisplayString() == "OneOf"
        && oneOf.TypeArguments.Length == 2
        && oneOf.TypeArguments[1] is INamedTypeSymbol problemType
        && problemType.Name == "SharedProblemDetails"
        && problemType.ContainingNamespace?.ToDisplayString() == "TimeWarp.Foundation.Types")
    {
      problem = true;
      successTypeName = oneOf.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    string handlerInterface = messageType switch
    {
      "Query" => $"global::TimeWarp.Mediator.IQueryHandler<{commandTypeName}, {responseTypeName}>",
      "IdempotentCommand" => $"global::TimeWarp.Mediator.IIdempotentCommandHandler<{commandTypeName}, {responseTypeName}>",
      _ => $"global::TimeWarp.Mediator.ICommandHandler<{commandTypeName}, {responseTypeName}>"
    };

    string? renderer = FindRenderer(compilation, successTypeName, requestType);
    return new ResponseShape(problem, successTypeName, renderer, handlerInterface);
  }

  private static ITypeSymbol? MediatorResponseType(INamedTypeSymbol requestType)
  {
    ITypeSymbol? idempotent = null;
    ITypeSymbol? command = null;
    foreach (INamedTypeSymbol iface in requestType.AllInterfaces)
    {
      if (!iface.IsGenericType || !IsMediatorInterface(iface) || iface.TypeArguments.Length == 0)
        continue;

      if (iface.Name == "IQuery")
        return iface.TypeArguments[0];
      if (iface.Name == "IIdempotentCommand")
        idempotent = iface.TypeArguments[0];
      else if (iface.Name == "ICommand")
        command = iface.TypeArguments[0];
    }

    return idempotent ?? command;
  }

  private sealed class MediatorScan
  {
    public required List<(INamedTypeSymbol Handler, INamedTypeSymbol Request)> Handlers { get; init; }

    public required List<(INamedTypeSymbol Renderer, string SuccessTypeName, bool SameAssembly)> Renderers { get; init; }
  }

  private static readonly global::System.Runtime.CompilerServices.ConditionalWeakTable<Compilation, MediatorScan> MediatorScans = [];

  private static MediatorScan ScanMediator(Compilation compilation) =>
    MediatorScans.GetValue(compilation, static compilation => BuildMediatorScan(compilation));

  private static MediatorScan BuildMediatorScan(Compilation compilation)
  {
    List<(INamedTypeSymbol Handler, INamedTypeSymbol Request)> handlers = [];
    List<(INamedTypeSymbol Renderer, string SuccessTypeName, bool SameAssembly)> renderers = [];
    CollectMediatorTypes(compilation.Assembly.GlobalNamespace, publicOnly: false, sameAssembly: true, handlers, renderers);
    foreach (MetadataReference reference in compilation.References)
    {
      if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assembly)
        continue;

      if (IsFrameworkAssembly(assembly.Name))
        continue;

      CollectMediatorTypes(assembly.GlobalNamespace, publicOnly: true, sameAssembly: false, handlers, renderers);
    }

    return new MediatorScan { Handlers = handlers, Renderers = renderers };
  }

  private static bool IsFrameworkAssembly(string? name) =>
    string.IsNullOrEmpty(name)
    || name.StartsWith("System", StringComparison.Ordinal)
    || name.StartsWith("Microsoft", StringComparison.Ordinal)
    || name is "mscorlib" or "netstandard";

  private static void CollectMediatorTypes(
    INamespaceSymbol ns,
    bool publicOnly,
    bool sameAssembly,
    List<(INamedTypeSymbol Handler, INamedTypeSymbol Request)> handlers,
    List<(INamedTypeSymbol Renderer, string SuccessTypeName, bool SameAssembly)> renderers)
  {
    foreach (INamedTypeSymbol type in ns.GetTypeMembers())
      CollectMediatorType(type, publicOnly, sameAssembly, handlers, renderers);

    foreach (INamespaceSymbol child in ns.GetNamespaceMembers())
      CollectMediatorTypes(child, publicOnly, sameAssembly, handlers, renderers);
  }

  private static void CollectMediatorType(
    INamedTypeSymbol type,
    bool publicOnly,
    bool sameAssembly,
    List<(INamedTypeSymbol Handler, INamedTypeSymbol Request)> handlers,
    List<(INamedTypeSymbol Renderer, string SuccessTypeName, bool SameAssembly)> renderers)
  {
    if (IsAccessibleHandler(type, publicOnly))
    {
      foreach (INamedTypeSymbol iface in type.AllInterfaces)
      {
        if (iface.ContainingNamespace?.ToDisplayString() != MediatorNamespace)
          continue;

        if (iface.Name is not ("IQueryHandler" or "ICommandHandler" or "IIdempotentCommandHandler"))
          continue;

        if (iface.TypeArguments.Length > 0 && iface.TypeArguments[0] is INamedTypeSymbol request)
          handlers.Add((type, request));
      }
    }

    foreach (string successTypeName in RendererSuccessTypes(type, publicOnly))
      renderers.Add((type, successTypeName, sameAssembly));

    foreach (INamedTypeSymbol nested in type.GetTypeMembers())
      CollectMediatorType(nested, publicOnly, sameAssembly, handlers, renderers);
  }

  private static bool HasVisibleHandler(Compilation compilation, INamedTypeSymbol requestType)
  {
    INamedTypeSymbol? nestedHandler = requestType.GetTypeMembers("Handler").FirstOrDefault();
    if (nestedHandler is not null && ImplementsRequestHandler(nestedHandler, requestType))
      return true;

    foreach ((INamedTypeSymbol _, INamedTypeSymbol request) in ScanMediator(compilation).Handlers)
    {
      if (SymbolEqualityComparer.Default.Equals(request, requestType))
        return true;
    }

    return false;
  }

  private static bool IsAccessibleHandler(INamedTypeSymbol type, bool publicOnly)
  {
    if (type.TypeKind != TypeKind.Class || type.IsAbstract)
      return false;

    if (publicOnly)
      return type.DeclaredAccessibility == Accessibility.Public;

    return type.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal;
  }

  private static bool ImplementsRequestHandler(INamedTypeSymbol type, INamedTypeSymbol requestType)
  {
    foreach (INamedTypeSymbol iface in type.AllInterfaces)
    {
      if (iface.ContainingNamespace?.ToDisplayString() != MediatorNamespace)
        continue;

      if (iface.Name is not ("IQueryHandler" or "ICommandHandler" or "IIdempotentCommandHandler"))
        continue;

      if (iface.TypeArguments.Length > 0
          && SymbolEqualityComparer.Default.Equals(iface.TypeArguments[0], requestType))
      {
        return true;
      }
    }

    return false;
  }

  private static string? FindRenderer(Compilation? compilation, string successTypeName, INamedTypeSymbol requestType)
  {
    _ = requestType;
    if (compilation is null)
      return null;

    List<INamedTypeSymbol> sameAssembly = [];
    List<INamedTypeSymbol> referenced = [];
    foreach ((INamedTypeSymbol renderer, string success, bool same) in ScanMediator(compilation).Renderers)
    {
      if (success != successTypeName)
        continue;

      if (same)
        sameAssembly.Add(renderer);
      else
        referenced.Add(renderer);
    }

    if (sameAssembly.Count == 1)
      return sameAssembly[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    if (sameAssembly.Count > 1)
      return null;

    if (referenced.Count == 1 && HasParameterlessConstructor(referenced[0]))
      return referenced[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    return null;
  }

  private static List<string> RendererSuccessTypes(INamedTypeSymbol type, bool publicOnly)
  {
    List<string> successTypes = [];
    if (type.TypeKind != TypeKind.Class || type.IsAbstract || !HasParameterlessConstructor(type))
      return successTypes;

    if (publicOnly && type.DeclaredAccessibility != Accessibility.Public)
      return successTypes;

    if (!publicOnly && type.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal))
      return successTypes;

    foreach (INamedTypeSymbol iface in type.AllInterfaces)
    {
      if (iface.Name != "IResponseRenderer" || !iface.IsGenericType || iface.TypeArguments.Length != 1)
        continue;

      if (iface.ContainingNamespace?.ToDisplayString() != "TimeWarp.Contracts")
        continue;

      successTypes.Add(iface.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
    }

    return successTypes;
  }

  private static bool HasParameterlessConstructor(INamedTypeSymbol type) =>
    type.InstanceConstructors.Any(constructor =>
      constructor.Parameters.Length == 0
      && constructor.DeclaredAccessibility == Accessibility.Public);

  private static bool IsDerivableProperty(IPropertySymbol property)
  {
    if (property.IsStatic || property.IsIndexer)
      return false;

    if (property.DeclaredAccessibility != Accessibility.Public)
      return false;

    return property.SetMethod is { DeclaredAccessibility: Accessibility.Public };
  }

  private static readonly SymbolDisplayFormat OmittedGlobalFormat =
    SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted);

  private static string JsonCapabilityType(ITypeSymbol type) =>
    "json " + type.ToDisplayString(OmittedGlobalFormat);

  private static bool TrySelectConstructorArguments(
    INamedTypeSymbol type,
    Location location,
    bool deriveUnmarked,
    out EquatableArray<string> arguments,
    out Diagnostic? diagnostic)
  {
    arguments = default;
    diagnostic = null;
    string? kindReason = type switch
    {
      { IsStatic: true } => "it is static. An endpoint must be a concrete class or struct.",
      { IsAbstract: true } => "it is abstract. Put the attribute on a concrete type.",
      { TypeKind: not (TypeKind.Class or TypeKind.Struct) } => "it is not a class or struct.",
      _ => null
    };
    if (kindReason is not null)
    {
      diagnostic = Unconstructable(type, location, kindReason);
      return false;
    }

    if (HasCallableParameterlessConstructor(type))
      return true;

    IMethodSymbol? best = null;
    List<string>? bestNames = null;
    foreach (IMethodSymbol constructor in type.InstanceConstructors)
    {
      if (!IsAccessibleConstructor(constructor))
        continue;

      if (IsCopyConstructor(type, constructor))
        continue;

      if (!ConstructorParametersMatchProperties(type, constructor, deriveUnmarked, out List<string> names))
        continue;

      if (best is null || constructor.Parameters.Length > best.Parameters.Length)
      {
        best = constructor;
        bestNames = names;
      }
    }

    if (bestNames is null)
    {
      diagnostic = Unconstructable(
        type,
        location,
        "it has no accessible parameterless constructor and no constructor whose parameters all match, ignoring case, public settable properties bound as route parameters or options.");
      return false;
    }

    arguments = [.. bestNames];
    return true;
  }

  private static Diagnostic Unconstructable(INamedTypeSymbol type, Location location, string reason) =>
    Diagnostic.Create(DiagnosticDescriptors.UnconstructableEndpoint, location, type.Name, reason);

  private static bool IsAccessibleConstructor(IMethodSymbol constructor) =>
    constructor.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedOrInternal;

  // new() compiles when every parameter is optional or params, so that counts as parameterless.
  private static bool HasCallableParameterlessConstructor(INamedTypeSymbol type) =>
    type.InstanceConstructors.Any(constructor =>
      IsAccessibleConstructor(constructor)
      && constructor.Parameters.All(parameter => parameter.IsOptional || parameter.IsParams));

  private static bool IsCopyConstructor(INamedTypeSymbol type, IMethodSymbol constructor) =>
    constructor.Parameters.Length == 1
    && SymbolEqualityComparer.Default.Equals(constructor.Parameters[0].Type, type);

  private static bool ConstructorParametersMatchProperties(
    INamedTypeSymbol type,
    IMethodSymbol constructor,
    bool deriveUnmarked,
    out List<string> propertyNames)
  {
    propertyNames = [];
    if (constructor.Parameters.Length == 0)
      return false;

    foreach (IParameterSymbol parameter in constructor.Parameters)
    {
      IPropertySymbol? match = FindPropertyByName(type, parameter.Name);
      if (match is null || !IsConstructorBoundProperty(type, match, deriveUnmarked))
        return false;

      propertyNames.Add(match.Name);
    }

    return true;
  }

  /// <summary>
  /// True when the property gets a route binding, so a constructor argument has a value.
  /// A [NuruRoute] type binds only marked properties. An [Operation] type also derives
  /// unmarked ones, except from a [NuruRouteGroup] base.
  /// </summary>
  private static bool IsConstructorBoundProperty(INamedTypeSymbol type, IPropertySymbol property, bool deriveUnmarked)
  {
    if (!IsDerivableProperty(property))
      return false;

    if (HasCliMemberAttribute(property))
      return true;

    if (!deriveUnmarked)
      return false;

    return SymbolEqualityComparer.Default.Equals(property.ContainingType, type)
      || !HasNuruRouteGroup(property.ContainingType);
  }

  private static IPropertySymbol? FindPropertyByName(INamedTypeSymbol type, string name)
  {
    for (INamedTypeSymbol? current = type;
         current is not null && current.SpecialType != SpecialType.System_Object;
         current = current.BaseType)
    {
      foreach (IPropertySymbol property in current.GetMembers().OfType<IPropertySymbol>())
      {
        if (property.IsStatic)
          continue;

        if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
          return property;
      }
    }

    return null;
  }

  private static bool HasCliMemberAttribute(IPropertySymbol property)
  {
    foreach (AttributeData attribute in property.GetAttributes())
    {
      if (IsMetadataAttribute(attribute, ParameterAttributeMetadataName)
          || IsMetadataAttribute(attribute, OptionAttributeMetadataName)
          || IsMetadataAttribute(attribute, GroupOptionAttributeMetadataName))
      {
        return true;
      }
    }

    return false;
  }

  private static string? DescriptionFromAttribute(IPropertySymbol property)
  {
    foreach (AttributeData attribute in property.GetAttributes())
    {
      if (!IsMetadataAttribute(attribute, DescriptionAttributeMetadataName))
        continue;

      if (attribute.ConstructorArguments.Length > 0)
        return attribute.ConstructorArguments[0].Value as string;
    }

    return null;
  }

  private static bool IsJsonMember(ITypeSymbol type)
  {
    if (IsScalar(type))
      return false;

    if (IsRepeatedOptionType(type))
    {
      ITypeSymbol? element = ElementType(type);
      return element is null || !IsScalar(element);
    }

    return true;
  }

  /// <summary>
  /// Type constraint of a repeated scalar element (int for int[], null for string[]).
  /// The repeated emitter reads this the same way a fluent <c>{id:int}*</c> option does.
  /// </summary>
  private static string? RepeatedElementConstraint(ITypeSymbol type)
  {
    ITypeSymbol? element = ElementType(type);
    if (element is null)
      return null;

    return GetTypeConstraintFromClrType(element.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
  }

  private static bool IsScalar(ITypeSymbol type)
  {
    if (type is INamedTypeSymbol named && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
      return named.TypeArguments.Length == 1 && IsScalar(named.TypeArguments[0]);

    if (type.TypeKind == TypeKind.Enum)
      return true;

    if (type.SpecialType is SpecialType.System_Boolean
        or SpecialType.System_Char
        or SpecialType.System_String
        or SpecialType.System_Byte
        or SpecialType.System_SByte
        or SpecialType.System_Int16
        or SpecialType.System_UInt16
        or SpecialType.System_Int32
        or SpecialType.System_UInt32
        or SpecialType.System_Int64
        or SpecialType.System_UInt64
        or SpecialType.System_Single
        or SpecialType.System_Double
        or SpecialType.System_Decimal
        or SpecialType.System_DateTime)
    {
      return true;
    }

    string display = type.ToDisplayString();
    return display is "System.DateTimeOffset"
      or "System.DateOnly"
      or "System.TimeOnly"
      or "System.TimeSpan"
      or "System.Guid"
      or "System.Uri"
      or "System.Version"
      or "System.Numerics.BigInteger";
  }

  private static ITypeSymbol? ElementType(ITypeSymbol type)
  {
    if (type is IArrayTypeSymbol array)
      return array.ElementType;

    if (type is INamedTypeSymbol named && named.TypeArguments.Length == 1)
      return named.TypeArguments[0];

    return null;
  }

  private static bool IsNullableOf(ITypeSymbol type, SpecialType specialType) =>
    type is INamedTypeSymbol named
    && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
    && named.TypeArguments.Length == 1
    && named.TypeArguments[0].SpecialType == specialType;

  private static string? NamedString(AttributeData attribute, string name)
  {
    foreach (KeyValuePair<string, TypedConstant> named in attribute.NamedArguments)
    {
      if (named.Key == name)
        return named.Value.Value as string;
    }

    return null;
  }

  private static Location AttributeLocation(AttributeData attribute, TypeDeclarationSyntax fallback)
  {
    SyntaxReference? reference = attribute.ApplicationSyntaxReference;
    if (reference is null)
      return fallback.Identifier.GetLocation();

    return reference.GetSyntax().GetLocation();
  }
}
