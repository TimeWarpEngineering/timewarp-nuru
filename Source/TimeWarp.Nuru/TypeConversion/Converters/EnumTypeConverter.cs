namespace TimeWarp.Nuru.TypeConversion.Converters;

/// <summary>
/// Generic type converter for enum types.
/// Provides case-insensitive parsing with helpful error messages.
/// </summary>
/// <typeparam name="TEnum">The enum type to convert to.</typeparam>
public class EnumTypeConverter<TEnum> : IRouteTypeConverter where TEnum : struct, Enum
{
  public Type TargetType => typeof(TEnum);
  /// <summary>
  /// Gets the constraint name based on the enum type name.
  /// For example, MyEnum becomes "myenum".
  /// </summary>
  public string ConstraintName => typeof(TEnum).Name.ToLowerInvariant();

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