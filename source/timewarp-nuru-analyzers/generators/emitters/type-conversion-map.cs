namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Provides compile-time type conversion mappings for built-in types.
/// This mirrors the runtime DefaultTypeConverters.cs to ensure parity.
/// </summary>
internal static class TypeConversionMap
{
  /// <summary>
  /// Gets the CLR type and parse expression for a built-in type constraint.
  /// Returns null if the constraint is not a known built-in type.
  /// </summary>
  /// <param name="constraint">The type constraint from the route pattern (e.g., "int", "FileInfo")</param>
  /// <param name="varName">The variable name to use in the parse expression</param>
  /// <returns>Tuple of (fullyQualifiedClrType, parseExpression) or null if unknown</returns>
  public static (string ClrType, string ParseExpr)? GetBuiltInConversion(string constraint, string varName)
  {
    return constraint.ToLowerInvariant() switch
    {
      // Primitive numeric types
      "int" => ("int", $"int.Parse({varName}, global::System.Globalization.CultureInfo.InvariantCulture)"),
      "long" => ("long", $"long.Parse({varName}, global::System.Globalization.CultureInfo.InvariantCulture)"),
      "double" => ("double", $"double.Parse({varName}, global::System.Globalization.CultureInfo.InvariantCulture)"),
      "decimal" => ("decimal", $"decimal.Parse({varName}, global::System.Globalization.CultureInfo.InvariantCulture)"),
      "float" => ("float", $"float.Parse({varName}, global::System.Globalization.CultureInfo.InvariantCulture)"),
      "byte" => ("byte", $"byte.Parse({varName}, global::System.Globalization.CultureInfo.InvariantCulture)"),
      "sbyte" => ("sbyte", $"sbyte.Parse({varName}, global::System.Globalization.CultureInfo.InvariantCulture)"),
      "short" => ("short", $"short.Parse({varName}, global::System.Globalization.CultureInfo.InvariantCulture)"),
      "ushort" => ("ushort", $"ushort.Parse({varName}, global::System.Globalization.CultureInfo.InvariantCulture)"),
      "uint" => ("uint", $"uint.Parse({varName}, global::System.Globalization.CultureInfo.InvariantCulture)"),
      "ulong" => ("ulong", $"ulong.Parse({varName}, global::System.Globalization.CultureInfo.InvariantCulture)"),

      // Other primitives
      "bool" => ("bool", $"bool.Parse({varName})"),
      "char" => ("char", $"{varName}[0]"),

      // System value types
      "guid" => ("global::System.Guid", $"global::System.Guid.Parse({varName})"),
      "datetime" => ("global::System.DateTime", $"global::System.DateTime.Parse({varName}, global::System.Globalization.CultureInfo.InvariantCulture)"),
      "timespan" => ("global::System.TimeSpan", $"global::System.TimeSpan.Parse({varName}, global::System.Globalization.CultureInfo.InvariantCulture)"),
      "dateonly" => ("global::System.DateOnly", $"global::System.DateOnly.Parse({varName}, global::System.Globalization.CultureInfo.InvariantCulture)"),
      "timeonly" => ("global::System.TimeOnly", $"global::System.TimeOnly.Parse({varName}, global::System.Globalization.CultureInfo.InvariantCulture)"),

      // Reference types
      "uri" => ("global::System.Uri", $"new global::System.Uri({varName}, global::System.UriKind.RelativeOrAbsolute)"),
      "fileinfo" => ("global::System.IO.FileInfo", $"new global::System.IO.FileInfo({varName})"),
      "directoryinfo" => ("global::System.IO.DirectoryInfo", $"new global::System.IO.DirectoryInfo({varName})"),
      "ipaddress" => ("global::System.Net.IPAddress", $"global::System.Net.IPAddress.Parse({varName})"),

      _ => null
    };
  }

  /// <summary>
  /// Checks if the given constraint is a known built-in type.
  /// </summary>
  public static bool IsBuiltInType(string constraint)
  {
    return GetBuiltInConversion(constraint, "_") is not null;
  }

  /// <summary>
  /// Gets the fully qualified CLR type name for a built-in type constraint.
  /// Returns null if the constraint is not a known built-in type.
  /// </summary>
  /// <param name="constraint">The type constraint from the route pattern (e.g., "int", "FileInfo")</param>
  /// <returns>Fully qualified CLR type name or null if unknown</returns>
  public static string? GetClrTypeName(string constraint)
  {
    return constraint.ToLowerInvariant() switch
    {
      // Primitive numeric types - use keyword forms for properties
      "int" or "int32" => "global::System.Int32",
      "long" or "int64" => "global::System.Int64",
      "short" or "int16" => "global::System.Int16",
      "byte" => "global::System.Byte",
      "sbyte" => "global::System.SByte",
      "ushort" or "uint16" => "global::System.UInt16",
      "uint" or "uint32" => "global::System.UInt32",
      "ulong" or "uint64" => "global::System.UInt64",
      "float" or "single" => "global::System.Single",
      "double" => "global::System.Double",
      "decimal" => "global::System.Decimal",
      "bool" or "boolean" => "global::System.Boolean",
      "char" => "global::System.Char",
      "string" => "global::System.String",

      // System value types
      "guid" => "global::System.Guid",
      "datetime" => "global::System.DateTime",
      "datetimeoffset" => "global::System.DateTimeOffset",
      "timespan" => "global::System.TimeSpan",
      "dateonly" => "global::System.DateOnly",
      "timeonly" => "global::System.TimeOnly",

      // Reference types
      "uri" => "global::System.Uri",
      "version" => "global::System.Version",
      "fileinfo" => "global::System.IO.FileInfo",
      "directoryinfo" => "global::System.IO.DirectoryInfo",
      "ipaddress" => "global::System.Net.IPAddress",

      _ => null
    };
  }
}
