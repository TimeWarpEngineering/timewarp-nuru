namespace TimeWarp.Nuru;

/// <summary>
/// Defines a registry for managing type converters.
/// </summary>
public interface ITypeConverterRegistry
{
  /// <summary>
  /// Registers a type converter.
  /// </summary>
  void RegisterConverter(IRouteTypeConverter converter);
  /// <summary>
  /// Gets a converter by constraint name (e.g., "int", "bool").
  /// </summary>
  IRouteTypeConverter? GetConverterByConstraint(string constraintName);
  /// <summary>
  /// Gets a converter by target type.
  /// </summary>
  IRouteTypeConverter? GetConverterByType(Type targetType);
  /// <summary>
  /// Attempts to convert a string value to the specified type using the constraint.
  /// </summary>
  bool TryConvert(string value, string constraintName, out object? result);
  /// <summary>
  /// Attempts to convert a string value to the specified type.
  /// </summary>
  bool TryConvert(string value, Type targetType, out object? result);
}
