#region Purpose
// ServiceExtractor partial: constructor selection, dependencies and parameter default value formatting.
#endregion

namespace TimeWarp.Nuru.Generators;

using System.Globalization;

internal static partial class ServiceExtractor
{
  /// <summary>
  /// Extracts constructor dependencies from an implementation type.
  /// Supports multiple constructors - chooses the one with most parameters.
  /// </summary>
  internal static ImmutableArray<string> GetConstructorDependencyTypes(INamedTypeSymbol implementationType)
    => ExtractConstructorDependencies(implementationType);

  /// <summary>
  /// Extracts constructor dependencies from an implementation type.
  /// Supports multiple constructors - chooses the one with most parameters.
  /// </summary>
  private static ImmutableArray<string> ExtractConstructorDependencies(INamedTypeSymbol implementationType)
  {
    // Find the best constructor (most parameters)
    IMethodSymbol? constructor = FindBestConstructor(implementationType);

    if (constructor is null || constructor.Parameters.Length == 0)
      return [];

    return [.. constructor.Parameters
      .Select(p => p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))];
  }

  /// <summary>
  /// Extracts detailed constructor parameters from an implementation type.
  /// Supports multiple constructors, optional parameters, and built-in type detection.
  /// </summary>
  internal static ImmutableArray<ConstructorParameter> GetConstructorParameters(INamedTypeSymbol implementationType)
    => ExtractConstructorParameters(implementationType);

  /// <summary>
  /// Extracts detailed constructor parameters from an implementation type.
  /// Supports multiple constructors, optional parameters, and built-in type detection.
  /// </summary>
  private static ImmutableArray<ConstructorParameter> ExtractConstructorParameters(INamedTypeSymbol implementationType)
  {
    // Find the best constructor (most parameters)
    IMethodSymbol? constructor = FindBestConstructor(implementationType);

    if (constructor is null || constructor.Parameters.Length == 0)
      return [];

    ImmutableArray<ConstructorParameter>.Builder parameters = ImmutableArray.CreateBuilder<ConstructorParameter>();

    foreach (IParameterSymbol param in constructor.Parameters)
    {
      string typeName = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
      bool hasDefaultValue = param.HasExplicitDefaultValue;
      string? defaultValue = null;

      if (hasDefaultValue)
      {
        defaultValue = GetDefaultValueExpression(param);
      }

      parameters.Add(new ConstructorParameter(
        ParameterName: param.Name,
        TypeName: typeName,
        HasDefaultValue: hasDefaultValue,
        DefaultValue: defaultValue));
    }

    return parameters.ToImmutable();
  }

  /// <summary>
  /// Finds the best constructor for DI resolution.
  /// MS DI behavior: choose the constructor with the most parameters.
  /// </summary>
  private static IMethodSymbol? FindBestConstructor(INamedTypeSymbol implementationType)
  {
    // Get all public non-static constructors
    // Choose the constructor with the most parameters
    // This matches MS DI behavior for constructor selection
    return implementationType.InstanceConstructors
      .Where(c => c.DeclaredAccessibility == Accessibility.Public && !c.IsImplicitlyDeclared)
      .OrderByDescending(c => c.Parameters.Length)
      .FirstOrDefault();
  }

  /// <summary>
  /// Gets the default value expression for a parameter.
  /// </summary>
  private static string? GetDefaultValueExpression(IParameterSymbol param)
  {
    if (!param.HasExplicitDefaultValue)
      return null;

    object? defaultValue = param.ExplicitDefaultValue;
    if (defaultValue is null)
      return "null";

    ITypeSymbol valueType = UnwrapNullable(param.Type);
    if (valueType.TypeKind == TypeKind.Enum && valueType is INamedTypeSymbol enumType)
      return FormatEnumDefault(enumType, defaultValue);

    return FormatPrimitiveDefault(defaultValue);
  }

  private static ITypeSymbol UnwrapNullable(ITypeSymbol type)
  {
    if (type is INamedTypeSymbol named
        && named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
        && named.TypeArguments.Length == 1)
    {
      return named.TypeArguments[0];
    }

    return type;
  }

  /// <summary>
  /// Emits <c>global::Namespace.Enum.Member</c>, or a cast of the underlying
  /// literal when the constant is not a single named member.
  /// </summary>
  private static string FormatEnumDefault(INamedTypeSymbol enumType, object defaultValue)
  {
    foreach (IFieldSymbol field in enumType.GetMembers().OfType<IFieldSymbol>())
    {
      if (field.HasConstantValue
          && field.ConstantValue is not null
          && EnumConstantEquals(field.ConstantValue, defaultValue))
      {
        return field.ToDisplayString(FullyQualifiedMemberFormat);
      }
    }

    string typeName = enumType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    string literal = FormatPrimitiveDefault(defaultValue);
    return $"({typeName}){literal}";
  }

  /// <summary>
  /// Roslyn boxes an enum default as its underlying integral value. Field
  /// constants use that same box. Compare bits so a boxed enum still matches.
  /// </summary>
  private static bool EnumConstantEquals(object fieldValue, object defaultValue)
  {
    if (fieldValue.Equals(defaultValue))
      return true;

    return TryIntegralBits(fieldValue, out ulong fieldBits)
      && TryIntegralBits(defaultValue, out ulong defaultBits)
      && fieldBits == defaultBits;
  }

  private static bool TryIntegralBits(object value, out ulong bits)
  {
    if (value is Enum enumValue)
    {
      value = Convert.ChangeType(
        enumValue,
        Enum.GetUnderlyingType(enumValue.GetType()),
        CultureInfo.InvariantCulture);
    }

    switch (value)
    {
      case byte v:
        bits = v;
        return true;
      case sbyte v:
        bits = unchecked((ulong)v);
        return true;
      case short v:
        bits = unchecked((ulong)v);
        return true;
      case ushort v:
        bits = v;
        return true;
      case int v:
        bits = unchecked((ulong)v);
        return true;
      case uint v:
        bits = v;
        return true;
      case long v:
        bits = unchecked((ulong)v);
        return true;
      case ulong v:
        bits = v;
        return true;
      default:
        bits = 0;
        return false;
    }
  }

  /// <summary>
  /// String and char use FormatLiteral. Other primitives use FormatPrimitive.
  /// FormatPrimitive does not emit F/M/U/L/UL suffixes or non-finite names,
  /// which are required for the expression to compile as that parameter type.
  /// </summary>
  private static string FormatPrimitiveDefault(object defaultValue)
  {
    switch (defaultValue)
    {
      case string s:
        return SymbolDisplay.FormatLiteral(s, quote: true);
      case char c:
        return SymbolDisplay.FormatLiteral(c, quote: true);
      case float f when float.IsNaN(f):
        return "float.NaN";
      case float f when float.IsPositiveInfinity(f):
        return "float.PositiveInfinity";
      case float f when float.IsNegativeInfinity(f):
        return "float.NegativeInfinity";
      case double d when double.IsNaN(d):
        return "double.NaN";
      case double d when double.IsPositiveInfinity(d):
        return "double.PositiveInfinity";
      case double d when double.IsNegativeInfinity(d):
        return "double.NegativeInfinity";
    }

    string? literal = SymbolDisplay.FormatPrimitive(defaultValue, quoteStrings: true, useHexadecimalNumbers: false);
    if (literal is null)
      return "default";

    return defaultValue switch
    {
      float => literal + "F",
      decimal => literal + "M",
      uint => literal + "U",
      long => literal + "L",
      ulong => literal + "UL",
      _ => literal
    };
  }
}
