namespace TimeWarp.Nuru.TypeConversion;

/// <summary>
/// Registry for managing type converters used in route parameter conversion.
/// </summary>
public class TypeConverterRegistry : ITypeConverterRegistry
{
  // Instance-specific custom converters (usually empty)
  private readonly Dictionary<string, IRouteTypeConverter> ConvertersByConstraint = new(StringComparer.OrdinalIgnoreCase);
  private readonly Dictionary<Type, IRouteTypeConverter> ConvertersByType = [];

  public TypeConverterRegistry()
  {
    // No initialization needed - defaults are handled by static methods
  }

  /// <summary>
  /// Registers a type converter.
  /// </summary>
  public void RegisterConverter(IRouteTypeConverter converter)
  {
    ArgumentNullException.ThrowIfNull(converter);

    ConvertersByConstraint[converter.ConstraintName] = converter;
    ConvertersByType[converter.TargetType] = converter;
  }

  /// <summary>
  /// Gets a converter by constraint name (e.g., "int", "bool").
  /// </summary>
  public IRouteTypeConverter? GetConverterByConstraint(string constraintName)
  {
    ArgumentNullException.ThrowIfNull(constraintName);

    if (constraintName.Length == 0)
      return null;

    // Check custom converters
    return ConvertersByConstraint.TryGetValue(constraintName, out IRouteTypeConverter? converter) ? converter : null;
  }

  /// <summary>
  /// Gets a converter by target type.
  /// </summary>
  public IRouteTypeConverter? GetConverterByType(Type targetType)
  {
    if (targetType is null)
      return null;

    // Check custom converters
    return ConvertersByType.TryGetValue(targetType, out IRouteTypeConverter? converter) ? converter : null;
  }

  /// <summary>
  /// Attempts to convert a string value to the specified type using the constraint.
  /// </summary>
  public bool TryConvert(string value, string constraintName, out object? result)
  {
    ArgumentNullException.ThrowIfNull(value);
    ArgumentNullException.ThrowIfNull(constraintName);

    // Try default constraint conversions first
    Type? targetType = DefaultTypeConverters.GetTypeForConstraint(constraintName);
    if (targetType is not null && DefaultTypeConverters.TryConvert(value, targetType, out result))
      return true;

    // Fall back to custom converters
    IRouteTypeConverter? converter = GetConverterByConstraint(constraintName);
    if (converter is null)
    {
      result = null;
      return false;
    }

    return converter.TryConvert(value, out result);
  }

  /// <summary>
  /// Attempts to convert a string value to the specified type.
  /// </summary>
  public bool TryConvert(string value, Type targetType, out object? result)
  {
    ArgumentNullException.ThrowIfNull(value);
    ArgumentNullException.ThrowIfNull(targetType);

    // Try default conversions first (no allocations)
    if (DefaultTypeConverters.TryConvert(value, targetType, out result))
      return true;

    // Fall back to custom converters
    IRouteTypeConverter? converter = GetConverterByType(targetType);
    if (converter is null)
    {
      result = null;
      return false;
    }

    return converter.TryConvert(value, out result);
  }

}
