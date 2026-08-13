// Extracts route definitions from classes decorated with [NuruRoute] attribute.
//
// Handles endpoints pattern:
// - Read pattern from [NuruRoute("pattern")]
// - Check base class for [NuruRouteGroup] for prefix
// - Infer message type from interface (IQuery<T>, ICommand<T>)
// - Find [Parameter] and [Option] properties
// - Find nested Handler class

#region Design
// ENDPOINT SCOPING: All [NuruRoute] classes in a compilation are collected globally by the
// source generator. They are filtered per-app via FilterEndpointsForApp() during emission.
// .DiscoverEndpoints() includes ALL endpoints; .Map<T>() includes only that type.
//
// ALIAS GENERATION: ExtractAndCombineAliases builds full alias strings that completely replace
// groupPrefix + pattern. For group aliases, the alias replaces one GROUP's entire prefix (which
// may be multi-word, e.g. "git remote") by index into GroupInfo.GroupPrefixes. The emitter should
// NOT re-match literal segments after the alias prefix.
#endregion

namespace TimeWarp.Nuru.Generators;

using RoslynSyntaxNode = Microsoft.CodeAnalysis.SyntaxNode;

/// <summary>
/// Extracts route definitions from classes decorated with [NuruRoute] attribute.
/// </summary>
internal static class EndpointExtractor
{
  private const string NuruRouteAttributeName = "NuruRoute";
  private const string NuruRouteGroupAttributeName = "NuruRouteGroup";
  private const string NuruRouteAliasAttributeName = "NuruRouteAlias";
  private const string NuruRouteExampleAttributeName = "NuruRouteExample";
  private const string ParameterAttributeName = "Parameter";
  private const string OptionAttributeName = "Option";
  private const string GroupOptionAttributeName = "GroupOption";

  /// <summary>
  /// Like SymbolDisplayFormat.FullyQualifiedFormat, but also qualifies the containing type
  /// of member symbols (fields/properties), so an enum member or a static field reference
  /// (e.g. Environment.Production, string.Empty) formats as "global::MyApp.Environment.Production"
  /// rather than just "Production" — FullyQualifiedFormat's memberOptions default to None.
  /// </summary>
  private static readonly SymbolDisplayFormat FullyQualifiedMemberFormat = new(
    globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
    typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
    genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
    memberOptions: SymbolDisplayMemberOptions.IncludeContainingType);

  /// <summary>
  /// Extracts a RouteDefinition from a class with [NuruRoute] attribute.
  /// </summary>
  /// <param name="classDeclaration">The class declaration with [NuruRoute].</param>
  /// <param name="semanticModel">Semantic model for type resolution.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>An extraction result containing the route definition and any diagnostics.</returns>
  public static EndpointExtractionResult Extract
  (
    ClassDeclarationSyntax classDeclaration,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    // Find the [NuruRoute] attribute
    (string? pattern, string? description, Location attributeLocation) = ExtractNuruRouteAttribute(classDeclaration);
    if (pattern is null)
      return EndpointExtractionResult.Empty;

    // VALIDATE: Pattern must be empty string OR a single literal only
    Diagnostic? patternDiagnostic = ValidateRoutePattern(pattern, attributeLocation);
    if (patternDiagnostic is not null)
    {
      return EndpointExtractionResult.Failure(patternDiagnostic);
    }

    // Get the group info from base class hierarchy - always collect full hierarchy
    GroupInfo groupInfo = ExtractGroupInfo(classDeclaration, semanticModel, cancellationToken);

    // Infer message type from interfaces
    string messageType = InferMessageType(classDeclaration, semanticModel, cancellationToken);

    // Extract parameters and options from properties
    ImmutableArray<SegmentDefinition> segments = ExtractSegmentsFromProperties(classDeclaration, semanticModel, cancellationToken);

    // For validated patterns, we know they are either empty or single literal
    // So we can safely use the pattern as-is (no need to parse for parameters/options)
    ImmutableArray<SegmentDefinition> patternSegments = string.IsNullOrEmpty(pattern)
      ? []
      : [new LiteralDefinition(0, pattern)];

    // Merge segments (pattern segments first, then property segments that aren't duplicates)
    ImmutableArray<SegmentDefinition> mergedSegments = MergeSegments(patternSegments, segments);

    // Get handler info (requires nested Handler class)
    HandlerDefinition? handler = ExtractHandler(classDeclaration, semanticModel, cancellationToken);
    if (handler is null)
    {
      // No nested Handler class found - skip this route
      return EndpointExtractionResult.Empty;
    }

    // Extract filter interfaces (for behavior filtering)
    ImmutableArray<InterfaceImplementationDefinition> filterInterfaces =
      ExtractFilterInterfaces(classDeclaration, semanticModel, cancellationToken);

    // Calculate specificity
    int specificity = mergedSegments.Sum(s => s.SpecificityContribution);

    // Extract and combine aliases
    ImmutableArray<string> aliases = ExtractAndCombineAliases(
      classDeclaration,
      semanticModel,
      groupInfo,
      pattern,
      cancellationToken);

    // Extract usage examples (all [NuruRouteExample] attributes, in source order)
    ImmutableArray<ExampleDefinition> examples = ExtractNuruRouteExampleAttributes(
      classDeclaration,
      semanticModel,
      cancellationToken);

    RouteDefinition route = RouteDefinition.Create(
      originalPattern: pattern,
      segments: mergedSegments,
      handler: handler,
      messageType: messageType,
      description: description,
      groupPrefix: groupInfo.FullPrefix,
      groupTypeHierarchy: groupInfo.TypeHierarchy,
      computedSpecificity: specificity,
      aliases: aliases,
      implements: filterInterfaces,
      examples: examples);

    return EndpointExtractionResult.Success(route);
  }

  /// <summary>
  /// Extracts and combines aliases from the command class and group hierarchy.
  /// </summary>
  private static ImmutableArray<string> ExtractAndCombineAliases(
    ClassDeclarationSyntax classDeclaration,
    SemanticModel semanticModel,
    GroupInfo groupInfo,
    string pattern,
    CancellationToken cancellationToken)
  {
    List<string> allAliases = [];

    // Extract direct aliases from command class
    ImmutableArray<string> directAliases = ExtractNuruRouteAliasAttribute(classDeclaration, semanticModel, cancellationToken);
    allAliases.AddRange(directAliases);

    // Generate full alias patterns from group aliases. GroupPrefixIndex selects a GROUP
    // (not a word), so the alias replaces that group's ENTIRE prefix — correct even when the
    // prefix is multi-word (e.g. a group "git remote" aliased to "gr" yields "gr {pattern}").
    if (!groupInfo.GroupAliases.IsDefaultOrEmpty && !groupInfo.GroupPrefixes.IsDefaultOrEmpty)
    {
      ImmutableArray<string> groupPrefixes = groupInfo.GroupPrefixes;

      foreach (GroupAliasDefinition groupAlias in groupInfo.GroupAliases)
      {
        if (groupAlias.GroupPrefixIndex < 0 || groupAlias.GroupPrefixIndex >= groupPrefixes.Length)
          continue;

        string aliasPrefix = string.Join(
          " ",
          groupPrefixes.Select((prefix, index) => index == groupAlias.GroupPrefixIndex ? groupAlias.Alias : prefix));

        string fullAliasPattern = string.IsNullOrEmpty(pattern)
          ? aliasPrefix
          : $"{aliasPrefix} {pattern}";

        allAliases.Add(fullAliasPattern);
      }
    }

    return [..allAliases];
  }

  /// <summary>
  /// Validates that a route pattern is a single literal identifier or empty string.
  /// Returns a diagnostic if the pattern is invalid, null if valid.
  /// </summary>
  private static Diagnostic? ValidateRoutePattern(string pattern, Location attributeLocation)
  {
    // Empty string is valid (root/default route)
    if (string.IsNullOrEmpty(pattern))
      return null;

    // Parse the pattern to see what it contains
    ImmutableArray<SegmentDefinition> segments = PatternStringExtractor.ExtractSegments(pattern);

    // Valid: exactly one segment that is a literal
    if (segments.Length == 1 && segments[0] is LiteralDefinition)
      return null;

    // Invalid: zero segments (shouldn't happen), multiple segments, or non-literal segment
    return Diagnostic.Create(
      DiagnosticDescriptors.InvalidNuruRoutePattern,
      attributeLocation,
      pattern);
  }

  /// <summary>
  /// Extracts pattern, description, and location from [NuruRoute] attribute.
  /// </summary>
  private static (string? Pattern, string? Description, Location Location) ExtractNuruRouteAttribute(ClassDeclarationSyntax classDeclaration)
  {
    foreach (AttributeListSyntax attributeList in classDeclaration.AttributeLists)
    {
      foreach (AttributeSyntax attribute in attributeList.Attributes)
      {
        string? attributeName = GetAttributeName(attribute);
        if (attributeName != NuruRouteAttributeName && attributeName != $"{NuruRouteAttributeName}Attribute")
          continue;

        string? pattern = ExtractPositionalStringArgument(attribute, 0);
        string? description = ExtractNamedStringArgument(attribute, "Description");
        Location location = attribute.GetLocation();

        return (pattern, description, location);
      }
    }

    return (null, null, Location.None);
  }

  /// <summary>
  /// Extracts aliases from [NuruRouteAlias] attribute on the command class.
  /// </summary>
  private static ImmutableArray<string> ExtractNuruRouteAliasAttribute(
    ClassDeclarationSyntax classDeclaration,
    SemanticModel semanticModel,
    CancellationToken cancellationToken)
  {
    INamedTypeSymbol? classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken);
    if (classSymbol is null)
      return [];

    foreach (AttributeData attribute in classSymbol.GetAttributes())
    {
      string? attributeName = attribute.AttributeClass?.Name;
      if (attributeName != NuruRouteAliasAttributeName && attributeName != $"{NuruRouteAliasAttributeName}Attribute")
        continue;

      if (attribute.ConstructorArguments.Length > 0)
      {
        TypedConstant argsArray = attribute.ConstructorArguments[0];

        List<string> aliases = [];

        if (argsArray.Values.Length > 0)
        {
          foreach (TypedConstant value in argsArray.Values)
          {
            if (value.Value is string alias)
              aliases.Add(alias);
          }
        }
        else if (argsArray.Value is string singleAlias)
        {
          aliases.Add(singleAlias);
        }

        return [..aliases];
      }
    }

    return [];
  }

  /// <summary>
  /// Extracts usage examples from all <c>[NuruRouteExample]</c> attributes on the command class.
  /// Unlike aliases, examples use <c>AllowMultiple = true</c>, so every matching attribute is
  /// accumulated (not just the first), preserving source declaration order. Attributes with a
  /// null or empty command are silently skipped.
  /// </summary>
  private static ImmutableArray<ExampleDefinition> ExtractNuruRouteExampleAttributes(
    ClassDeclarationSyntax classDeclaration,
    SemanticModel semanticModel,
    CancellationToken cancellationToken)
  {
    INamedTypeSymbol? classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken);
    if (classSymbol is null)
      return [];

    List<ExampleDefinition> examples = [];

    foreach (AttributeData attribute in classSymbol.GetAttributes())
    {
      string? attributeName = attribute.AttributeClass?.Name;
      if (attributeName != NuruRouteExampleAttributeName && attributeName != $"{NuruRouteExampleAttributeName}Attribute")
        continue;

      if (attribute.ConstructorArguments.Length == 0 ||
          attribute.ConstructorArguments[0].Value is not string command ||
          string.IsNullOrEmpty(command))
      {
        continue;
      }

      string? description = null;
      foreach (KeyValuePair<string, TypedConstant> namedArg in attribute.NamedArguments)
      {
        if (namedArg.Key == "Description")
        {
          description = namedArg.Value.Value as string;
        }
      }

      examples.Add(new ExampleDefinition(command, description));
    }

    return [.. examples];
  }

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
  private static GroupInfo ExtractGroupInfo
  (
    ClassDeclarationSyntax classDeclaration,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    if (classDeclaration.BaseList is null)
    {
      // No base class - ungrouped endpoint
      return new GroupInfo
      (
        TypeHierarchy: [],
        FullPrefix: null,
        GroupAliases: [],
        GroupPrefixes: []
      );
    }

    INamedTypeSymbol? classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken);
    if (classSymbol?.BaseType is null)
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
        string? attributeName = attribute.AttributeClass?.Name;
        if (attributeName != NuruRouteGroupAttributeName && attributeName != $"{NuruRouteGroupAttributeName}Attribute")
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
    ClassDeclarationSyntax classDeclaration,
    SemanticModel semanticModel,
    int startPosition,
    CancellationToken cancellationToken
  )
  {
    if (classDeclaration.BaseList is null)
      return [];

    INamedTypeSymbol? classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken);
    if (classSymbol?.BaseType is null)
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

          OptionDefinition? option = ExtractGroupOptionFromAttribute(property, attribute, position++, semanticModel.Compilation, cancellationToken);
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
    string? defaultValueLiteral = ExtractPropertyDefaultValueFromSymbol(property, compilation, cancellationToken);

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
      DefaultValueLiteral: defaultValueLiteral);
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

  /// <summary>
  /// Infers message type from implemented interfaces.
  /// </summary>
  private static string InferMessageType
  (
    ClassDeclarationSyntax classDeclaration,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    INamedTypeSymbol? classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken);
    if (classSymbol is null)
      return "Unspecified";

    foreach (INamedTypeSymbol iface in classSymbol.AllInterfaces)
    {
      string interfaceName = iface.Name;

      if (interfaceName == "IQuery" || interfaceName.StartsWith("IQuery`", StringComparison.Ordinal))
        return "Query";

      if (interfaceName == "IIdempotentCommand" || interfaceName.StartsWith("IIdempotentCommand`", StringComparison.Ordinal))
        return "IdempotentCommand";

      if (interfaceName == "ICommand" || interfaceName.StartsWith("ICommand`", StringComparison.Ordinal))
        return "Command";
    }

    return "Unspecified";
  }

  /// <summary>
  /// Extracts segments from properties with [Parameter] or [Option] attributes.
  /// Uses semantic model to find all properties including those in partial class files.
  /// Also extracts [GroupOption] attributes from base classes.
  /// </summary>
  private static ImmutableArray<SegmentDefinition> ExtractSegmentsFromProperties
  (
    ClassDeclarationSyntax classDeclaration,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    ImmutableArray<SegmentDefinition>.Builder segments = ImmutableArray.CreateBuilder<SegmentDefinition>();
    int position = 100; // Start after pattern segments

    // Get the type symbol to find ALL properties including those in partial classes
    INamedTypeSymbol? classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken);
    if (classSymbol is null)
      return segments.ToImmutable();

    // Iterate over all properties from the type symbol (handles partial classes correctly)
    foreach (IPropertySymbol propertySymbol in classSymbol.GetMembers().OfType<IPropertySymbol>())
    {
      SegmentDefinition? segment = ExtractSegmentFromPropertySymbol(propertySymbol, position++, semanticModel.Compilation, cancellationToken);
      if (segment is not null)
        segments.Add(segment);
    }

    // Extract GroupOptions from base classes and add them to segments
    ImmutableArray<SegmentDefinition> groupOptions = ExtractGroupOptionsFromBaseClasses(
      classDeclaration,
      semanticModel,
      position,
      cancellationToken);
    segments.AddRange(groupOptions);

    return segments.ToImmutable();
  }

  /// <summary>
  /// Extracts a segment from a property symbol with [Parameter] or [Option] attribute.
  /// Works with IPropertySymbol to handle properties from partial classes.
  /// </summary>
  private static SegmentDefinition? ExtractSegmentFromPropertySymbol
  (
    IPropertySymbol propertySymbol,
    int position,
    Compilation? compilation,
    CancellationToken cancellationToken
  )
  {
    foreach (AttributeData attribute in propertySymbol.GetAttributes())
    {
      string? attributeName = attribute.AttributeClass?.Name;

      if (attributeName == ParameterAttributeName || attributeName == $"{ParameterAttributeName}Attribute")
      {
        return ExtractParameterFromAttribute(propertySymbol, attribute, position);
      }

      if (attributeName == OptionAttributeName || attributeName == $"{OptionAttributeName}Attribute")
      {
        return ExtractOptionFromAttribute(propertySymbol, attribute, position, compilation, cancellationToken);
      }
    }

    return null;
  }

  /// <summary>
  /// Extracts a segment from a property with [Parameter] or [Option] attribute.
  /// </summary>
  private static SegmentDefinition? ExtractSegmentFromProperty
  (
    PropertyDeclarationSyntax property,
    int position,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    IPropertySymbol? propertySymbol = semanticModel.GetDeclaredSymbol(property, cancellationToken);
    if (propertySymbol is null)
      return null;

    foreach (AttributeData attribute in propertySymbol.GetAttributes())
    {
      string? attributeName = attribute.AttributeClass?.Name;

      if (attributeName == ParameterAttributeName || attributeName == $"{ParameterAttributeName}Attribute")
      {
        return ExtractParameterFromAttribute(propertySymbol, attribute, position);
      }

      if (attributeName == OptionAttributeName || attributeName == $"{OptionAttributeName}Attribute")
      {
        return ExtractOptionFromAttribute(propertySymbol, attribute, position, semanticModel.Compilation, cancellationToken);
      }
    }

    return null;
  }

  /// <summary>
  /// Extracts a ParameterDefinition from a [Parameter] attribute.
  /// </summary>
  private static ParameterDefinition ExtractParameterFromAttribute
  (
    IPropertySymbol property,
    AttributeData attribute,
    int position
  )
  {
    string name = property.Name;
    string? description = null;
    bool isCatchAll = false;
    bool isOptional = property.NullableAnnotation == NullableAnnotation.Annotated;

    // Check named arguments
    foreach (KeyValuePair<string, TypedConstant> namedArg in attribute.NamedArguments)
    {
      switch (namedArg.Key)
      {
        case "Description":
          description = namedArg.Value.Value as string;
          break;
        case "IsCatchAll":
          isCatchAll = namedArg.Value.Value is true;
          break;
      }
    }

    string typeName = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    string? typeConstraint = GetTypeConstraintFromClrType(typeName);

    return new ParameterDefinition(
      Position: position,
      Name: name.ToLowerInvariant(),
      TypeConstraint: typeConstraint,
      Description: description,
      IsOptional: isOptional,
      IsCatchAll: isCatchAll,
      ResolvedClrTypeName: typeName);
  }

  /// <summary>
  /// Extracts an OptionDefinition from an [Option] attribute.
  /// </summary>
  private static OptionDefinition ExtractOptionFromAttribute
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

    // Extract default value from property initializer via DeclaringSyntaxReferences
    // This handles properties defined in partial class files correctly
    string? defaultValueLiteral = ExtractPropertyDefaultValueFromSymbol(property, compilation, cancellationToken);

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
      DefaultValueLiteral: defaultValueLiteral);
  }

  /// <summary>
  /// Extracts the default value literal from a property symbol using DeclaringSyntaxReferences.
  /// This method correctly handles properties defined in partial class files.
  /// Prefers semantic resolution (constant value / symbol reference, emitted fully-qualified)
  /// over raw syntax text, so defaults referencing types outside the generated file's using
  /// scope (e.g. an enum member or a static field) still resolve correctly.
  /// </summary>
  /// <param name="property">The property symbol.</param>
  /// <param name="compilation">Compilation used to obtain a SemanticModel for the initializer, if available.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>The default value literal (e.g., "1", "\"default\"", "global::MyApp.Environment.Production"), or null if no initializer.</returns>
  private static string? ExtractPropertyDefaultValueFromSymbol(IPropertySymbol property, Compilation? compilation, CancellationToken cancellationToken)
  {
    // Get syntax references for this property (handles partial classes correctly)
    ImmutableArray<SyntaxReference> syntaxReferences = property.DeclaringSyntaxReferences;
    if (syntaxReferences.Length == 0)
      return null;

    // Get the first syntax reference and locate the PropertyDeclarationSyntax
    Microsoft.CodeAnalysis.SyntaxNode? syntaxNode = syntaxReferences[0].GetSyntax(cancellationToken);
    if (syntaxNode is not PropertyDeclarationSyntax propertySyntax)
      return null;

    // Check for property initializer (e.g., public int X { get; set; } = 1;)
    if (propertySyntax.Initializer?.Value is not { } initializerValue)
      return null;

    if (compilation is not null)
    {
      SemanticModel semanticModel = compilation.GetSemanticModel(syntaxReferences[0].SyntaxTree);

      // Member/type references (e.g. Environment.Production, SomeStaticClass.Default)
      // resolve directly to a symbol; emit fully-qualified so the reference resolves
      // outside the initializer's original using scope.
      SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(initializerValue, cancellationToken);
      if (symbolInfo.Symbol is not null)
        return symbolInfo.Symbol.ToDisplayString(FullyQualifiedMemberFormat);

      // Otherwise, prefer the compile-time constant value (numbers, bools, strings, chars).
      Optional<object?> constantValue = semanticModel.GetConstantValue(initializerValue, cancellationToken);
      if (constantValue.HasValue)
      {
        return constantValue.Value switch
        {
          string s => SymbolDisplay.FormatLiteral(s, quote: true),
          char c => SymbolDisplay.FormatLiteral(c, quote: true),
          bool b => b ? "true" : "false",
          null => "null",
          IFormattable formattable => formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
          _ => constantValue.Value.ToString()
        };
      }
    }

    // Fall back to raw syntax text if semantic resolution is unavailable or fails.
    return initializerValue.ToString();
  }

  /// <summary>
  /// Extracts the default value literal from a property initializer.
  /// </summary>
  /// <param name="property">The property declaration syntax.</param>
  /// <returns>The default value literal (e.g., "1", "\"default\""), or null if no initializer.</returns>
  /// <remarks>
  /// TODO(454-012): unlike ExtractPropertyDefaultValueFromSymbol, this syntax-only overload
  /// still emits raw initializer text and has no SemanticModel available to resolve it
  /// fully-qualified. It is currently unused (dead code) — ExtractSegmentFromProperty, its
  /// only caller, is itself never invoked. Fix here too if it is ever wired up.
  /// </remarks>
  private static string? ExtractPropertyDefaultValue(PropertyDeclarationSyntax property)
  {
    // Check for property initializer (e.g., public int X { get; set; } = 1;)
    if (property.Initializer?.Value is { } initializerValue)
    {
      return initializerValue.ToString();
    }

    return null;
  }

  /// <summary>
  /// Extracts handler information for endpoints.
  /// Finds nested Handler class and extracts its constructor dependencies.
  /// </summary>
  /// <returns>The handler definition, or null if no nested Handler class is found.</returns>
  private static HandlerDefinition? ExtractHandler
  (
    ClassDeclarationSyntax classDeclaration,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    INamedTypeSymbol? classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken);
    if (classSymbol is null)
      return null;

    // 1. Find nested Handler class (required)
    INamedTypeSymbol? handlerClass = classSymbol.GetTypeMembers("Handler").FirstOrDefault();
    if (handlerClass is null)
    {
      // No nested Handler class found - skip this route
      return null;
    }

    string commandTypeName = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    string handlerTypeName = handlerClass.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    // 2. Extract constructor dependencies from the Handler class
    ImmutableArray<ParameterBinding> constructorDeps = ExtractConstructorDependencies(handlerClass);

    // 3. Extract property bindings from the command class
    ImmutableArray<ParameterBinding>.Builder propertyBindings = ImmutableArray.CreateBuilder<ParameterBinding>();

    foreach (IPropertySymbol property in classSymbol.GetMembers().OfType<IPropertySymbol>())
    {
      // Skip properties without setters
      if (property.SetMethod is null && property.IsReadOnly)
        continue;

      // Check for [Parameter] or [Option] attributes
      foreach (AttributeData attribute in property.GetAttributes())
      {
        string? attributeName = attribute.AttributeClass?.Name;

        if (attributeName == ParameterAttributeName || attributeName == $"{ParameterAttributeName}Attribute")
        {
          string typeName = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

          // Check if this is a catch-all parameter
          bool isCatchAll = attribute.NamedArguments.Any(na =>
            na.Key == "IsCatchAll" && na.Value.Value is true);

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

        if (attributeName == OptionAttributeName || attributeName == $"{OptionAttributeName}Attribute")
        {
          string typeName = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

          // Extract the actual option long form from the attribute (e.g., "no-cache" from [Option("no-cache", null)])
          // This must match what ExtractOptionFromAttribute extracts for OptionDefinition.LongForm
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

    // 3b. Extract GroupOption bindings from base classes
    ExtractGroupOptionBindingsFromBaseClasses(classSymbol, propertyBindings);

    // 4. Infer return type from interface
    HandlerReturnType returnType = InferReturnTypeFromInterfaces(classSymbol);

    return HandlerDefinition.ForCommand(
      commandTypeName: commandTypeName,
      nestedHandlerTypeName: handlerTypeName,
      propertyBindings: propertyBindings.ToImmutable(),
      constructorDependencies: constructorDeps,
      returnType: returnType);
  }

  /// <summary>
  /// Extracts constructor dependencies from a nested Handler class.
  /// </summary>
  private static ImmutableArray<ParameterBinding> ExtractConstructorDependencies(INamedTypeSymbol handlerClass)
  {
    ImmutableArray<ParameterBinding>.Builder deps = ImmutableArray.CreateBuilder<ParameterBinding>();

    // Find the first public constructor (or primary constructor)
    IMethodSymbol? constructor = handlerClass.InstanceConstructors
      .FirstOrDefault(c => c.DeclaredAccessibility == Accessibility.Public);

    if (constructor is null)
      return deps.ToImmutable();

    foreach (IParameterSymbol param in constructor.Parameters)
    {
      string typeName = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

      // All constructor params are services (resolved via static instantiation per task #292)
      deps.Add(ParameterBinding.FromService(
        parameterName: param.Name,
        serviceTypeName: typeName));
    }

    return deps.ToImmutable();
  }

  /// <summary>
  /// Infers the return type from implemented interfaces.
  /// </summary>
  private static HandlerReturnType InferReturnTypeFromInterfaces(INamedTypeSymbol classSymbol)
  {
    foreach (INamedTypeSymbol iface in classSymbol.AllInterfaces)
    {
      if (!iface.IsGenericType)
        continue;

      string interfaceName = iface.Name;
      if (interfaceName != "IQuery" && interfaceName != "ICommand" && interfaceName != "IIdempotentCommand")
        continue;

      if (iface.TypeArguments.Length > 0)
      {
        ITypeSymbol resultType = iface.TypeArguments[0];
        string fullTypeName = resultType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string shortTypeName = resultType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        return HandlerReturnType.TaskOf(fullTypeName, shortTypeName);
      }
    }

    return HandlerReturnType.Task;
  }

  /// <summary>
  /// Extracts filter interfaces from the command class.
  /// Excludes message interfaces (ICommand, IQuery, etc.) as those are for message typing, not behavior filtering.
  /// </summary>
  private static ImmutableArray<InterfaceImplementationDefinition> ExtractFilterInterfaces
  (
    ClassDeclarationSyntax classDeclaration,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    INamedTypeSymbol? classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken);
    if (classSymbol is null)
      return [];

    ImmutableArray<InterfaceImplementationDefinition>.Builder filterInterfaces =
      ImmutableArray.CreateBuilder<InterfaceImplementationDefinition>();

    foreach (INamedTypeSymbol iface in classSymbol.AllInterfaces)
    {
      // Skip Nuru message interfaces (ICommand, IQuery, etc.)
      string interfaceName = iface.Name;
      if (IsMessageInterface(interfaceName))
        continue;

      // Skip common .NET interfaces
      if (IsCommonDotNetInterface(iface))
        continue;

      // This is a filter interface - add it
      string fullTypeName = iface.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

      // For endpoints, properties are already on the class - no extraction needed
      filterInterfaces.Add(new InterfaceImplementationDefinition(
        FullInterfaceTypeName: fullTypeName,
        Properties: []));
    }

    return filterInterfaces.ToImmutable();
  }

  /// <summary>
  /// Checks if an interface is a Nuru message interface (ICommand, IQuery, etc.).
  /// These are for message typing, not behavior filtering.
  /// </summary>
  private static bool IsMessageInterface(string interfaceName)
  {
    return interfaceName is "ICommand" or "IQuery" or "IIdempotentCommand"
        || interfaceName.StartsWith("ICommand`", StringComparison.Ordinal)
        || interfaceName.StartsWith("IQuery`", StringComparison.Ordinal)
        || interfaceName.StartsWith("IIdempotentCommand`", StringComparison.Ordinal)
        || interfaceName is "ICommandHandler" or "IQueryHandler"
        || interfaceName.StartsWith("ICommandHandler`", StringComparison.Ordinal)
        || interfaceName.StartsWith("IQueryHandler`", StringComparison.Ordinal);
  }

  /// <summary>
  /// Checks if an interface is a common .NET interface that shouldn't be treated as a filter.
  /// </summary>
  private static bool IsCommonDotNetInterface(INamedTypeSymbol iface)
  {
    string? containingNamespace = iface.ContainingNamespace?.ToDisplayString();

    // Skip System.* interfaces
    if (containingNamespace?.StartsWith("System", StringComparison.Ordinal) == true)
      return true;

    // Skip Microsoft.* interfaces
    if (containingNamespace?.StartsWith("Microsoft", StringComparison.Ordinal) == true)
      return true;

    return false;
  }

  /// <summary>
  /// Merges pattern segments with property segments, avoiding duplicates.
  /// </summary>
  private static ImmutableArray<SegmentDefinition> MergeSegments
  (
    ImmutableArray<SegmentDefinition> patternSegments,
    ImmutableArray<SegmentDefinition> propertySegments
  )
  {
    if (propertySegments.Length == 0)
      return patternSegments;

    if (patternSegments.Length == 0)
      return propertySegments;

    ImmutableArray<SegmentDefinition>.Builder merged = ImmutableArray.CreateBuilder<SegmentDefinition>();
    merged.AddRange(patternSegments);

    HashSet<string> existingNames = [];

    foreach (SegmentDefinition segment in patternSegments)
    {
      if (segment is ParameterDefinition param)
        existingNames.Add(param.Name.ToLowerInvariant());
      else if (segment is OptionDefinition option)
        existingNames.Add((option.LongForm ?? option.ShortForm ?? "").ToLowerInvariant());
    }

    // Add property segments that don't duplicate pattern segments
    foreach (SegmentDefinition segment in propertySegments)
    {
      string name = segment switch
      {
        ParameterDefinition param => param.Name.ToLowerInvariant(),
        OptionDefinition option => (option.LongForm ?? option.ShortForm ?? "").ToLowerInvariant(),
        _ => ""
      };

      if (!string.IsNullOrEmpty(name) && !existingNames.Contains(name))
        merged.Add(segment);
    }

    return merged.ToImmutable();
  }

  /// <summary>
  /// Gets the attribute name from an AttributeSyntax.
  /// </summary>
  private static string? GetAttributeName(AttributeSyntax attribute)
  {
    return attribute.Name switch
    {
      IdentifierNameSyntax identifier => identifier.Identifier.Text,
      QualifiedNameSyntax qualified => qualified.Right.Identifier.Text,
      _ => null
    };
  }

  /// <summary>
  /// Extracts a positional string argument from an attribute.
  /// </summary>
  private static string? ExtractPositionalStringArgument(AttributeSyntax attribute, int index)
  {
    AttributeArgumentListSyntax? args = attribute.ArgumentList;
    if (args is null || args.Arguments.Count <= index)
      return null;

    AttributeArgumentSyntax arg = args.Arguments[index];

    // Skip named arguments
    if (arg.NameEquals is not null || arg.NameColon is not null)
      return null;

    return arg.Expression switch
    {
      LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression)
        => literal.Token.ValueText,
      _ => null
    };
  }

  /// <summary>
  /// Extracts a named string argument from an attribute.
  /// </summary>
  private static string? ExtractNamedStringArgument(AttributeSyntax attribute, string name)
  {
    AttributeArgumentListSyntax? args = attribute.ArgumentList;
    if (args is null)
      return null;

    foreach (AttributeArgumentSyntax arg in args.Arguments)
    {
      if (arg.NameEquals?.Name.Identifier.Text == name)
      {
        return arg.Expression switch
        {
          LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression)
            => literal.Token.ValueText,
          _ => null
        };
      }
    }

    return null;
  }

  /// <summary>
  /// Checks whether a property type represents a repeated option (array or a collection
  /// interface), via ITypeSymbol inspection rather than substring matching on the type
  /// name (which would false-positive on user types like MyApp.IListManager).
  /// </summary>
  private static bool IsRepeatedOptionType(ITypeSymbol type)
  {
    // string implements IEnumerable<char> but must never be treated as a repeated option.
    if (type.SpecialType == SpecialType.System_String)
      return false;

    if (type.TypeKind == TypeKind.Array)
      return true;

    if (type is not INamedTypeSymbol namedType)
      return false;

    if (IsCollectionInterface(namedType))
      return true;

    foreach (INamedTypeSymbol iface in namedType.AllInterfaces)
    {
      if (IsCollectionInterface(iface))
        return true;
    }

    return false;
  }

  /// <summary>
  /// Checks whether a type is IEnumerable&lt;T&gt;, IList&lt;T&gt;, or ICollection&lt;T&gt;
  /// from System.Collections.Generic, matched by OriginalDefinition rather than a
  /// substring check.
  /// </summary>
  private static bool IsCollectionInterface(INamedTypeSymbol type)
  {
    if (type.TypeKind != TypeKind.Interface)
      return false;

    if (type.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
      return true;

    if (type.OriginalDefinition.ContainingNamespace?.ToDisplayString() != "System.Collections.Generic")
      return false;

    return type.OriginalDefinition.Name is "IList" or "ICollection";
  }

  /// <summary>
  /// Gets a type constraint string from a CLR type name.
  /// Handles both C# keyword aliases (e.g., "double") and fully qualified names (e.g., "global::System.Double").
  /// Also handles nullable value types (e.g., "int?", "long?").
  /// </summary>
  private static string? GetTypeConstraintFromClrType(string clrTypeName)
  {
    // Handle nullable value types (e.g., "int?", "long?")
    if (clrTypeName.EndsWith('?'))
    {
      string baseType = clrTypeName[..^1];
      string? baseConstraint = GetTypeConstraintFromClrType(baseType);
      return baseConstraint is not null ? $"{baseConstraint}?" : null;
    }

    // Handle Nullable<T> generic syntax (e.g., "global::System.Nullable<global::System.Int64>")
    if (clrTypeName.Contains("System.Nullable<", StringComparison.Ordinal))
    {
      int start = clrTypeName.IndexOf('<', StringComparison.Ordinal) + 1;
      int end = clrTypeName.LastIndexOf('>');
      if (start > 0 && end > start)
      {
        string innerType = clrTypeName[start..end];
        string? baseConstraint = GetTypeConstraintFromClrType(innerType);
        return baseConstraint is not null ? $"{baseConstraint}?" : null;
      }
    }

    return clrTypeName switch
    {
      // C# keyword aliases (returned by SymbolDisplayFormat.FullyQualifiedFormat for built-in types)
      "int" or "global::System.Int32" => "int",
      "long" or "global::System.Int64" => "long",
      "short" or "global::System.Int16" => "short",
      "byte" or "global::System.Byte" => "byte",
      "float" or "global::System.Single" => "float",
      "double" or "global::System.Double" => "double",
      "decimal" or "global::System.Decimal" => "decimal",
      "bool" or "global::System.Boolean" => "bool",
      "char" or "global::System.Char" => "char",
      "string" or "global::System.String" => null, // string is default, no conversion needed
      "global::System.Guid" or "System.Guid" or "Guid" => "guid",
      "global::System.DateTime" or "System.DateTime" or "DateTime" => "datetime",
      "global::System.DateTimeOffset" or "System.DateTimeOffset" or "DateTimeOffset" => "datetimeoffset",
      "global::System.TimeSpan" or "System.TimeSpan" or "TimeSpan" => "timespan",
      "global::System.Uri" or "System.Uri" or "Uri" => "uri",
      "global::System.Version" or "System.Version" or "Version" => "version",
      _ => null
    };
  }

  /// <summary>
  /// Extracts the long form option name from an [Option] attribute.
  /// This must match the logic in ExtractOptionFromAttribute for consistency.
  /// </summary>
  private static string ExtractOptionLongForm(AttributeData attribute, string propertyName)
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
