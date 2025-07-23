namespace TimeWarp.Nuru.TypeConversion;

/// <summary>
/// Registry for managing type converters used in route parameter conversion.
/// </summary>
public class TypeConverterRegistry : ITypeConverterRegistry
{
    private readonly Dictionary<string, IRouteTypeConverter> _convertersByConstraint = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Type, IRouteTypeConverter> _convertersByType = new();

    public TypeConverterRegistry()
    {
        // Register default converters
        RegisterDefaultConverters();
    }

    /// <summary>
    /// Registers a type converter.
    /// </summary>
    public void RegisterConverter(IRouteTypeConverter converter)
    {
        ArgumentNullException.ThrowIfNull(converter);

        _convertersByConstraint[converter.ConstraintName] = converter;
        _convertersByType[converter.TargetType] = converter;
    }

    /// <summary>
    /// Gets a converter by constraint name (e.g., "int", "bool").
    /// </summary>
    public IRouteTypeConverter? GetConverterByConstraint(string constraintName)
    {
        if (string.IsNullOrEmpty(constraintName))
            return null;

        return _convertersByConstraint.TryGetValue(constraintName, out var converter)
            ? converter
            : null;
    }

    /// <summary>
    /// Gets a converter by target type.
    /// </summary>
    public IRouteTypeConverter? GetConverterByType(Type targetType)
    {
        if (targetType == null)
            return null;

        return _convertersByType.TryGetValue(targetType, out var converter)
            ? converter
            : null;
    }

    /// <summary>
    /// Attempts to convert a string value to the specified type using the constraint.
    /// </summary>
    public bool TryConvert(string value, string constraintName, out object? result)
    {
        var converter = GetConverterByConstraint(constraintName);
        if (converter == null)
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
        var converter = GetConverterByType(targetType);
        if (converter == null)
        {
            result = null;
            return false;
        }

        return converter.TryConvert(value, out result);
    }

    private void RegisterDefaultConverters()
    {
        RegisterConverter(new Converters.IntTypeConverter());
        RegisterConverter(new Converters.BoolTypeConverter());
        RegisterConverter(new Converters.LongTypeConverter());
        RegisterConverter(new Converters.DoubleTypeConverter());
        RegisterConverter(new Converters.DecimalTypeConverter());
        RegisterConverter(new Converters.GuidTypeConverter());
        RegisterConverter(new Converters.DateTimeTypeConverter());
        RegisterConverter(new Converters.TimeSpanTypeConverter());
    }
}