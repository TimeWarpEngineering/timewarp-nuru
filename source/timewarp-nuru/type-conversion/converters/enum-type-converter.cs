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

  private static readonly bool IsFlagsEnum = typeof(TEnum).GetCustomAttribute<FlagsAttribute>() is not null;

  private static readonly HashSet<string> NameSet = new(Enum.GetNames<TEnum>(), StringComparer.OrdinalIgnoreCase);

  /// <summary>
  /// No alias - use the enum type name directly (e.g., {level:LogLevel}).
  /// </summary>
  public string? ConstraintAlias => null;

  /// <summary>
  /// Converts a string to the enum value.
  /// </summary>
  /// <remarks>
  /// Non-[Flags] enums are accepted only when the parsed value is defined by <see cref="Enum.IsDefined{TEnum}(TEnum)"/>.
  /// Numeric strings that map directly to a defined member (for example, "10" when Normal = 10) are accepted,
  /// but undefined numeric values (for example, "999") are rejected.
  /// <para>
  /// [Flags] enums accept only named members or comma-separated combinations of named members
  /// (for example, "Read,Write"). Raw numeric input (for example, "12" or "1,2") is rejected so that
  /// the conversion behavior is predictable for users.
  /// </para>
  /// </remarks>
  public bool TryConvert(string value, out object? result)
  {
    if (value is null)
    {
      result = null;
      return false;
    }

    if (Enum.TryParse<TEnum>(value, ignoreCase: true, out TEnum enumValue))
    {
      if (IsFlagsEnum)
      {
        if (!IsAllNamedParts(value))
        {
          result = null;
          return false;
        }
      }
      else if (!Enum.IsDefined(enumValue))
      {
        result = null;
        return false;
      }

      result = enumValue;
      return true;
    }

    result = null;
    return false;
  }

  /// <summary>
  /// Verifies that every comma-separated part of the input is a named enum member.
  /// </summary>
  /// <remarks>
  /// C# enum member names are identifiers and cannot be purely numeric, so a defined name is never
  /// mistaken for a numeric value. Whitespace around commas is ignored.
  /// </remarks>
  private static bool IsAllNamedParts(string value)
  {
    string[] parts = value.Split(',');
    foreach (string part in parts)
    {
      string trimmedPart = part.Trim();
      if (string.IsNullOrEmpty(trimmedPart) || !NameSet.Contains(trimmedPart))
      {
        return false;
      }
    }

    return true;
  }

  /// <summary>
  /// Gets a helpful error message showing valid enum values.
  /// </summary>
  public string GetValidValuesMessage()
  {
    return $"Valid values are: {string.Join(", ", Enum.GetNames<TEnum>())}";
  }
}
