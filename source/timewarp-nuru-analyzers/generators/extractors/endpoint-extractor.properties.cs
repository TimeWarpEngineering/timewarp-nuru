#region Purpose
// EndpointExtractor partial: message type inference and [Parameter]/[Option] property segment extraction.
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
    CancellationToken cancellationToken,
    bool deriveUnmarkedMembers = false
  )
  {
    ImmutableArray<SegmentDefinition>.Builder segments = ImmutableArray.CreateBuilder<SegmentDefinition>();
    int position = 100; // Start after pattern segments

    // A derived member can redeclare a base property under a different option name.
    // Skip the base property by the declared name, not by the option spelling.
    HashSet<string> seenPropertyNames = new(StringComparer.Ordinal);

    // Iterate over all properties from the type symbol (handles partial classes correctly)
    foreach (IPropertySymbol propertySymbol in classSymbol.GetMembers().OfType<IPropertySymbol>())
    {
      seenPropertyNames.Add(propertySymbol.Name);
      SegmentDefinition? segment = ExtractSegmentFromPropertySymbol(propertySymbol, position++, compilation, cancellationToken);
      if (segment is null && deriveUnmarkedMembers)
        segment = TryDeriveSegment(propertySymbol, position - 1, compilation, cancellationToken);
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
    if (clrTypeName.EndsWith('?', StringComparison.Ordinal))
    {
      string baseType = clrTypeName[..^1];
      string? baseConstraint = GetTypeConstraintFromClrType(baseType);
      return baseConstraint is not null ? $"{baseConstraint}?" : null;
    }

    // Handle Nullable<T> generic syntax (e.g., "global::System.Nullable<global::System.Int64>")
    if (clrTypeName.Contains("System.Nullable<", StringComparison.Ordinal))
    {
      int start = clrTypeName.IndexOf('<', StringComparison.Ordinal) + 1;
      int end = clrTypeName.LastIndexOf('>', StringComparison.Ordinal);
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
