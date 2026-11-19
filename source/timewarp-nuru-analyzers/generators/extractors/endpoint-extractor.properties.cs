#region Purpose
// EndpointExtractor partial: message type inference and [Parameter] property segment extraction.
#endregion

#region Design
// [Parameter] Order is the positional order. Two or more parameters that omit it report NURU_A002.
// Name, when set, is the route segment name (lowercased) and must be an identifier.
// A name that is not an identifier reports NURU_A005 and the property name is used instead.
// A catch-all stays last.
// [Option] reading lives in endpoint-extractor.option-attributes.cs.
#endregion

namespace TimeWarp.Nuru.Generators;

internal static partial class EndpointExtractor
{
  /// <summary>
  /// Infers message type from implemented <c>TimeWarp.Mediator</c> interfaces.
  /// </summary>
  /// <remarks>
  /// Precedence is Query, then IdempotentCommand, then Command, independent of interface order.
  /// <c>IIdempotentCommand&lt;T&gt;</c> derives <c>ICommand&lt;T&gt;</c>, so a first-match scan of
  /// <see cref="ITypeSymbol.AllInterfaces"/> could classify it as a plain Command.
  /// </remarks>
  private static string InferMessageType(INamedTypeSymbol classSymbol)
  {
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
    INamedTypeSymbol classSymbol,
    Compilation compilation,
    ImmutableArray<Diagnostic>.Builder diagnostics,
    bool deriveUnmarkedMembers,
    CancellationToken cancellationToken
  )
  {
    ImmutableArray<SegmentDefinition>.Builder segments = ImmutableArray.CreateBuilder<SegmentDefinition>();
    List<PendingParameter> pendingParameters = [];
    int position = 100; // Start after pattern segments
    int declarationIndex = 0;

    // A derived member can redeclare a base property under a different option name.
    // Skip the base property by the declared name, not by the option spelling.
    HashSet<string> seenPropertyNames = [with(StringComparer.Ordinal)];

    // Iterate over all properties from the type symbol (handles partial classes correctly)
    foreach (IPropertySymbol propertySymbol in classSymbol.GetMembers().OfType<IPropertySymbol>())
    {
      seenPropertyNames.Add(propertySymbol.Name);
      SegmentDefinition? segment = ExtractSegmentFromPropertySymbol(
        propertySymbol,
        position++,
        compilation,
        diagnostics,
        out int parameterOrder,
        out bool isParameter,
        cancellationToken);
      if (segment is null && deriveUnmarkedMembers)
        segment = TryDeriveSegment(propertySymbol, position - 1, compilation, cancellationToken);
      if (segment is null)
        continue;

      if (isParameter)
      {
        pendingParameters.Add(new PendingParameter(
          segments.Count,
          parameterOrder,
          declarationIndex,
          segment is ParameterDefinition { IsCatchAll: true },
          propertySymbol.Name,
          propertySymbol.Locations.FirstOrDefault() ?? Location.None));
        declarationIndex++;
      }

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
            compilation,
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
      classSymbol,
      compilation,
      position,
      cancellationToken);
    segments.AddRange(groupOptions);

    ReportMissingParameterOrder(pendingParameters, diagnostics);
    ReorderParametersByOrder(segments, pendingParameters);
    return segments.ToImmutable();
  }

  /// <summary>
  /// One [Parameter] waiting to be ordered. SegmentIndex is its slot in the segment list.
  /// </summary>
  private readonly record struct PendingParameter(
    int SegmentIndex,
    int Order,
    int DeclarationIndex,
    bool IsCatchAll,
    string PropertyName,
    Location Location);

  /// <summary>
  /// Two or more parameters that leave Order at its default each report NURU_A002.
  /// A single parameter may omit Order.
  /// </summary>
  private static void ReportMissingParameterOrder(
    List<PendingParameter> parameters,
    ImmutableArray<Diagnostic>.Builder diagnostics)
  {
    if (parameters.Count < 2)
      return;

    foreach (PendingParameter parameter in parameters)
    {
      if (parameter.Order >= 0)
        continue;

      diagnostics.Add(Diagnostic.Create(
        DiagnosticDescriptors.MultipleParametersRequireOrder,
        parameter.Location,
        parameter.PropertyName));
    }
  }

  /// <summary>
  /// Writes parameters back into their slots in Order, then declaration order.
  /// A catch-all stays in the last parameter slot.
  /// </summary>
  private static void ReorderParametersByOrder(
    ImmutableArray<SegmentDefinition>.Builder segments,
    List<PendingParameter> parameters)
  {
    if (parameters.Count < 2)
      return;

    List<PendingParameter> sorted = [.. parameters];
    sorted.Sort(static (left, right) =>
    {
      int catchAll = left.IsCatchAll.CompareTo(right.IsCatchAll);
      if (catchAll != 0)
        return catchAll;

      int leftOrder = left.Order < 0 ? int.MaxValue : left.Order;
      int rightOrder = right.Order < 0 ? int.MaxValue : right.Order;
      int byOrder = leftOrder.CompareTo(rightOrder);
      return byOrder != 0 ? byOrder : left.DeclarationIndex.CompareTo(right.DeclarationIndex);
    });

    SegmentDefinition[] ordered = new SegmentDefinition[sorted.Count];
    int[] slots = new int[parameters.Count];
    for (int i = 0; i < sorted.Count; i++)
    {
      ordered[i] = segments[sorted[i].SegmentIndex];
      slots[i] = parameters[i].SegmentIndex;
    }

    Array.Sort(slots);
    for (int i = 0; i < slots.Length; i++)
      segments[slots[i]] = ordered[i];
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
    ImmutableArray<Diagnostic>.Builder diagnostics,
    out int parameterOrder,
    out bool isParameter,
    CancellationToken cancellationToken
  )
  {
    parameterOrder = -1;
    isParameter = false;

    foreach (AttributeData attribute in propertySymbol.GetAttributes())
    {
      string? attributeName = attribute.AttributeClass?.Name;

      if (attributeName == ParameterAttributeName || attributeName == $"{ParameterAttributeName}Attribute")
      {
        isParameter = true;
        return ExtractParameterFromAttribute(propertySymbol, attribute, position, diagnostics, out parameterOrder);
      }

      if (attributeName == OptionAttributeName || attributeName == $"{OptionAttributeName}Attribute")
      {
        return ExtractOptionFromAttribute(propertySymbol, attribute, position, compilation, diagnostics, cancellationToken);
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
    int position,
    ImmutableArray<Diagnostic>.Builder diagnostics,
    out int order
  )
  {
    string? description = null;
    bool isCatchAll = false;
    bool isOptional = property.NullableAnnotation == NullableAnnotation.Annotated;
    order = -1;

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
        case "Order":
          if (namedArg.Value.Value is int orderValue)
            order = orderValue;
          break;
      }
    }

    string typeName = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    string? typeConstraint = GetTypeConstraintFromClrType(typeName);

    return new ParameterDefinition(
      Position: position,
      Name: ParameterSegmentName(attribute, property, diagnostics),
      TypeConstraint: typeConstraint,
      Description: description,
      IsOptional: isOptional,
      IsCatchAll: isCatchAll,
      ResolvedClrTypeName: typeName);
  }

  /// <summary>
  /// Route segment name for a [Parameter]: the Name override when it is set, otherwise the property name.
  /// The matcher local and the command-construction local both derive from this string.
  /// A name that is not an identifier reports NURU_A005 and the property name is used, so both locals compile.
  /// </summary>
  private static string ParameterSegmentName
  (
    AttributeData attribute,
    IPropertySymbol property,
    ImmutableArray<Diagnostic>.Builder? diagnostics
  )
  {
    foreach (KeyValuePair<string, TypedConstant> namedArg in attribute.NamedArguments)
    {
      if (namedArg.Key == "Name" && namedArg.Value.Value is string name && !string.IsNullOrWhiteSpace(name))
      {
        string trimmed = name.Trim();
        if (!IsValidParameterName(trimmed))
        {
          diagnostics?.Add(Diagnostic.Create(
            DiagnosticDescriptors.InvalidParameterName,
            property.Locations.FirstOrDefault() ?? Location.None,
            trimmed));
          return property.Name.ToLowerInvariant();
        }

        return trimmed.ToLowerInvariant();
      }
    }

    return property.Name.ToLowerInvariant();
  }

  /// <summary>
  /// Same identifier rule the route parser uses: a letter or underscore, then letters, digits, and underscores.
  /// </summary>
  private static bool IsValidParameterName(string name)
  {
    if (name.Length == 0)
      return false;

    char first = name[0];
    if (!char.IsLetter(first) && first != '_')
      return false;

    for (int i = 1; i < name.Length; i++)
    {
      char character = name[i];
      if (!char.IsLetterOrDigit(character) && character != '_')
        return false;
    }

    return true;
  }
}
