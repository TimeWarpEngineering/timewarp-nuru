#region Purpose
// EndpointExtractor partial: nested Handler extraction, filter interfaces and segment merging.
#endregion

namespace TimeWarp.Nuru.Generators;

internal static partial class EndpointExtractor
{
  /// <summary>
  /// Extracts handler information for endpoints.
  /// Finds nested Handler class and extracts its constructor dependencies.
  /// </summary>
  /// <returns>The handler definition, or null if no nested Handler class is found.</returns>
  private static HandlerDefinition? ExtractHandler
  (
    INamedTypeSymbol classSymbol,
    Compilation compilation,
    CancellationToken cancellationToken,
    bool hasOperation = false,
    EquatableArray<string> constructorArguments = default
  )
  {
    // 1. Find nested Handler class (required). A referenced assembly's Handler must be
    // accessible from this compilation, or generated code cannot name it.
    INamedTypeSymbol? handlerClass = FindNestedHandler(compilation, classSymbol);
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
    ResponseShape shape = DescribeResponse(compilation, classSymbol);
    ImmutableArray<ParameterBinding> propertyBindings = BuildPropertyBindings(
      classSymbol,
      hasOperation,
      compilation,
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
      ConstructorArguments = constructorArguments,
      ProblemRendererTypeName = shape.ProblemRendererTypeName,
      NestedHandlerFactoryConstructible = IsFactoryConstructible(compilation, classSymbol, handlerClass)
    };
  }

  /// <summary>
  /// True when generated code can construct <paramref name="handlerClass"/> exactly as the
  /// Microsoft DI container would: the handler is declared in this compilation, is not abstract
  /// or generic, and has one public constructor whose parameters are all plain required services
  /// (no defaults, no <c>params</c>, no <c>ref</c>). The generator then swaps the mediator's
  /// reflection-activated registration for an explicit <c>new</c> factory, so CA1812 in a
  /// consumer sees the handler instantiated (kanban 573).
  /// </summary>
  private static bool IsFactoryConstructible(Compilation compilation, INamedTypeSymbol commandClass, INamedTypeSymbol handlerClass)
  {
    if (!IsDeclaredIn(compilation, handlerClass)
        || handlerClass.IsAbstract
        || handlerClass.IsGenericType
        || commandClass.IsGenericType)
    {
      return false;
    }

    IMethodSymbol[] publicConstructors =
    [
      .. handlerClass.InstanceConstructors.Where(c => c.DeclaredAccessibility == Accessibility.Public)
    ];

    if (publicConstructors.Length != 1)
      return false;

    return publicConstructors[0].Parameters.All
    (
      p => p.RefKind == RefKind.None && !p.IsOptional && !p.IsParams && !p.HasExplicitDefaultValue
    );
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
  private static ImmutableArray<InterfaceImplementationDefinition> ExtractFilterInterfaces(INamedTypeSymbol classSymbol)
  {
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
}
