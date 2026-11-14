#region Purpose
// Finds the registered custom type converter for a route type constraint.
#endregion

#region Design
// One rule for every caller: the route matcher, the option conversions, the json-args binder, and the
// unknown-constraint validator (NURU_S010). A constraint matches a converter by, in order of the
// converter list: fully qualified target type, simple target type name, ConstraintAlias, or the
// converter name minus its "Converter" suffix. All comparisons ignore case (task 584).
#endregion

namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Matches a route type constraint to a custom converter registered with <c>AddTypeConverter()</c>.
/// </summary>
internal static class CustomConverterMatcher
{
  private const string GlobalPrefix = "global::";
  private const string ConverterSuffix = "Converter";

  /// <summary>
  /// Returns the first converter the constraint names, or null when none does.
  /// </summary>
  /// <param name="constraint">The constraint without a trailing '?' (e.g. "point", "Point").</param>
  /// <param name="converters">Converters registered on the app.</param>
  public static CustomConverterDefinition? Find(string? constraint, IEnumerable<CustomConverterDefinition>? converters)
  {
    if (string.IsNullOrEmpty(constraint) || converters is null)
      return null;

    // A default ImmutableArray throws on enumeration; it means no converters.
    if (converters is ImmutableArray<CustomConverterDefinition> { IsDefault: true })
      return null;

    return converters.FirstOrDefault(c => Matches(c, constraint!));
  }

  private static bool Matches(CustomConverterDefinition converter, string constraint) =>
    string.Equals(converter.TargetTypeName, constraint, StringComparison.OrdinalIgnoreCase) ||
    string.Equals(SimpleTypeName(converter.TargetTypeName), constraint, StringComparison.OrdinalIgnoreCase) ||
    string.Equals(converter.ConstraintAlias, constraint, StringComparison.OrdinalIgnoreCase) ||
    string.Equals(TargetTypeFromConverterName(converter.ConverterTypeName), constraint, StringComparison.OrdinalIgnoreCase);

  /// <summary>
  /// Strips <c>global::</c> and the namespace: "global::MyApp.Types.EmailAddress" becomes "EmailAddress".
  /// </summary>
  public static string SimpleTypeName(string fullyQualifiedName)
  {
    if (fullyQualifiedName.StartsWith(GlobalPrefix, StringComparison.Ordinal))
      fullyQualifiedName = fullyQualifiedName[GlobalPrefix.Length..];

    int lastDot = fullyQualifiedName.LastIndexOf('.', StringComparison.Ordinal);
    return lastDot >= 0 ? fullyQualifiedName[(lastDot + 1)..] : fullyQualifiedName;
  }

  /// <summary>
  /// The convention target name: "EmailAddressConverter" becomes "EmailAddress".
  /// </summary>
  public static string TargetTypeFromConverterName(string converterTypeName)
  {
    string simpleName = SimpleTypeName(converterTypeName);
    if (simpleName.EndsWith(ConverterSuffix, StringComparison.Ordinal) && simpleName.Length > ConverterSuffix.Length)
      return simpleName[..^ConverterSuffix.Length];

    return simpleName;
  }
}
