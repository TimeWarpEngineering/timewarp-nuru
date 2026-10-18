// Derives a Nuru endpoint from TimeWarp.Contracts.OperationAttribute.
//
// A request that carries [Operation] and a TimeWarp.Mediator kind is a complete endpoint.
// The route literal is the operation name. Each public settable member is an option named
// after the property. [NuruRoute], [Parameter], and [Option] stay CLI overrides.
//
// Group path: a containing [NuruRouteGroup] wins, otherwise [NuruCliBinding(Group = "...")],
// otherwise the endpoint is ungrouped. TimeWarp.Contracts CliOperationBinding has Pattern only.

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
    bool HasOperation);

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
    ClassDeclarationSyntax classDeclaration,
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
        operationLocation = AttributeLocation(attribute, classDeclaration);
      }
    }

    if (hasNuruRoute)
      return new RouteIdentity(routePattern, routeDescription, routeLocation, true, hasOperation);

    if (hasOperation)
      return new RouteIdentity(operationName, operationDescription, operationLocation, false, true);

    return new RouteIdentity(null, null, Location.None, false, false);
  }

  /// <summary>
  /// Applies [NuruCliBinding.Group] when no [NuruRouteGroup] prefix is present.
  /// </summary>
  private static GroupInfo ApplyCliBindingGroup(
    GroupInfo groupInfo,
    ClassDeclarationSyntax classDeclaration,
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
    CancellationToken cancellationToken)
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
      rendererTypeName: shape.RendererTypeName);
  }

  private static void AddDerivedPropertyBindings(
    INamedTypeSymbol classSymbol,
    ImmutableArray<ParameterBinding>.Builder propertyBindings,
    Compilation? compilation,
    CancellationToken cancellationToken)
  {
    foreach (IPropertySymbol property in classSymbol.GetMembers().OfType<IPropertySymbol>())
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
    string? defaultLiteral = ExtractPropertyDefaultValueFromSymbol(property, compilation, cancellationToken);
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
        CapabilityType: "json " + typeName);
    }

    if (isBool && defaultLiteral == "true")
    {
      return BoolValueOption(property, position, description, "true", isOptional: true, parameterIsOptional: false);
    }

    if (isBool && defaultLiteral is null && contractRequired)
    {
      return BoolValueOption(property, position, description, null, isOptional: false, parameterIsOptional: false);
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
        CapabilityRequired: false);
    }

    if (isBool)
    {
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
        DefaultValueLiteral: defaultLiteral ?? "false");
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
      CapabilityRequired: contractRequired);
  }

  private static OptionDefinition BoolValueOption(
    IPropertySymbol property,
    int position,
    string? description,
    string? defaultLiteral,
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
      DefaultValueLiteral: defaultLiteral,
      CapabilityRequired: !isOptional);
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

  private static bool HasVisibleHandler(Compilation compilation, INamedTypeSymbol requestType)
  {
    INamedTypeSymbol? nestedHandler = requestType.GetTypeMembers("Handler").FirstOrDefault();
    if (nestedHandler is not null && ImplementsRequestHandler(nestedHandler, requestType))
      return true;

    if (NamespaceHasHandler(compilation.Assembly.GlobalNamespace, requestType, publicOnly: false))
      return true;

    foreach (MetadataReference reference in compilation.References)
    {
      if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assembly)
        continue;

      if (assembly.Name.StartsWith("System", StringComparison.Ordinal)
          || assembly.Name.StartsWith("Microsoft", StringComparison.Ordinal))
      {
        continue;
      }

      if (NamespaceHasHandler(assembly.GlobalNamespace, requestType, publicOnly: true))
        return true;
    }

    return false;
  }

  private static bool NamespaceHasHandler(INamespaceSymbol ns, INamedTypeSymbol requestType, bool publicOnly)
  {
    foreach (INamedTypeSymbol type in ns.GetTypeMembers())
    {
      if (TypeOrNestedHasHandler(type, requestType, publicOnly))
        return true;
    }

    foreach (INamespaceSymbol child in ns.GetNamespaceMembers())
    {
      if (NamespaceHasHandler(child, requestType, publicOnly))
        return true;
    }

    return false;
  }

  private static bool TypeOrNestedHasHandler(INamedTypeSymbol type, INamedTypeSymbol requestType, bool publicOnly)
  {
    if (IsAccessibleHandler(type, publicOnly) && ImplementsRequestHandler(type, requestType))
      return true;

    foreach (INamedTypeSymbol nested in type.GetTypeMembers())
    {
      if (TypeOrNestedHasHandler(nested, requestType, publicOnly))
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
    if (compilation is null)
      return null;

    List<INamedTypeSymbol> sameAssembly = [];
    CollectRenderers(compilation.Assembly.GlobalNamespace, successTypeName, publicOnly: false, sameAssembly);
    if (sameAssembly.Count == 1)
      return sameAssembly[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    if (sameAssembly.Count > 1)
      return null;

    List<INamedTypeSymbol> referenced = [];
    foreach (MetadataReference reference in compilation.References)
    {
      if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol assembly)
        continue;

      CollectRenderers(assembly.GlobalNamespace, successTypeName, publicOnly: true, referenced);
    }

    _ = requestType;
    if (referenced.Count == 1 && HasParameterlessConstructor(referenced[0]))
      return referenced[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    return null;
  }

  private static void CollectRenderers(
    INamespaceSymbol ns,
    string successTypeName,
    bool publicOnly,
    List<INamedTypeSymbol> matches)
  {
    foreach (INamedTypeSymbol type in ns.GetTypeMembers())
      CollectRendererType(type, successTypeName, publicOnly, matches);

    foreach (INamespaceSymbol child in ns.GetNamespaceMembers())
      CollectRenderers(child, successTypeName, publicOnly, matches);
  }

  private static void CollectRendererType(
    INamedTypeSymbol type,
    string successTypeName,
    bool publicOnly,
    List<INamedTypeSymbol> matches)
  {
    if (IsRenderer(type, successTypeName, publicOnly))
      matches.Add(type);

    foreach (INamedTypeSymbol nested in type.GetTypeMembers())
      CollectRendererType(nested, successTypeName, publicOnly, matches);
  }

  private static bool IsRenderer(INamedTypeSymbol type, string successTypeName, bool publicOnly)
  {
    if (type.TypeKind != TypeKind.Class || type.IsAbstract || !HasParameterlessConstructor(type))
      return false;

    if (publicOnly && type.DeclaredAccessibility != Accessibility.Public)
      return false;

    if (!publicOnly && type.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal))
      return false;

    foreach (INamedTypeSymbol iface in type.AllInterfaces)
    {
      if (iface.Name != "IResponseRenderer" || !iface.IsGenericType || iface.TypeArguments.Length != 1)
        continue;

      if (iface.ContainingNamespace?.ToDisplayString() != "TimeWarp.Contracts")
        continue;

      string argument = iface.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
      if (argument == successTypeName)
        return true;
    }

    return false;
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

  private static Location AttributeLocation(AttributeData attribute, ClassDeclarationSyntax fallback)
  {
    SyntaxReference? reference = attribute.ApplicationSyntaxReference;
    if (reference is null)
      return fallback.Identifier.GetLocation();

    return reference.GetSyntax().GetLocation();
  }
}
