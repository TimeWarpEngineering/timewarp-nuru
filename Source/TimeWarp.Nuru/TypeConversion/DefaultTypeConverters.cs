namespace TimeWarp.Nuru;

/// <summary>
/// Provides zero-allocation type conversions for built-in types.
/// This approach eliminates object allocations compared to using IRouteTypeConverter instances.
/// The if/else chain is intentional - it's faster than dictionary lookup and allocates nothing.
/// </summary>
internal static class DefaultTypeConverters
{
  /// <summary>
  /// Attempts to convert a string value to the specified type.
  /// </summary>
  [UnconditionalSuppressMessage("Trimming", "IL2070:UnrecognizedReflectionPattern",
      Justification = "Enum parsing is safe - enum values are preserved when enum type is used")]
  public static bool TryConvert(string value, Type targetType, out object? result)
  {
    result = null;

    if (targetType == typeof(int))
    {
      if (int.TryParse(value, out int intValue))
      {
        result = intValue;
        return true;
      }
    }
    else if (targetType == typeof(bool))
    {
      if (bool.TryParse(value, out bool boolValue))
      {
        result = boolValue;
        return true;
      }
    }
    else if (targetType == typeof(long))
    {
      if (long.TryParse(value, out long longValue))
      {
        result = longValue;
        return true;
      }
    }
    else if (targetType == typeof(double))
    {
      if (double.TryParse(value, out double doubleValue))
      {
        result = doubleValue;
        return true;
      }
    }
    else if (targetType == typeof(decimal))
    {
      if (decimal.TryParse(value, out decimal decimalValue))
      {
        result = decimalValue;
        return true;
      }
    }
    else if (targetType == typeof(Guid))
    {
      if (Guid.TryParse(value, out Guid guidValue))
      {
        result = guidValue;
        return true;
      }
    }
    else if (targetType == typeof(DateTime))
    {
      if (DateTime.TryParse(value, out DateTime dateTimeValue))
      {
        result = dateTimeValue;
        return true;
      }
    }
    else if (targetType == typeof(TimeSpan))
    {
      if (TimeSpan.TryParse(value, out TimeSpan timeSpanValue))
      {
        result = timeSpanValue;
        return true;
      }
    }
    else if (targetType.IsEnum)
    {
      try
      {
        result = Enum.Parse(targetType, value, ignoreCase: true);
        return true;
      }
      catch (ArgumentException)
      {
        // Value is not a valid enum member
        return false;
      }
    }

    return false;
  }

  /// <summary>
  /// Gets the Type associated with a constraint name.
  /// </summary>
  public static Type? GetTypeForConstraint(string constraintName)
  {
    return constraintName switch
    {
      "int" => typeof(int),
      "bool" => typeof(bool),
      "long" => typeof(long),
      "double" => typeof(double),
      "decimal" => typeof(decimal),
      "guid" => typeof(Guid),
      "datetime" => typeof(DateTime),
      "timespan" => typeof(TimeSpan),
      _ => null
    };
  }
}
