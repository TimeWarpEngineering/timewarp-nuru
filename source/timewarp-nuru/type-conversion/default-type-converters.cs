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
      if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
      {
        result = intValue;
        return true;
      }
    }
    else if (targetType == typeof(byte))
    {
      if (byte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte byteValue))
      {
        result = byteValue;
        return true;
      }
    }
    else if (targetType == typeof(sbyte))
    {
      if (sbyte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out sbyte sbyteValue))
      {
        result = sbyteValue;
        return true;
      }
    }
    else if (targetType == typeof(short))
    {
      if (short.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out short shortValue))
      {
        result = shortValue;
        return true;
      }
    }
    else if (targetType == typeof(ushort))
    {
      if (ushort.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort ushortValue))
      {
        result = ushortValue;
        return true;
      }
    }
    else if (targetType == typeof(uint))
    {
      if (uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint uintValue))
      {
        result = uintValue;
        return true;
      }
    }
    else if (targetType == typeof(ulong))
    {
      if (ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong ulongValue))
      {
        result = ulongValue;
        return true;
      }
    }
    else if (targetType == typeof(float))
    {
      if (float.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float floatValue))
      {
        result = floatValue;
        return true;
      }
    }
    else if (targetType == typeof(char))
    {
      if (value.Length == 1)
      {
        result = value[0];
        return true;
      }
    }
    else if (targetType == typeof(bool))
    {
      if (BooleanConverter.TryParse(value, out bool boolValue))
      {
        result = boolValue;
        return true;
      }
    }
    else if (targetType == typeof(long))
    {
      if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long longValue))
      {
        result = longValue;
        return true;
      }
    }
    else if (targetType == typeof(double))
    {
      if (double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double doubleValue))
      {
        result = doubleValue;
        return true;
      }
    }
    else if (targetType == typeof(decimal))
    {
      if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal decimalValue))
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
      if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateTimeValue))
      {
        result = dateTimeValue;
        return true;
      }
    }
    else if (targetType == typeof(TimeSpan))
    {
      if (TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out TimeSpan timeSpanValue))
      {
        result = timeSpanValue;
        return true;
      }
    }
    else if (targetType == typeof(Uri))
    {
      if (Uri.TryCreate(value, UriKind.RelativeOrAbsolute, out Uri? uriValue))
      {
        result = uriValue;
        return true;
      }
    }
    else if (targetType == typeof(FileInfo))
    {
      try
      {
        result = new FileInfo(value);
        return true;
      }
      catch (ArgumentException)
      {
        // Invalid path
        return false;
      }
    }
    else if (targetType == typeof(DirectoryInfo))
    {
      try
      {
        result = new DirectoryInfo(value);
        return true;
      }
      catch (ArgumentException)
      {
        // Invalid path
        return false;
      }
    }
    else if (targetType == typeof(IPAddress))
    {
      if (IPAddress.TryParse(value, out IPAddress? ipValue))
      {
        result = ipValue;
        return true;
      }
    }
    else if (targetType == typeof(DateOnly))
    {
      if (DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly dateOnlyValue))
      {
        result = dateOnlyValue;
        return true;
      }
    }
    else if (targetType == typeof(TimeOnly))
    {
      if (TimeOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly timeOnlyValue))
      {
        result = timeOnlyValue;
        return true;
      }
    }
    else if (targetType.IsEnum)
    {
      try
      {
        object? enumValue = Enum.Parse(targetType, value, ignoreCase: true);
        bool isFlagsEnum = targetType.GetCustomAttribute<FlagsAttribute>() is not null;

        if (isFlagsEnum)
        {
          if (IsAllNamedEnumParts(value, targetType))
          {
            result = enumValue;
            return true;
          }
        }
        else if (Enum.IsDefined(targetType, enumValue))
        {
          result = enumValue;
          return true;
        }
      }
      catch (ArgumentException)
      {
        // Value is not a valid enum member
      }

      return false;
    }

    return false;
  }

  /// <summary>
  /// Verifies that every comma-separated part of the input is a named enum member.
  /// Used for [Flags] enums to reject raw numeric input.
  /// </summary>
  private static bool IsAllNamedEnumParts(string value, Type enumType)
  {
    HashSet<string> nameSet = new(Enum.GetNames(enumType), StringComparer.OrdinalIgnoreCase);
    string[] parts = value.Split(',');
    foreach (string part in parts)
    {
      string trimmedPart = part.Trim();
      if (string.IsNullOrEmpty(trimmedPart) || !nameSet.Contains(trimmedPart))
      {
        return false;
      }
    }

    return true;
  }

  /// <summary>
  /// Gets the Type associated with a constraint name (case-insensitive).
  /// </summary>
  /// <remarks>
  /// Supports:
  /// - C# primitive keywords: int, bool, long, double, decimal, float, byte, sbyte, short, ushort, uint, ulong, char
  /// - CLR type names: Int32, Boolean, Int64, Double, Decimal, Single, Byte, SByte, Int16, UInt16, UInt32, UInt64, Char
  /// - PascalCase type names: DateTime, TimeSpan, Guid, Uri, FileInfo, DirectoryInfo, IPAddress, DateOnly, TimeOnly
  /// </remarks>
  public static Type? GetTypeForConstraint(string constraintName)
  {
    return constraintName.ToLowerInvariant() switch
    {
      // C# primitive keywords (primary names)
      "int" or "int32" => typeof(int),
      "byte" => typeof(byte),
      "sbyte" => typeof(sbyte),
      "short" or "int16" => typeof(short),
      "ushort" or "uint16" => typeof(ushort),
      "uint" or "uint32" => typeof(uint),
      "ulong" or "uint64" => typeof(ulong),
      "float" or "single" => typeof(float),
      "char" => typeof(char),
      "bool" or "boolean" => typeof(bool),
      "long" or "int64" => typeof(long),
      "double" => typeof(double),
      "decimal" => typeof(decimal),

      // PascalCase type names (case-insensitive matching)
      "guid" => typeof(Guid),
      "datetime" => typeof(DateTime),
      "timespan" => typeof(TimeSpan),
      "uri" => typeof(Uri),
      "fileinfo" => typeof(FileInfo),
      "directoryinfo" => typeof(DirectoryInfo),
      "ipaddress" => typeof(IPAddress),
      "dateonly" => typeof(DateOnly),
      "timeonly" => typeof(TimeOnly),

      _ => null
    };
  }
}
