#region Purpose
// EndpointExtractor partial: group hierarchy, group aliases and [GroupOption] extraction from base classes.
#endregion

namespace TimeWarp.Nuru.Generators;

internal static partial class EndpointExtractor
{
  /// <summary>
  /// Represents an alias defined at a specific level in the group hierarchy.
  /// </summary>
  /// <param name="Alias">The alias string (e.g., "ws")</param>
  /// <param name="GroupPrefixIndex">The index in the full prefix that this alias replaces (0 = first segment)</param>
  private readonly record struct GroupAliasDefinition(
    string Alias,
    int GroupPrefixIndex);

  /// <summary>
  /// Contains the result of extracting group information from the inheritance hierarchy.
  /// </summary>
  private readonly record struct GroupInfo
  (
    ImmutableArray<string> TypeHierarchy,
    string? FullPrefix,
    ImmutableArray<GroupAliasDefinition> GroupAliases,
    // Per-group prefixes in root-to-leaf order (each may itself be multi-word, e.g. "git remote").
    // GroupAliasDefinition.GroupPrefixIndex indexes into this, so a group alias replaces the whole
    // group's prefix regardless of how many words it contains.
    ImmutableArray<string> GroupPrefixes
  );

  /// <summary>
  /// Extracts group information from base class hierarchy with [NuruRouteGroup] attributes.
  /// Walks the full inheritance chain and:
  /// 1. Collects all group type full names in the chain (TypeHierarchy)
  /// 2. Collects all prefixes
  /// 3. Returns the full concatenated prefix
  ///
  /// This always returns the full hierarchy - filtering by group types happens in
  /// FilterEndpointsForApp after extraction.
  /// </summary>
  private static GroupInfo ExtractGroupInfo(INamedTypeSymbol classSymbol)
  {
    if (classSymbol.BaseType is null)
    {
      return new GroupInfo
      (
        TypeHierarchy: [],
        FullPrefix: null,
        GroupAliases: [],
        GroupPrefixes: []
      );
    }

    // Walk the full inheritance chain from leaf to root, collecting info
    // We'll store in reverse order first, then reverse
    List<string> typeHierarchyReversed = [];
    List<string> prefixesReversed = [];
    List<GroupAliasDefinition> groupAliasesReversed = [];

    INamedTypeSymbol? current = classSymbol.BaseType;
    int groupIndex = 0;  // Only counts classes with [NuruRouteGroup] + non-empty prefix

    while (current is not null && current.SpecialType != SpecialType.System_Object)
    {
      string typeFullName = current.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
      typeHierarchyReversed.Add(typeFullName);

      // Check this type for [NuruRouteGroup] attribute and collect prefix
      string? prefix = null;
      foreach (AttributeData attribute in current.GetAttributes())
      {
        if (!IsMetadataAttribute(attribute, NuruRouteGroupAttributeMetadataName))
          continue;

        // Get the prefix from the first constructor argument
        if (attribute.ConstructorArguments.Length > 0 &&
            attribute.ConstructorArguments[0].Value is string p &&
            !string.IsNullOrEmpty(p))
        {
          prefix = p;
          break;
        }
      }

      prefixesReversed.Add(prefix ?? "");

      // Check this type for [NuruRouteAlias] attribute(s) and collect aliases
      // Use groupIndex which only increments for actual group classes
      if (!string.IsNullOrEmpty(prefix))
      {
        foreach (AttributeData attribute in current.GetAttributes())
        {
          string? attributeName = attribute.AttributeClass?.Name;
          if (attributeName != NuruRouteAliasAttributeName && attributeName != $"{NuruRouteAliasAttributeName}Attribute")
            continue;

          if (attribute.ConstructorArguments.Length > 0)
          {
            TypedConstant argsArray = attribute.ConstructorArguments[0];

            if (argsArray.Values.Length > 0)
            {
              foreach (TypedConstant value in argsArray.Values)
              {
                if (value.Value is string alias)
                  groupAliasesReversed.Add(new GroupAliasDefinition(alias, groupIndex));
              }
            }
            else if (argsArray.Value is string singleAlias)
            {
              groupAliasesReversed.Add(new GroupAliasDefinition(singleAlias, groupIndex));
            }
          }
        }

        groupIndex++;
      }

      current = current.BaseType;
    }

    // Reverse to get root-to-leaf order
    typeHierarchyReversed.Reverse();
    prefixesReversed.Reverse();

    // Recalculate alias indices after reversal
    // groupAliases was collected leaf-to-root, so after reversal:
    // - What was at index N-1 (leaf) is now at index 0 (root)
    // - What was at index 0 (root) is now at index N-1 (leaf)
    // For an alias that was at index i, its new index is (groupIndex - 1 - i)
    List<GroupAliasDefinition> recalculatedAliases = [];
    foreach (GroupAliasDefinition alias in groupAliasesReversed)
    {
      int newIndex = groupIndex - 1 - alias.GroupPrefixIndex;
      recalculatedAliases.Add(new GroupAliasDefinition(alias.Alias, newIndex));
    }

    recalculatedAliases.Reverse();

    ImmutableArray<string> typeHierarchy = [.. typeHierarchyReversed];

    // Build full prefix from all prefixes
    string? fullPrefix = prefixesReversed.Any(p => !string.IsNullOrEmpty(p))
      ? string.Join(" ", prefixesReversed.Where(p => !string.IsNullOrEmpty(p)))
      : null;

    return new GroupInfo
    (
      TypeHierarchy: typeHierarchy,
      FullPrefix: fullPrefix,
      GroupAliases: [.. recalculatedAliases],
      // Non-empty prefixes in root-to-leaf order — the un-joined form of FullPrefix, aligned
      // with GroupAliasDefinition.GroupPrefixIndex (which only counts non-empty group prefixes).
      GroupPrefixes: [.. prefixesReversed.Where(p => !string.IsNullOrEmpty(p))]
    );
  }

  /// <summary>
  /// Extracts GroupOptions from base class hierarchy.
  /// Walks the full inheritance chain and collects options from properties with [GroupOption] attributes.
  /// </summary>
  private static ImmutableArray<SegmentDefinition> ExtractGroupOptionsFromBaseClasses
  (
    INamedTypeSymbol classSymbol,
    Compilation compilation,
    int startPosition,
    CancellationToken cancellationToken
  )
  {
    if (classSymbol.BaseType is null)
      return [];

    ImmutableArray<SegmentDefinition>.Builder options = ImmutableArray.CreateBuilder<SegmentDefinition>();
    int position = startPosition;
    INamedTypeSymbol? current = classSymbol.BaseType;

    while (current is not null && current.SpecialType != SpecialType.System_Object)
    {
      // Check this type for properties with [GroupOption] attributes
      foreach (IPropertySymbol property in current.GetMembers().OfType<IPropertySymbol>())
      {
        foreach (AttributeData attribute in property.GetAttributes())
        {
          string? attributeName = attribute.AttributeClass?.Name;
          if (attributeName != GroupOptionAttributeName && attributeName != $"{GroupOptionAttributeName}Attribute")
            continue;

          OptionDefinition? option = ExtractGroupOptionFromAttribute(property, attribute, position++, compilation, cancellationToken);
          if (option is not null)
            options.Add(option);
        }
      }

      current = current.BaseType;
    }

    return options.ToImmutable();
  }

  /// <summary>
  /// Extracts an OptionDefinition from a [GroupOption] attribute.
  /// Similar to ExtractOptionFromAttribute but for GroupOption.
  /// </summary>
  private static OptionDefinition? ExtractGroupOptionFromAttribute
  (
    IPropertySymbol property,
    AttributeData attribute,
    int position,
    Compilation? compilation,
    CancellationToken cancellationToken
  )
  {
    string longForm = property.Name.ToLowerInvariant();
    string? shortForm = null;
    string? description = null;
    bool isOptional = true; // Options are optional by default
    bool isRepeated = false;

    // Check constructor arguments
    if (attribute.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0].Value is string ctorLongForm)
      longForm = ctorLongForm.TrimStart('-');

    if (attribute.ConstructorArguments.Length > 1 && attribute.ConstructorArguments[1].Value is string ctorShortForm)
      shortForm = ctorShortForm.TrimStart('-');

    // Check named arguments
    foreach (KeyValuePair<string, TypedConstant> namedArg in attribute.NamedArguments)
    {
      switch (namedArg.Key)
      {
        case "LongName":
          longForm = (namedArg.Value.Value as string)?.TrimStart('-') ?? longForm;
          break;
        case "ShortName":
          shortForm = (namedArg.Value.Value as string)?.TrimStart('-');
          break;
        case "Description":
          description = namedArg.Value.Value as string;
          break;
        case "IsRequired":
          isOptional = namedArg.Value.Value is not true;
          break;
      }
    }

    string typeName = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    bool isFlag = typeName is "bool" or "global::System.Boolean";

    // Check for array/collection types (repeated options), via symbol inspection.
    isRepeated = IsRepeatedOptionType(property.Type);

    // Extract default value from property initializer
    PropertyDefault propertyDefault = ExtractPropertyDefaultValueFromSymbol(property, compilation, cancellationToken);

    return new OptionDefinition(
      Position: position,
      LongForm: longForm,
      ShortForm: shortForm,
      ParameterName: isFlag ? null : property.Name.ToLowerInvariant(),
      TypeConstraint: isFlag ? null : GetTypeConstraintFromClrType(typeName),
      Description: description,
      ExpectsValue: !isFlag,
      IsOptional: isOptional,
      IsRepeated: isRepeated,
      ParameterIsOptional: property.NullableAnnotation == NullableAnnotation.Annotated,
      ResolvedClrTypeName: typeName,
      DefaultValueLiteral: propertyDefault.Code,
      IsInitOnly: PropertyIsInitOnly(property),
      CapabilityDefaultValue: propertyDefault.Display,
      IsRequiredMember: property.IsRequired);
  }

  /// <summary>
  /// Extracts GroupOption bindings from base class hierarchy for handler property bindings.
  /// Walks the full inheritance chain and creates ParameterBinding entries for [GroupOption] properties.
  /// </summary>
  private static void ExtractGroupOptionBindingsFromBaseClasses
  (
    INamedTypeSymbol classSymbol,
    ImmutableArray<ParameterBinding>.Builder propertyBindings
  )
  {
    if (classSymbol.BaseType is null)
      return;

    INamedTypeSymbol? current = classSymbol.BaseType;

    while (current is not null && current.SpecialType != SpecialType.System_Object)
    {
      foreach (IPropertySymbol property in current.GetMembers().OfType<IPropertySymbol>())
      {
        // Skip properties without setters
        if (property.SetMethod is null && property.IsReadOnly)
          continue;

        foreach (AttributeData attribute in property.GetAttributes())
        {
          string? attributeName = attribute.AttributeClass?.Name;
          if (attributeName != GroupOptionAttributeName && attributeName != $"{GroupOptionAttributeName}Attribute")
            continue;

          string typeName = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
          string optionLongForm = ExtractGroupOptionLongForm(attribute, property.Name);

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

      current = current.BaseType;
    }
  }

  /// <summary>
  /// Extracts the long form option name from a [GroupOption] attribute.
  /// This must match the logic in ExtractGroupOptionFromAttribute for consistency.
  /// </summary>
  private static string ExtractGroupOptionLongForm(AttributeData attribute, string propertyName)
  {
    string longForm = propertyName.ToLowerInvariant();

    // Check constructor arguments - first positional arg is the long form
    if (attribute.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0].Value is string ctorLongForm)
      longForm = ctorLongForm.TrimStart('-');

    // Check named arguments
    foreach (KeyValuePair<string, TypedConstant> namedArg in attribute.NamedArguments)
    {
      if (namedArg.Key == "LongName")
      {
        longForm = (namedArg.Value.Value as string)?.TrimStart('-') ?? longForm;
        break;
      }
    }

    return longForm;
  }
}
