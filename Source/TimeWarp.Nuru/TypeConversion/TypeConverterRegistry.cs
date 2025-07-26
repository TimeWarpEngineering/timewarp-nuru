namespace TimeWarp.Nuru.TypeConversion;

/// <summary>
/// Registry for managing type converters used in route parameter conversion.
/// </summary>
public class TypeConverterRegistry : ITypeConverterRegistry
{
  // Static shared default converters - initialized once for all instances
  private static readonly Dictionary<string, IRouteTypeConverter> DefaultConvertersByConstraint = new(StringComparer.OrdinalIgnoreCase);
  private static readonly Dictionary<Type, IRouteTypeConverter> DefaultConvertersByType = [];
  
  // Instance-specific custom converters (usually empty)
  private readonly Dictionary<string, IRouteTypeConverter> ConvertersByConstraint = new(StringComparer.OrdinalIgnoreCase);
  private readonly Dictionary<Type, IRouteTypeConverter> ConvertersByType = [];

  static TypeConverterRegistry()
  {
    // Register default converters once for all instances
    RegisterDefaultConverters();
  }

  public TypeConverterRegistry()
  {
    // No need to register defaults - they're in static dictionaries
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

    // Check instance converters first, then defaults
    return ConvertersByConstraint.TryGetValue(constraintName, out IRouteTypeConverter? converter) ? converter
         : DefaultConvertersByConstraint.TryGetValue(constraintName, out converter) ? converter
         : null;
  }

  /// <summary>
  /// Gets a converter by target type.
  /// </summary>
  public IRouteTypeConverter? GetConverterByType(Type targetType)
  {
    if (targetType is null)
      return null;

    // Check instance converters first, then defaults
    return ConvertersByType.TryGetValue(targetType, out IRouteTypeConverter? converter) ? converter
         : DefaultConvertersByType.TryGetValue(targetType, out converter) ? converter
         : null;
  }

  /// <summary>
  /// Attempts to convert a string value to the specified type using the constraint.
  /// </summary>
  public bool TryConvert(string value, string constraintName, out object? result)
  {
    ArgumentNullException.ThrowIfNull(value);
    ArgumentNullException.ThrowIfNull(constraintName);

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

    IRouteTypeConverter? converter = GetConverterByType(targetType);
    if (converter is null)
    {
      result = null;
      return false;
    }

    return converter.TryConvert(value, out result);
  }

  private static void RegisterDefaultConverters()
  {
    RegisterDefaultConverter(new Converters.IntTypeConverter());
    RegisterDefaultConverter(new Converters.BoolTypeConverter());
    RegisterDefaultConverter(new Converters.LongTypeConverter());
    RegisterDefaultConverter(new Converters.DoubleTypeConverter());
    RegisterDefaultConverter(new Converters.DecimalTypeConverter());
    RegisterDefaultConverter(new Converters.GuidTypeConverter());
    RegisterDefaultConverter(new Converters.DateTimeTypeConverter());
    RegisterDefaultConverter(new Converters.TimeSpanTypeConverter());
  }
  
  private static void RegisterDefaultConverter(IRouteTypeConverter converter)
  {
    DefaultConvertersByConstraint[converter.ConstraintName] = converter;
    DefaultConvertersByType[converter.TargetType] = converter;
  }
}
