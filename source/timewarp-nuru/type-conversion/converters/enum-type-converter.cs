namespace TimeWarp.Nuru;

/// <summary>
/// Generic type converter for enum types.
/// Provides case-insensitive parsing with helpful error messages.
/// </summary>
/// <typeparam name="TEnum">The enum type to convert to.</typeparam>
/// <remarks>
/// The primary constraint name is the enum type name (e.g., "LogLevel").
/// No alias is provided by default.
/// </remarks>
public class EnumTypeConverter<TEnum> : IRouteTypeConverter where TEnum : struct, Enum
{
  public Type TargetType => typeof(TEnum);

  /// <summary>
  /// No alias - use the enum type name directly (e.g., {level:LogLevel}).
  /// </summary>
  public string? ConstraintAlias => null;

  public bool TryConvert(string value, out object? result)
  {
    if (Enum.TryParse<TEnum>(value, ignoreCase: true, out TEnum enumValue))
    {
      result = enumValue;
      return true;
    }

    result = null;
    return false;
  }

  /// <summary>
  /// Gets a helpful error message showing valid enum values.
  /// </summary>
  public string GetValidValuesMessage()
  {
    return $"Valid values are: {string.Join(", ", Enum.GetNames<TEnum>())}";
  }
}
