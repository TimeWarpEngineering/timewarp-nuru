#region Purpose
// EndpointExtractor partial: [Option] attribute reading, repeatability, and CLR type constraints.
#endregion

#region Design
// IsRepeated = true on a non-collection reports NURU_A004. A collection repeats from its type
// even when IsRepeated is left false. A non-nullable bool whose initializer is true is a value
// option, so absence keeps true. The property initializer applies only when the option is absent.
#endregion

namespace TimeWarp.Nuru.Generators;

internal static partial class EndpointExtractor
{
  /// <summary>
  /// A non-nullable bool whose initializer is the constant true. Absence must keep true,
  /// so the option takes a value instead of starting from a flag's false.
  /// </summary>
  private static bool IsTrueBoolDefault(PropertyDefault propertyDefault) =>
    propertyDefault.Code == "true"
    || (propertyDefault.Code == UnemittableDefaultLiteral && propertyDefault.Display == "true");

  /// <summary>
  /// Extracts an OptionDefinition from an [Option] attribute.
  /// </summary>
  private static OptionDefinition ExtractOptionFromAttribute
  (
    IPropertySymbol property,
    AttributeData attribute,
    int position,
    Compilation? compilation,
    ImmutableArray<Diagnostic>.Builder diagnostics,
    CancellationToken cancellationToken
  )
  {
    string longForm = property.Name.ToLowerInvariant();
    string? shortForm = null;
    string? description = null;
    bool isOptional = true; // Options are optional by default
    bool? repeatedArgument = null;

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
        case "IsRepeated":
          repeatedArgument = namedArg.Value.Value is true;
          break;
      }
    }

    string typeName = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    // A collection repeats from its type. IsRepeated = false does not turn it into one value.
    // IsRepeated = true on a scalar is an error; the option stays a single value.
    bool isRepeated = IsRepeatedOptionType(property.Type);
    if (repeatedArgument is true && !isRepeated)
    {
      diagnostics.Add(Diagnostic.Create(
        DiagnosticDescriptors.ScalarOptionIsRepeated,
        property.Locations.FirstOrDefault() ?? Location.None,
        longForm,
        property.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
    }

    // Extract default value from property initializer via DeclaringSyntaxReferences
    // This handles properties defined in partial class files correctly
    PropertyDefault propertyDefault = ExtractPropertyDefaultValueFromSymbol(property, compilation, cancellationToken);
    bool isBool = typeName is "bool" or "global::System.Boolean";
    bool isFlag = isBool && !IsTrueBoolDefault(propertyDefault);

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
