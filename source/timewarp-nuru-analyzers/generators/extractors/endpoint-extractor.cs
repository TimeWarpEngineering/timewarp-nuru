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
//
// DEFAULT LITERALS: A finite constant is formatted with the suffix its type needs (1.5m, 2.0f).
// NaN and the infinities use the named constants. An enum member stays a member name.
// A const field whose type is an enum stays a symbol. A flags combination or a cast
// becomes a cast to the enum type. A field or property symbol is fully qualified.
// Object creation, an invocation, or a method group becomes default!.
// = null! on a non-nullable reference is copied as null!.
// A positional record property takes its primary-constructor parameter default.
// An initializer that is only a parameter (= Name) takes that parameter default.
// Any other initializer that reads a parameter has no default.
// Capabilities omit default!, null!, and a non-const field or property. They show a
// plain number, the enum member name, or the member names of a [Flags] combination.
// The object initializer omits default! until the caller supplies a value, so the
// declared initializer runs. A type with no parameterless constructor is new(args)
// for the constructor whose parameters match property names.
#endregion

namespace TimeWarp.Nuru.Generators;

using RoslynSyntaxNode = Microsoft.CodeAnalysis.SyntaxNode;

/// <summary>
/// Extracts route definitions from classes decorated with [NuruRoute] attribute.
/// </summary>
internal static partial class EndpointExtractor
{
  private const string NuruRouteAttributeName = "NuruRoute";
  private const string NuruRouteAliasAttributeName = "NuruRouteAlias";
  private const string NuruRouteExampleAttributeName = "NuruRouteExample";
  private const string DestructiveAttributeFullName = "TimeWarp.Nuru.DestructiveAttribute";
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
  /// Sentinel stored when the initializer cannot be copied into generated code.
  /// </summary>
  internal const string UnemittableDefaultLiteral = "default!";

  /// <summary>
  /// Literal for <c>= null!</c> on a non-nullable reference. It is copied as code, so it
  /// needs no supplied test, and capabilities omit it.
  /// </summary>
  internal const string NullForgivingDefaultLiteral = "null!";

  /// <summary>
  /// C# literal for an initializer, and the separate value <c>--capabilities</c> shows.
  /// </summary>
  private readonly record struct PropertyDefault(string? Code, string? Display);

  /// <summary>
  /// Extracts a RouteDefinition from a class with [NuruRoute] attribute.
  /// </summary>
  /// <param name="classDeclaration">The class declaration with [NuruRoute].</param>
  /// <param name="semanticModel">Semantic model for type resolution.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>An extraction result containing the route definition and any diagnostics.</returns>
  public static EndpointExtractionResult Extract
  (
    TypeDeclarationSyntax classDeclaration,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    RouteIdentity identity = ResolveRouteIdentity(classDeclaration, semanticModel, cancellationToken);
    if (!identity.HasNuruRoute && !identity.HasOperation)
      return EndpointExtractionResult.Empty;

    string? pattern = identity.Pattern;
    string? description = identity.Description;
    Location attributeLocation = identity.Location;
    if (pattern is null)
      return EndpointExtractionResult.Empty;

    // [Operation] without a mediator kind is not an endpoint. [NuruRoute] still is.
    // Run this before pattern validation so a non-endpoint operation does not
    // report a route-pattern diagnostic.
    string messageType = InferMessageType(classDeclaration, semanticModel, cancellationToken);
    if (!identity.HasNuruRoute && messageType == "Unspecified")
      return EndpointExtractionResult.Empty;

    // VALIDATE: Pattern must be empty string OR a single literal only
    Diagnostic? patternDiagnostic = ValidateRoutePattern(pattern, attributeLocation);
    if (patternDiagnostic is not null)
    {
      return EndpointExtractionResult.Failure(patternDiagnostic);
    }

    // Get the group info from base class hierarchy - always collect full hierarchy
    GroupInfo groupInfo = ExtractGroupInfo(classDeclaration, semanticModel, cancellationToken);
    groupInfo = ApplyCliBindingGroup(groupInfo, classDeclaration, semanticModel, cancellationToken);

    INamedTypeSymbol? endpointSymbol = semanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken);
    if (endpointSymbol is null)
      return EndpointExtractionResult.Empty;

    if (!TrySelectConstructorArguments(
      endpointSymbol,
      attributeLocation,
      identity.HasOperation,
      out EquatableArray<string> constructorArguments,
      out Diagnostic? constructorDiagnostic))
    {
      return EndpointExtractionResult.Failure(constructorDiagnostic!);
    }

    // Extract parameters and options from properties
    ImmutableArray<SegmentDefinition> segments = ExtractSegmentsFromProperties(
      classDeclaration,
      semanticModel,
      cancellationToken,
      identity.HasOperation);

    // For validated patterns, we know they are either empty or single literal
    // So we can safely use the pattern as-is (no need to parse for parameters/options)
    ImmutableArray<SegmentDefinition> patternSegments = string.IsNullOrEmpty(pattern)
      ? []
      : [new LiteralDefinition(0, pattern)];

    // Merge segments (pattern segments first, then property segments that aren't duplicates)
    ImmutableArray<SegmentDefinition> mergedSegments = MergeSegments(patternSegments, segments);

    // Get handler info (requires nested Handler class)
    HandlerDefinition? handler = ExtractHandler(
      classDeclaration,
      semanticModel,
      cancellationToken,
      identity.HasOperation,
      constructorArguments);
    Diagnostic? missingHandler = null;
    if (handler is null)
    {
      INamedTypeSymbol? classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken);
      if (classSymbol is null)
        return EndpointExtractionResult.Empty;

      bool visible = HasVisibleHandler(semanticModel.Compilation, classSymbol);
      if (!identity.HasOperation && !visible)
        return EndpointExtractionResult.Empty;

      handler = CreateMediatorHandler(
        classSymbol,
        messageType,
        handlerVisible: visible,
        semanticModel.Compilation,
        cancellationToken,
        constructorArguments);

      if (identity.HasOperation && !visible)
      {
        missingHandler = Diagnostic.Create(
          DiagnosticDescriptors.MissingHandler,
          attributeLocation,
          classSymbol.Name);
      }
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
      examples: examples,
      isDestructive: HasDestructiveAttribute(classDeclaration, semanticModel, cancellationToken));

    if (missingHandler is not null)
      return EndpointExtractionResult.SuccessWithDiagnostics(route, [missingHandler]);

    return EndpointExtractionResult.Success(route);
  }

  /// <summary>
  /// Extracts and combines aliases from the command class and group hierarchy.
  /// </summary>
  private static ImmutableArray<string> ExtractAndCombineAliases(
    TypeDeclarationSyntax classDeclaration,
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

    return [.. allAliases];
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
  private static (string? Pattern, string? Description, Location Location) ExtractNuruRouteAttribute(TypeDeclarationSyntax classDeclaration)
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
    TypeDeclarationSyntax classDeclaration,
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

        return [.. aliases];
      }
    }

    return [];
  }

  /// <summary>
  /// True when the command class carries <c>TimeWarp.Nuru.DestructiveAttribute</c>.
  /// </summary>
  private static bool HasDestructiveAttribute(
    TypeDeclarationSyntax classDeclaration,
    SemanticModel semanticModel,
    CancellationToken cancellationToken)
  {
    INamedTypeSymbol? classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken);
    if (classSymbol is null)
      return false;

    foreach (AttributeData attribute in classSymbol.GetAttributes())
    {
      if (attribute.AttributeClass?.ToDisplayString() == DestructiveAttributeFullName)
        return true;
    }

    return false;
  }

  /// <summary>
  /// Extracts usage examples from all <c>[NuruRouteExample]</c> attributes on the command class.
  /// Unlike aliases, examples use <c>AllowMultiple = true</c>, so every matching attribute is
  /// accumulated (not just the first), preserving source declaration order. Attributes with a
  /// null or empty command are silently skipped.
  /// </summary>
  private static ImmutableArray<ExampleDefinition> ExtractNuruRouteExampleAttributes(
    TypeDeclarationSyntax classDeclaration,
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

      // Normalize "" to null so both emitters (help / capabilities) see the same absent-vs-present
      // signal - capabilities' `is not null` guard would otherwise emit an empty "description": "".
      if (string.IsNullOrEmpty(description))
        description = null;

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
    TypeDeclarationSyntax classDeclaration,
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
    TypeDeclarationSyntax classDeclaration,
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

  private const string MediatorNamespace = "TimeWarp.Mediator";

  /// <summary>
  /// Infers message type from implemented <c>TimeWarp.Mediator</c> interfaces.
  /// </summary>
  /// <remarks>
  /// Precedence is Query, then IdempotentCommand, then Command, independent of interface order.
  /// <c>IIdempotentCommand&lt;T&gt;</c> derives <c>ICommand&lt;T&gt;</c>, so a first-match scan of
  /// <see cref="ITypeSymbol.AllInterfaces"/> could classify it as a plain Command.
  /// </remarks>
  private static string InferMessageType
  (
    TypeDeclarationSyntax classDeclaration,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    INamedTypeSymbol? classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken);
    if (classSymbol is null)
      return "Unspecified";

    bool isCommand = false;
    bool isIdempotent = false;

    foreach (INamedTypeSymbol iface in classSymbol.AllInterfaces)
    {
      if (!IsMediatorInterface(iface))
        continue;

      switch (iface.Name)
      {
        case "IQuery":
          return "Query";
        case "IIdempotentCommand":
          isCommand = true;
          isIdempotent = true;
          break;
        case "ICommand":
          isCommand = true;
          break;
        case "IIdempotent":
          isIdempotent = true;
          break;
      }
    }

    if (!isCommand)
      return "Unspecified";

    return isIdempotent ? "IdempotentCommand" : "Command";
  }

  /// <summary>
  /// Checks whether an interface is declared in the <c>TimeWarp.Mediator</c> namespace.
  /// </summary>
  private static bool IsMediatorInterface(INamedTypeSymbol iface) =>
    iface.ContainingNamespace?.ToDisplayString() == MediatorNamespace;

  /// <summary>
  /// Extracts segments from properties with [Parameter] or [Option] attributes.
  /// Uses semantic model to find all properties including those in partial class files.
  /// Also extracts [GroupOption] attributes from base classes.
  /// </summary>
  private static ImmutableArray<SegmentDefinition> ExtractSegmentsFromProperties
  (
    TypeDeclarationSyntax classDeclaration,
    SemanticModel semanticModel,
    CancellationToken cancellationToken,
    bool deriveUnmarkedMembers = false
  )
  {
    ImmutableArray<SegmentDefinition>.Builder segments = ImmutableArray.CreateBuilder<SegmentDefinition>();
    int position = 100; // Start after pattern segments

    // Get the type symbol to find ALL properties including those in partial classes
    INamedTypeSymbol? classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken);
    if (classSymbol is null)
      return segments.ToImmutable();

    // A derived member can redeclare a base property under a different option name.
    // Skip the base property by the declared name, not by the option spelling.
    HashSet<string> seenPropertyNames = new(StringComparer.Ordinal);

    // Iterate over all properties from the type symbol (handles partial classes correctly)
    foreach (IPropertySymbol propertySymbol in classSymbol.GetMembers().OfType<IPropertySymbol>())
    {
      seenPropertyNames.Add(propertySymbol.Name);
      SegmentDefinition? segment = ExtractSegmentFromPropertySymbol(propertySymbol, position++, semanticModel.Compilation, cancellationToken);
      if (segment is null && deriveUnmarkedMembers)
        segment = TryDeriveSegment(propertySymbol, position - 1, semanticModel.Compilation, cancellationToken);
      if (segment is not null)
        segments.Add(segment);
    }

    // Inherited public settable members are options too. Bindings already include them;
    // the route has to declare the same locals or the initializer names do not exist.
    if (deriveUnmarkedMembers)
    {
      for (INamedTypeSymbol? baseType = classSymbol.BaseType;
           baseType is not null && baseType.SpecialType != SpecialType.System_Object;
           baseType = baseType.BaseType)
      {
        // Group options stay on ExtractGroupOptionsFromBaseClasses.
        if (HasNuruRouteGroup(baseType))
          continue;

        foreach (IPropertySymbol propertySymbol in baseType.GetMembers().OfType<IPropertySymbol>())
        {
          if (!seenPropertyNames.Add(propertySymbol.Name))
            continue;

          SegmentDefinition? inherited = TryDeriveSegment(
            propertySymbol,
            position,
            semanticModel.Compilation,
            cancellationToken);
          if (inherited is null)
            continue;

          segments.Add(inherited);
          position++;
        }
      }
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
  /// Extracts the default value literal from a property symbol using DeclaringSyntaxReferences.
  /// This method correctly handles properties defined in partial class files.
  /// Prefers semantic resolution (constant value / symbol reference, emitted fully-qualified)
  /// over raw syntax text, so defaults referencing types outside the generated file's using
  /// scope (e.g. an enum member or a static field) still resolve correctly.
  /// </summary>
  /// <param name="property">The property symbol.</param>
  /// <param name="compilation">Compilation used to obtain a SemanticModel for the initializer, if available.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>The C# literal and the capabilities display, or nulls when the property has no initializer.</returns>
  private static PropertyDefault ExtractPropertyDefaultValueFromSymbol(IPropertySymbol property, Compilation? compilation, CancellationToken cancellationToken)
  {
    // Get syntax references for this property (handles partial classes correctly)
    ImmutableArray<SyntaxReference> syntaxReferences = property.DeclaringSyntaxReferences;
    if (syntaxReferences.Length == 0)
      return default;

    // A positional record property is declared by its primary-constructor parameter.
    // The parameter default, when there is one, is the default.
    Microsoft.CodeAnalysis.SyntaxNode? syntaxNode = syntaxReferences[0].GetSyntax(cancellationToken);
    if (syntaxNode is Microsoft.CodeAnalysis.CSharp.Syntax.ParameterSyntax parameterSyntax)
    {
      if (compilation is null)
        return default;

      SemanticModel parameterModel = compilation.GetSemanticModel(syntaxReferences[0].SyntaxTree);
      return parameterModel.GetDeclaredSymbol(parameterSyntax, cancellationToken) is IParameterSymbol declared
        ? ParameterDefault(declared, property.Type)
        : default;
    }

    if (syntaxNode is not PropertyDeclarationSyntax propertySyntax)
      return default;

    // Check for property initializer (e.g., public int X { get; set; } = 1;)
    if (propertySyntax.Initializer?.Value is not { } initializerValue)
      return default;

    if (compilation is not null)
    {
      SemanticModel semanticModel = compilation.GetSemanticModel(syntaxReferences[0].SyntaxTree);
      SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(initializerValue, cancellationToken);
      ISymbol? symbol = symbolInfo.Symbol;
      ITypeSymbol valueType = UnwrapNullableType(property.Type);

      // `= Name` copies a primary-constructor parameter. Its default is the parameter default.
      // Any other initializer that reads a parameter cannot be copied and has no default.
      if (symbol is IParameterSymbol initializerParameter)
        return ParameterDefault(initializerParameter, property.Type);

      if (ReferencesParameter(initializerValue, semanticModel, cancellationToken))
        return default;

      // Enum members have a numeric constant value. Keep the member so the generated
      // assignment stays typed. Capabilities show the member name only.
      if (symbol is IFieldSymbol enumField && enumField.ContainingType.TypeKind == TypeKind.Enum)
      {
        return new PropertyDefault(enumField.ToDisplayString(FullyQualifiedMemberFormat), enumField.Name);
      }

      Optional<object?> constantValue = semanticModel.GetConstantValue(initializerValue, cancellationToken);
      if (constantValue.HasValue
          && constantValue.Value is not null
          && valueType.TypeKind == TypeKind.Enum
          && valueType is INamedTypeSymbol enumType)
      {
        string display = EnumDisplay(enumType, constantValue.Value);
        if (symbol is IFieldSymbol or IPropertySymbol)
          return new PropertyDefault(symbol.ToDisplayString(FullyQualifiedMemberFormat), display);

        return new PropertyDefault(FormatEnumCast(enumType, constantValue.Value), display);
      }

      if (constantValue.HasValue)
        return FormatConstantLiteral(constantValue.Value, property.Type);

      // A static readonly field or a property is copied as code. Its value is not known
      // at build time, so capabilities omit it.
      if (symbol is IFieldSymbol or IPropertySymbol)
        return new PropertyDefault(symbol.ToDisplayString(FullyQualifiedMemberFormat), null);

      // new(), new Foo(), and method calls are not a capabilities default.
      // Collection expressions stay as syntax so `[]` still compiles.
      if (initializerValue is CollectionExpressionSyntax
          or ArrayCreationExpressionSyntax
          or ImplicitArrayCreationExpressionSyntax)
      {
        string syntax = initializerValue.ToString();
        return new PropertyDefault(syntax, syntax);
      }

      if (initializerValue is ObjectCreationExpressionSyntax
          or ImplicitObjectCreationExpressionSyntax
          or InvocationExpressionSyntax
          || symbol is IMethodSymbol)
      {
        return new PropertyDefault(UnemittableDefaultLiteral, null);
      }
    }

    string text = initializerValue.ToString();
    return new PropertyDefault(text, text);
  }

  /// <summary>
  /// Emits a C# literal for a compile-time constant, and a suffix-free display value.
  /// </summary>
  private static PropertyDefault FormatConstantLiteral(object? value, ITypeSymbol propertyType)
  {
    switch (value)
    {
      case string text:
      {
        string quoted = SymbolDisplay.FormatLiteral(text, quote: true);
        return new PropertyDefault(quoted, quoted);
      }
      case char character:
      {
        string quoted = SymbolDisplay.FormatLiteral(character, quote: true);
        return new PropertyDefault(quoted, quoted);
      }
      case bool flag:
      {
        string text = flag ? "true" : "false";
        return new PropertyDefault(text, text);
      }
      case null:
      {
        if (propertyType.IsReferenceType && propertyType.NullableAnnotation != NullableAnnotation.Annotated)
          return new PropertyDefault(NullForgivingDefaultLiteral, null);

        // `= default` on a non-nullable struct, typically a parameter default.
        if (propertyType.IsValueType && propertyType.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T)
          return new PropertyDefault("default", null);

        return new PropertyDefault("null", "null");
      }
      case float number:
      {
        if (float.IsNaN(number))
          return new PropertyDefault("float.NaN", "NaN");

        if (float.IsPositiveInfinity(number))
          return new PropertyDefault("float.PositiveInfinity", "Infinity");

        if (float.IsNegativeInfinity(number))
          return new PropertyDefault("float.NegativeInfinity", "-Infinity");

        string plain = number.ToString("R", global::System.Globalization.CultureInfo.InvariantCulture);
        return new PropertyDefault(FormatRealLiteral(plain, "f"), TrimRealDisplay(plain));
      }
      case double number:
      {
        if (double.IsNaN(number))
          return new PropertyDefault("double.NaN", "NaN");

        if (double.IsPositiveInfinity(number))
          return new PropertyDefault("double.PositiveInfinity", "Infinity");

        if (double.IsNegativeInfinity(number))
          return new PropertyDefault("double.NegativeInfinity", "-Infinity");

        string plain = number.ToString("R", global::System.Globalization.CultureInfo.InvariantCulture);
        return new PropertyDefault(plain, TrimRealDisplay(plain));
      }
      case decimal number:
      {
        string plain = number.ToString(global::System.Globalization.CultureInfo.InvariantCulture);
        return new PropertyDefault(FormatRealLiteral(plain, "m"), TrimRealDisplay(plain));
      }
      case uint number:
      {
        string plain = number.ToString(global::System.Globalization.CultureInfo.InvariantCulture);
        return new PropertyDefault(plain + "u", plain);
      }
      case long number:
      {
        string plain = number.ToString(global::System.Globalization.CultureInfo.InvariantCulture);
        return new PropertyDefault(plain + "L", plain);
      }
      case ulong number:
      {
        string plain = number.ToString(global::System.Globalization.CultureInfo.InvariantCulture);
        return new PropertyDefault(plain + "UL", plain);
      }
      case IFormattable formattable:
      {
        string plain = formattable.ToString(null, global::System.Globalization.CultureInfo.InvariantCulture);
        return new PropertyDefault(plain, plain);
      }
      default:
      {
        string plain = value.ToString() ?? UnemittableDefaultLiteral;
        return new PropertyDefault(plain, plain == UnemittableDefaultLiteral ? null : plain);
      }
    }
  }

  private static string TrimRealDisplay(string text)
  {
    if (text.EndsWith(".0", StringComparison.Ordinal))
      return text[..^2];

    return text;
  }

  private static ITypeSymbol UnwrapNullableType(ITypeSymbol type)
  {
    if (type is INamedTypeSymbol named
        && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
        && named.TypeArguments.Length == 1)
    {
      return named.TypeArguments[0];
    }

    return type;
  }

  private static bool PropertyIsInitOnly(IPropertySymbol property) =>
    property.SetMethod is { IsInitOnly: true };

  /// <summary>
  /// Default from a primary-constructor parameter: its explicit default, or none.
  /// </summary>
  private static PropertyDefault ParameterDefault(IParameterSymbol parameter, ITypeSymbol propertyType)
  {
    if (!parameter.HasExplicitDefaultValue)
      return default;

    object? value = parameter.ExplicitDefaultValue;
    if (value is not null
        && UnwrapNullableType(propertyType) is INamedTypeSymbol { TypeKind: TypeKind.Enum } enumType)
    {
      return new PropertyDefault(FormatEnumCast(enumType, value), EnumDisplay(enumType, value));
    }

    return FormatConstantLiteral(value, propertyType);
  }

  private static bool ReferencesParameter(ExpressionSyntax initializer, SemanticModel semanticModel, CancellationToken cancellationToken)
  {
    foreach (IdentifierNameSyntax identifier in initializer.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
    {
      if (semanticModel.GetSymbolInfo(identifier, cancellationToken).Symbol is IParameterSymbol)
        return true;
    }

    return false;
  }

  /// <summary>
  /// The member name, the comma-joined member names of an exact <c>[Flags]</c> decomposition,
  /// or the number.
  /// </summary>
  private static string EnumDisplay(INamedTypeSymbol enumType, object value)
  {
    if (MatchingEnumMemberName(enumType, value) is { } memberName)
      return memberName;

    if (FlagsMemberNames(enumType, value) is { } flagNames)
      return flagNames;

    return IntegralDisplay(value);
  }

  private static string? FlagsMemberNames(INamedTypeSymbol enumType, object value)
  {
    bool isFlags = enumType.GetAttributes().Any(attribute =>
      attribute.AttributeClass?.ToDisplayString() == "System.FlagsAttribute");
    if (!isFlags || !TryIntegralBits(UnwrapEnumBox(value), out ulong remaining) || remaining == 0)
      return null;

    // Same order as Enum.ToString: largest members first, listed smallest first.
    List<(ulong Bits, string Name)> members = [];
    foreach (IFieldSymbol field in enumType.GetMembers().OfType<IFieldSymbol>())
    {
      if (field.HasConstantValue
          && TryIntegralBits(UnwrapEnumBox(field.ConstantValue), out ulong bits)
          && bits != 0)
      {
        members.Add((bits, field.Name));
      }
    }

    List<string> names = [];
    foreach ((ulong bits, string name) in members.OrderByDescending(member => member.Bits))
    {
      if ((remaining & bits) == bits)
      {
        names.Add(name);
        remaining &= ~bits;
      }
    }

    if (remaining != 0)
      return null;

    names.Reverse();
    return string.Join(", ", names);
  }

  private static string? MatchingEnumMemberName(INamedTypeSymbol enumType, object? value)
  {
    if (value is null)
      return null;

    foreach (IFieldSymbol field in enumType.GetMembers().OfType<IFieldSymbol>())
    {
      if (!field.HasConstantValue || field.ConstantValue is null)
        continue;

      if (EnumBitsEqual(field.ConstantValue, value))
        return field.Name;
    }

    return null;
  }

  private static string FormatEnumCast(INamedTypeSymbol enumType, object? value)
  {
    string typeName = enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    return $"({typeName}){FormatIntegralLiteral(value)}";
  }

  private static string IntegralDisplay(object? value)
  {
    object? unwrapped = UnwrapEnumBox(value);
    return unwrapped switch
    {
      byte number => number.ToString(global::System.Globalization.CultureInfo.InvariantCulture),
      sbyte number => number.ToString(global::System.Globalization.CultureInfo.InvariantCulture),
      short number => number.ToString(global::System.Globalization.CultureInfo.InvariantCulture),
      ushort number => number.ToString(global::System.Globalization.CultureInfo.InvariantCulture),
      int number => number.ToString(global::System.Globalization.CultureInfo.InvariantCulture),
      uint number => number.ToString(global::System.Globalization.CultureInfo.InvariantCulture),
      long number => number.ToString(global::System.Globalization.CultureInfo.InvariantCulture),
      ulong number => number.ToString(global::System.Globalization.CultureInfo.InvariantCulture),
      _ => "0"
    };
  }

  private static string FormatIntegralLiteral(object? value)
  {
    object? unwrapped = UnwrapEnumBox(value);
    return unwrapped switch
    {
      byte number => number.ToString(global::System.Globalization.CultureInfo.InvariantCulture),
      sbyte number => number.ToString(global::System.Globalization.CultureInfo.InvariantCulture),
      short number => number.ToString(global::System.Globalization.CultureInfo.InvariantCulture),
      ushort number => number.ToString(global::System.Globalization.CultureInfo.InvariantCulture),
      int number => number.ToString(global::System.Globalization.CultureInfo.InvariantCulture),
      uint number => number.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "u",
      long number => number.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "L",
      ulong number => number.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "UL",
      _ => "0"
    };
  }

  private static object? UnwrapEnumBox(object? value)
  {
    if (value is Enum enumValue)
    {
      return Convert.ChangeType(
        enumValue,
        Enum.GetUnderlyingType(enumValue.GetType()),
        global::System.Globalization.CultureInfo.InvariantCulture);
    }

    return value;
  }

  private static bool EnumBitsEqual(object fieldValue, object defaultValue)
  {
    object? fieldBox = UnwrapEnumBox(fieldValue);
    object? defaultBox = UnwrapEnumBox(defaultValue);
    if (object.Equals(fieldBox, defaultBox))
      return true;

    return TryIntegralBits(fieldBox, out ulong fieldBits)
      && TryIntegralBits(defaultBox, out ulong defaultBits)
      && fieldBits == defaultBits;
  }

  private static bool TryIntegralBits(object? value, out ulong bits)
  {
    switch (value)
    {
      case byte number:
        bits = number;
        return true;
      case sbyte number:
        bits = unchecked((ulong)number);
        return true;
      case short number:
        bits = unchecked((ulong)number);
        return true;
      case ushort number:
        bits = number;
        return true;
      case int number:
        bits = unchecked((ulong)number);
        return true;
      case uint number:
        bits = number;
        return true;
      case long number:
        bits = unchecked((ulong)number);
        return true;
      case ulong number:
        bits = number;
        return true;
      default:
        bits = 0;
        return false;
    }
  }

  private static string FormatRealLiteral(string text, string suffix)
  {
    if (text.IndexOf('.', StringComparison.Ordinal) < 0
        && text.IndexOf('E', StringComparison.Ordinal) < 0
        && text.IndexOf('e', StringComparison.Ordinal) < 0)
    {
      text += ".0";
    }

    return text + suffix;
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
    TypeDeclarationSyntax classDeclaration,
    SemanticModel semanticModel,
    CancellationToken cancellationToken,
    bool hasOperation = false,
    EquatableArray<string> constructorArguments = default
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

    // 4. Infer return type from interface
    HandlerReturnType returnType = InferReturnTypeFromInterfaces(classSymbol);
    ResponseShape shape = DescribeResponse(semanticModel.Compilation, classSymbol, InferMessageType(classDeclaration, semanticModel, cancellationToken));
    ImmutableArray<ParameterBinding> propertyBindings = BuildPropertyBindings(
      classSymbol,
      hasOperation,
      semanticModel.Compilation,
      cancellationToken);

    return HandlerDefinition.ForCommand(
      commandTypeName: commandTypeName,
      nestedHandlerTypeName: handlerTypeName,
      propertyBindings: propertyBindings,
      constructorDependencies: constructorDeps,
      returnType: returnType) with
    {
      HandlerInterfaceName = shape.HandlerInterfaceName,
      ReturnsProblemPair = shape.ReturnsProblemPair,
      SuccessTypeName = shape.SuccessTypeName,
      RendererTypeName = shape.RendererTypeName,
      ConstructorArguments = constructorArguments
    };
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
      if (!iface.IsGenericType || !IsMediatorInterface(iface))
        continue;

      if (iface.Name is not ("IQuery" or "ICommand" or "IIdempotentCommand"))
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
    TypeDeclarationSyntax classDeclaration,
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
      // Skip mediator message/handler interfaces (ICommand, IQuery, IRequest, IIdempotent, etc.)
      if (IsMediatorInterface(iface))
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
      "sbyte" or "global::System.SByte" => "sbyte",
      "ushort" or "global::System.UInt16" => "ushort",
      "uint" or "global::System.UInt32" => "uint",
      "ulong" or "global::System.UInt64" => "ulong",
      "global::System.DateOnly" or "System.DateOnly" or "DateOnly" => "dateonly",
      "global::System.TimeOnly" or "System.TimeOnly" or "TimeOnly" => "timeonly",
      "global::System.Numerics.BigInteger" or "System.Numerics.BigInteger" or "BigInteger" => "biginteger",
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
