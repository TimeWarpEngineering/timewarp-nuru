#region Purpose
// Constructor selection, property classification and attribute helpers for [Operation] endpoints (EndpointExtractor partial).
#endregion

namespace TimeWarp.Nuru.Generators;

internal static partial class EndpointExtractor
{
  private static bool HasParameterlessConstructor(INamedTypeSymbol type) =>
    type.InstanceConstructors.Any(constructor =>
      constructor.Parameters.Length == 0
      && constructor.DeclaredAccessibility == Accessibility.Public);

  private static bool IsDerivableProperty(IPropertySymbol property)
  {
    if (property.IsStatic || property.IsIndexer)
      return false;

    if (property.DeclaredAccessibility != Accessibility.Public)
      return false;

    return property.SetMethod is { DeclaredAccessibility: Accessibility.Public };
  }

  private static string JsonCapabilityType(ITypeSymbol type) =>
    "json " + type.ToDisplayString(OmittedGlobalFormat);

  private static bool TrySelectConstructorArguments(
    INamedTypeSymbol type,
    Compilation compilation,
    Location location,
    bool deriveUnmarked,
    out EquatableArray<string> arguments,
    out Diagnostic? diagnostic)
  {
    arguments = default;
    diagnostic = null;
    string? kindReason = type switch
    {
      { IsStatic: true } => "it is static. An endpoint must be a concrete class or struct.",
      { IsAbstract: true } => "it is abstract. Put the attribute on a concrete type.",
      { TypeKind: not (TypeKind.Class or TypeKind.Struct) } => "it is not a class or struct.",
      _ => null
    };
    if (kindReason is not null)
    {
      diagnostic = Unconstructable(type, location, kindReason);
      return false;
    }

    if (HasCallableParameterlessConstructor(compilation, type))
      return true;

    IMethodSymbol? best = null;
    List<string>? bestNames = null;
    foreach (IMethodSymbol constructor in type.InstanceConstructors)
    {
      if (!IsAccessibleConstructor(compilation, constructor))
        continue;

      if (IsCopyConstructor(type, constructor))
        continue;

      if (!ConstructorParametersMatchProperties(type, constructor, deriveUnmarked, out List<string> names))
        continue;

      if (best is null || constructor.Parameters.Length > best.Parameters.Length)
      {
        best = constructor;
        bestNames = names;
      }
    }

    if (bestNames is null)
    {
      diagnostic = Unconstructable(
        type,
        location,
        "it has no accessible parameterless constructor and no constructor whose parameters all match, ignoring case, public settable properties bound as route parameters or options.");
      return false;
    }

    arguments = [.. bestNames];
    return true;
  }

  private static Diagnostic Unconstructable(INamedTypeSymbol type, Location location, string reason) =>
    Diagnostic.Create(DiagnosticDescriptors.UnconstructableEndpoint, location, type.Name, reason);

  private static bool IsAccessibleConstructor(Compilation compilation, IMethodSymbol constructor) =>
    constructor.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedOrInternal
    && IsAccessibleFrom(compilation, constructor);

  // new() compiles when every parameter is optional or params, so that counts as parameterless.
  private static bool HasCallableParameterlessConstructor(Compilation compilation, INamedTypeSymbol type) =>
    CallableParameterlessConstructor(compilation, type) is not null;

  /// <summary>
  /// The constructor new() binds to: a zero-parameter constructor first, then one whose
  /// parameters are all optional or params. Null when new() does not compile.
  /// </summary>
  private static IMethodSymbol? CallableParameterlessConstructor(Compilation compilation, INamedTypeSymbol type)
  {
    IMethodSymbol? optional = null;
    foreach (IMethodSymbol constructor in type.InstanceConstructors)
    {
      if (!IsAccessibleConstructor(compilation, constructor))
        continue;

      if (constructor.Parameters.Length == 0)
        return constructor;

      if (constructor.Parameters.All(static parameter => parameter.IsOptional || parameter.IsParams))
        optional ??= constructor;
    }

    return optional;
  }

  private static bool IsCopyConstructor(INamedTypeSymbol type, IMethodSymbol constructor) =>
    constructor.Parameters.Length == 1
    && SymbolEqualityComparer.Default.Equals(constructor.Parameters[0].Type, type);

  private static bool ConstructorParametersMatchProperties(
    INamedTypeSymbol type,
    IMethodSymbol constructor,
    bool deriveUnmarked,
    out List<string> propertyNames)
  {
    propertyNames = [];
    if (constructor.Parameters.Length == 0)
      return false;

    foreach (IParameterSymbol parameter in constructor.Parameters)
    {
      IPropertySymbol? match = FindPropertyByName(type, parameter.Name);
      if (match is null || !IsConstructorBoundProperty(type, match, deriveUnmarked))
        return false;

      propertyNames.Add(match.Name);
    }

    return true;
  }

  /// <summary>
  /// True when the property gets a route binding, so a constructor argument has a value.
  /// A [NuruRoute] type binds only marked properties. An [Operation] type also derives
  /// unmarked ones, except from a [NuruRouteGroup] base.
  /// </summary>
  private static bool IsConstructorBoundProperty(INamedTypeSymbol type, IPropertySymbol property, bool deriveUnmarked)
  {
    if (!IsDerivableProperty(property))
      return false;

    if (HasCliMemberAttribute(property))
      return true;

    if (!deriveUnmarked)
      return false;

    return SymbolEqualityComparer.Default.Equals(property.ContainingType, type)
      || !HasNuruRouteGroup(property.ContainingType);
  }

  private static IPropertySymbol? FindPropertyByName(INamedTypeSymbol type, string name)
  {
    for (INamedTypeSymbol? current = type;
         current is not null && current.SpecialType != SpecialType.System_Object;
         current = current.BaseType)
    {
      foreach (IPropertySymbol property in current.GetMembers().OfType<IPropertySymbol>())
      {
        if (property.IsStatic)
          continue;

        if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
          return property;
      }
    }

    return null;
  }

  private static bool HasCliMemberAttribute(IPropertySymbol property)
  {
    foreach (AttributeData attribute in property.GetAttributes())
    {
      if (IsMetadataAttribute(attribute, ParameterAttributeMetadataName)
          || IsMetadataAttribute(attribute, OptionAttributeMetadataName)
          || IsMetadataAttribute(attribute, GroupOptionAttributeMetadataName))
      {
        return true;
      }
    }

    return false;
  }

  private static string? DescriptionFromAttribute(IPropertySymbol property)
  {
    foreach (AttributeData attribute in property.GetAttributes())
    {
      if (!IsMetadataAttribute(attribute, DescriptionAttributeMetadataName))
        continue;

      if (attribute.ConstructorArguments.Length > 0)
        return attribute.ConstructorArguments[0].Value as string;
    }

    return null;
  }

  private static bool IsJsonMember(ITypeSymbol type)
  {
    if (IsScalar(type))
      return false;

    if (IsRepeatedOptionType(type))
    {
      ITypeSymbol? element = ElementType(type);
      return element is null || !IsScalar(element);
    }

    return true;
  }

  /// <summary>
  /// Type constraint of a repeated scalar element (int for int[], null for string[]).
  /// The repeated emitter reads this the same way a fluent <c>{id:int}*</c> option does.
  /// </summary>
  private static string? RepeatedElementConstraint(ITypeSymbol type)
  {
    ITypeSymbol? element = ElementType(type);
    if (element is null)
      return null;

    return GetTypeConstraintFromClrType(element.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
  }

  private static bool IsScalar(ITypeSymbol type)
  {
    if (type is INamedTypeSymbol named && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
      return named.TypeArguments.Length == 1 && IsScalar(named.TypeArguments[0]);

    if (type.TypeKind == TypeKind.Enum)
      return true;

    if (type.SpecialType is SpecialType.System_Boolean
        or SpecialType.System_Char
        or SpecialType.System_String
        or SpecialType.System_Byte
        or SpecialType.System_SByte
        or SpecialType.System_Int16
        or SpecialType.System_UInt16
        or SpecialType.System_Int32
        or SpecialType.System_UInt32
        or SpecialType.System_Int64
        or SpecialType.System_UInt64
        or SpecialType.System_Single
        or SpecialType.System_Double
        or SpecialType.System_Decimal
        or SpecialType.System_DateTime)
    {
      return true;
    }

    string display = type.ToDisplayString();
    return display is "System.DateTimeOffset"
      or "System.DateOnly"
      or "System.TimeOnly"
      or "System.TimeSpan"
      or "System.Guid"
      or "System.Uri"
      or "System.Version"
      or "System.Numerics.BigInteger";
  }

  private static ITypeSymbol? ElementType(ITypeSymbol type)
  {
    if (type is IArrayTypeSymbol array)
      return array.ElementType;

    if (type is INamedTypeSymbol named && named.TypeArguments.Length == 1)
      return named.TypeArguments[0];

    return null;
  }

  private static bool IsNullableOf(ITypeSymbol type, SpecialType specialType) =>
    type is INamedTypeSymbol named
    && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
    && named.TypeArguments.Length == 1
    && named.TypeArguments[0].SpecialType == specialType;

  private static string? NamedString(AttributeData attribute, string name)
  {
    foreach (KeyValuePair<string, TypedConstant> named in attribute.NamedArguments)
    {
      if (named.Key == name)
        return named.Value.Value as string;
    }

    return null;
  }

  /// <summary>
  /// The attribute's location, or <paramref name="fallback"/> when its syntax is not in
  /// <paramref name="compilation"/>. A metadata reference has no syntax, and an IDE project
  /// reference has syntax from another compilation, which a reported diagnostic cannot use.
  /// </summary>
  private static Location AttributeLocation(AttributeData attribute, Compilation compilation, Location fallback)
  {
    SyntaxReference? reference = attribute.ApplicationSyntaxReference;
    if (reference is null || !compilation.ContainsSyntaxTree(reference.SyntaxTree))
      return fallback;

    return reference.GetSyntax().GetLocation();
  }
}
