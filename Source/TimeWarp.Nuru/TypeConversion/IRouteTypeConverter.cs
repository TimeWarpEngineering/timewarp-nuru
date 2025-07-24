namespace TimeWarp.Nuru.TypeConversion;

/// <summary>
/// Interface for converting string values from route parameters to strongly typed values.
/// </summary>
public interface IRouteTypeConverter
{
  /// <summary>
  /// Gets the target type this converter produces.
  /// </summary>
  Type TargetType { get; }

  /// <summary>
  /// Gets the constraint name used in route patterns (e.g., "int", "bool", "guid").
  /// </summary>
  string ConstraintName { get; }

  /// <summary>
  /// Attempts to convert a string value to the target type.
  /// </summary>
  /// <param name="value">The string value to convert.</param>
  /// <param name="result">The converted value if successful, null otherwise.</param>
  /// <returns>True if conversion succeeded, false otherwise.</returns>
  bool TryConvert(string value, out object? result);
}
