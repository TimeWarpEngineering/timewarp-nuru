#region Purpose
// Extracts route definitions from [NuruRoute] endpoint classes: pattern, group prefix, message type,
// [Parameter]/[Option] properties, and the nested Handler class.
#endregion

#region Design
// ENDPOINT SCOPING: All [NuruRoute] classes in a compilation are collected globally by the
// source generator. They are filtered per-app via FilterEndpointsForApp() during emission.
// .DiscoverEndpoints() includes ALL endpoints; .Map<T>() includes only that type.
// Extraction runs on the type symbol, so a referenced-assembly type (ReferencedEndpointLocator)
// goes through the same code. A diagnostic whose attribute syntax is not in this compilation
// (metadata, or an IDE project reference) uses the opt-in site.
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
// A property with no syntax in this compilation (a referenced assembly) has an unknown
// initializer: default!, so the compiled initializer runs, with [DefaultValue] as the capabilities
// display only. A positional record parameter default is in metadata and is copied.
//
// TOOL HINTS: Destructive and OpenWorld are TimeWarp.Contracts attributes, matched by metadata
// name, on every endpoint class. Absence is unset. No argument, or any argument other than false,
// is true. TimeWarp.Nuru does not define those attributes, so one short name exists.
// An operation's title is OperationAttribute.Title. Blank or whitespace is no title, and
// [DisplayName] is not a title source for an operation. A [NuruRoute] class with no [Operation]
// reads [DisplayName], because it has no operation attribute to carry a title.
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
  private const string DestructiveAttributeFullName = "TimeWarp.Contracts.DestructiveAttribute";
  private const string LocalOnlyAttributeFullName = "TimeWarp.Nuru.LocalOnlyAttribute";
  private const string ExclusiveAttributeFullName = "TimeWarp.Nuru.ExclusiveAttribute";
  private const string OpenWorldAttributeFullName = "TimeWarp.Contracts.OpenWorldAttribute";
  private const string DisplayNameAttributeFullName = "System.ComponentModel.DisplayNameAttribute";
  private const string DefaultValueAttributeFullName = "System.ComponentModel.DefaultValueAttribute";
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
    INamedTypeSymbol? classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken);
    if (classSymbol is null)
      return EndpointExtractionResult.Empty;

    return Extract(classSymbol, semanticModel.Compilation, classDeclaration.Identifier.GetLocation(), cancellationToken);
  }

  /// <summary>
  /// Extracts a RouteDefinition from a [NuruRoute] or [Operation] type symbol.
  /// The type can be declared in this compilation or in a referenced assembly.
  /// </summary>
  /// <param name="classSymbol">The endpoint type.</param>
  /// <param name="compilation">The compilation the generated code belongs to.</param>
  /// <param name="fallbackLocation">
  /// Diagnostic location when an attribute has no syntax, as for a referenced-assembly type.
  /// </param>
  /// <param name="cancellationToken">Cancellation token.</param>
  /// <returns>An extraction result containing the route definition and any diagnostics.</returns>
  public static EndpointExtractionResult Extract
  (
    INamedTypeSymbol classSymbol,
    Compilation compilation,
    Location fallbackLocation,
    CancellationToken cancellationToken
  )
  {
    RouteIdentity identity = ResolveRouteIdentity(classSymbol, compilation, fallbackLocation);
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
    string messageType = InferMessageType(classSymbol);
    if (!identity.HasNuruRoute && messageType == "Unspecified")
      return EndpointExtractionResult.Empty;

    // VALIDATE: Pattern must be empty string OR a single literal only
    Diagnostic? patternDiagnostic = ValidateRoutePattern(pattern, attributeLocation);
    if (patternDiagnostic is not null)
    {
      return EndpointExtractionResult.Failure(patternDiagnostic);
    }

    // Get the group info from base class hierarchy - always collect full hierarchy.
    // A non-blank [Operation(Group)] is metadata only and replaces any route-group prefix.
    EquatableArray<string> operationGroupPath = SplitOperationGroup(identity.OperationGroup);
    GroupInfo groupInfo = ExtractGroupInfo(classSymbol);
    groupInfo = operationGroupPath.IsDefaultOrEmpty
      ? ApplyCliBindingGroup(groupInfo, classSymbol)
      : WithoutRoutePrefix(groupInfo);

    if (!TrySelectConstructorArguments(
      classSymbol,
      compilation,
      attributeLocation,
      identity.HasOperation,
      out EquatableArray<string> constructorArguments,
      out Diagnostic? constructorDiagnostic))
    {
      return EndpointExtractionResult.Failure(constructorDiagnostic!);
    }

    // Extract parameters and options from properties
    ImmutableArray<SegmentDefinition> segments = ExtractSegmentsFromProperties(
      classSymbol,
      compilation,
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
      classSymbol,
      compilation,
      cancellationToken,
      identity.HasOperation,
      constructorArguments);
    Diagnostic? missingHandler = null;
    if (handler is null)
    {
      bool visible = HasVisibleHandler(compilation, classSymbol);
      if (!identity.HasOperation && !visible)
        return EndpointExtractionResult.Empty;

      handler = CreateMediatorHandler(
        classSymbol,
        messageType,
        handlerVisible: visible,
        compilation,
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
    ImmutableArray<InterfaceImplementationDefinition> filterInterfaces = ExtractFilterInterfaces(classSymbol);

    // Calculate specificity
    int specificity = mergedSegments.Sum(s => s.SpecificityContribution);

    // Extract and combine aliases
    ImmutableArray<string> aliases = ExtractAndCombineAliases(classSymbol, groupInfo, pattern);

    // Extract usage examples (all [NuruRouteExample] attributes, in source order)
    ImmutableArray<ExampleDefinition> examples = ExtractNuruRouteExampleAttributes(classSymbol);

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
      destructive: BoolAttributeValue(classSymbol, DestructiveAttributeFullName),
      isLocalOnly: HasAttribute(classSymbol, LocalOnlyAttributeFullName),
      groupPath: operationGroupPath,
      isExclusive: HasAttribute(classSymbol, ExclusiveAttributeFullName),
      openWorld: BoolAttributeValue(classSymbol, OpenWorldAttributeFullName),
      title: TitleValue(classSymbol, identity.HasOperation));

    if (missingHandler is not null)
      return EndpointExtractionResult.SuccessWithDiagnostics(route, [missingHandler]);

    return EndpointExtractionResult.Success(route);
  }

  private const string MediatorNamespace = "TimeWarp.Mediator";
}
