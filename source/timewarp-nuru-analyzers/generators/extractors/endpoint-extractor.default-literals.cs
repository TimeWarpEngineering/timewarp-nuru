#region Purpose
// EndpointExtractor partial: parameter defaults and enum/integral/real literal formatting for defaults.
#endregion

namespace TimeWarp.Nuru.Generators;

internal static partial class EndpointExtractor
{
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
}
