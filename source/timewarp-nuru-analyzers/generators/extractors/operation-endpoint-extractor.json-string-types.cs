#region Purpose
// Finds the types inside a JSON member that its catalog schema publishes as strings (EndpointExtractor partial).
#endregion

#region Design
// JsonSchemaExporter publishes a node with no type when its type has a custom converter, because it cannot
// see what the converter writes. A type with its own [JsonConverter] (not the string-enum converter) that
// implements IParsable<TSelf> is a typed ID: its converter writes the string form the type parses. The
// generator finds those types here, at compile time, because the runtime library is AOT and trim analyzed
// and cannot inspect interfaces by reflection. The walk follows type arguments, array elements and the
// public instance properties of non-framework types, base types included. Over-including a type is
// harmless: the runtime transform only rewrites a node the exporter left untyped. A type the generated
// code cannot name (private or protected nesting) is skipped. "format":"uuid" is added when the type has a
// public instance Guid Value property, the shape the TimeWarp typed-ID generator emits; Nuru does not
// reference that generator.
#endregion

namespace TimeWarp.Nuru.Generators;

internal static partial class EndpointExtractor
{
  private const string JsonConverterAttributeMetadataName = "System.Text.Json.Serialization.JsonConverterAttribute";
  private const string JsonSerializationNamespace = "System.Text.Json.Serialization";

  /// <summary>
  /// Types reachable from <paramref name="memberType"/> that publish as <c>{"type":"string"}</c>.
  /// </summary>
  private static EquatableArray<JsonStringType> FindJsonStringTypes(ITypeSymbol memberType)
  {
    List<JsonStringType> found = [];
    HashSet<ITypeSymbol> visited = new(SymbolEqualityComparer.Default);
    Stack<ITypeSymbol> pending = new();
    pending.Push(memberType);

    while (pending.Count > 0)
    {
      ITypeSymbol type = pending.Pop();
      if (!visited.Add(type))
        continue;

      if (type is IArrayTypeSymbol array)
      {
        pending.Push(array.ElementType);
        continue;
      }

      if (type is not INamedTypeSymbol named)
        continue;

      if (IsJsonStringType(named))
      {
        if (IsNameableFromGeneratedCode(named))
        {
          string typeName = named.WithNullableAnnotation(NullableAnnotation.NotAnnotated)
            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
          found.Add(new JsonStringType(typeName, HasGuidValue(named)));
        }

        continue;
      }

      foreach (ITypeSymbol argument in named.TypeArguments)
      {
        pending.Push(argument);
      }

      if (IsFrameworkType(named))
        continue;

      for (INamedTypeSymbol? current = named; current is not null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
      {
        foreach (IPropertySymbol property in current.GetMembers().OfType<IPropertySymbol>())
        {
          if (!property.IsStatic && !property.IsIndexer && property.DeclaredAccessibility == Accessibility.Public)
            pending.Push(property.Type);
        }
      }
    }

    return [.. found];
  }

  private static bool IsJsonStringType(INamedTypeSymbol type) =>
    HasCustomJsonConverter(type) && IsSelfParsable(type);

  private static bool HasCustomJsonConverter(INamedTypeSymbol type)
  {
    foreach (AttributeData attribute in type.GetAttributes())
    {
      if (!InheritsFrom(attribute.AttributeClass, JsonConverterAttributeMetadataName))
        continue;

      // The string-enum converter already exports its names.
      return !(attribute.ConstructorArguments is [{ Value: INamedTypeSymbol converter }]
        && converter.Name == "JsonStringEnumConverter"
        && converter.ContainingNamespace?.ToDisplayString() == JsonSerializationNamespace);
    }

    return false;
  }

  private static bool InheritsFrom(INamedTypeSymbol? type, string metadataName)
  {
    for (INamedTypeSymbol? current = type; current is not null; current = current.BaseType)
    {
      if (current.ToDisplayString() == metadataName)
        return true;
    }

    return false;
  }

  private static bool IsSelfParsable(INamedTypeSymbol type) =>
    type.AllInterfaces.Any(candidate =>
      candidate.Name == "IParsable"
      && candidate.TypeArguments.Length == 1
      && candidate.ContainingNamespace?.ToDisplayString() == "System"
      && SymbolEqualityComparer.Default.Equals(candidate.TypeArguments[0], type));

  private static bool HasGuidValue(INamedTypeSymbol type) =>
    type.GetMembers("Value").OfType<IPropertySymbol>().Any(property =>
      !property.IsStatic
      && property.DeclaredAccessibility == Accessibility.Public
      && property.Type.ToDisplayString() == "System.Guid");

  private static bool IsFrameworkType(INamedTypeSymbol type)
  {
    string? ns = type.ContainingNamespace?.ToDisplayString();
    return ns is not null && (ns == "System" || ns.StartsWith("System.", StringComparison.Ordinal) || ns.StartsWith("Microsoft.", StringComparison.Ordinal));
  }

  private static bool IsNameableFromGeneratedCode(INamedTypeSymbol type)
  {
    for (INamedTypeSymbol? current = type; current is not null; current = current.ContainingType)
    {
      if (current.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedOrInternal))
        return false;
    }

    return true;
  }
}
