#region Purpose
// Property binding derivation for [Operation] endpoints (EndpointExtractor partial).
#endregion

namespace TimeWarp.Nuru.Generators;

internal static partial class EndpointExtractor
{
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
    ResponseShape shape = DescribeResponse(compilation, classSymbol);

    return HandlerDefinition.ForMediator(
      commandTypeName: commandTypeName,
      propertyBindings: bindings,
      returnType: returnType,
      handlerVisible: handlerVisible,
      handlerInterfaceName: shape.HandlerInterfaceName,
      returnsProblemPair: shape.ReturnsProblemPair,
      successTypeName: shape.SuccessTypeName,
      rendererTypeName: shape.RendererTypeName,
      constructorArguments: constructorArguments,
      problemRendererTypeName: shape.ProblemRendererTypeName);
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
        IsRequiredMember: property.IsRequired,
        JsonStringTypes: FindJsonStringTypes(property.Type));
    }

    if (isBool && defaultLiteral == "true")
    {
      return BoolValueOption(property, position, description, new PropertyDefault("true", "true"), isOptional: true, parameterIsOptional: false);
    }

    // A referenced-assembly bool documented [DefaultValue(true)] cannot be a flag either.
    // Its compiled initializer still runs when the caller omits it.
    if (isBool && defaultLiteral == UnemittableDefaultLiteral && propertyDefault.Display == "true")
    {
      return BoolValueOption(property, position, description, propertyDefault, isOptional: true, parameterIsOptional: false);
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
        ? propertyDefault.Display
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
}
