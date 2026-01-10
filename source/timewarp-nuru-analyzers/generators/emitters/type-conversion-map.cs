namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Provides compile-time type conversion mappings for built-in types.
/// This mirrors the runtime DefaultTypeConverters.cs to ensure parity.
/// </summary>
/// <remarks>
/// Supports:
/// - C# primitive keywords: int, bool, long, double, decimal, float, byte, sbyte, short, ushort, uint, ulong, char
/// - CLR type names: Int32, Boolean, Int64, Double, Decimal, Single, Byte, SByte, Int16, UInt16, UInt32, UInt64, Char
/// - PascalCase type names: DateTime, TimeSpan, Guid, Uri, FileInfo, DirectoryInfo, IPAddress, DateOnly, TimeOnly
/// All matching is case-insensitive.
/// </remarks>
internal static class TypeConversionMap
{
  /// <summary>
  /// Gets the CLR type and parse expression for a built-in type constraint.
  /// Returns null if the constraint is not a known built-in type.
  /// </summary>
  /// <param name="constraint">The type constraint from the route pattern (e.g., "int", "DateTime", "FileInfo")</param>
  /// <param name="varName">The variable name to use in the parse expression</param>
  /// <returns>Tuple of (fullyQualifiedClrType, parseExpression) or null if unknown</returns>
  public static (string ClrType, string ParseExpr)? GetBuiltInConversion(string constraint, string varName)
  {
    return constraint.ToLowerInvariant() switch
    {
      // C# primitive keywords (primary) and CLR type names
      "int" or "int32" => ("int", $"int.Parse({varName}, global::System.Globalization.CultureInfo.InvariantCulture)"),
      "long" or "int64" => ("long", $"long.Parse({varName}, global::System.Globalization.CultureInfo.InvariantCulture)"),
      "short" or "int16" => ("short", $"short.Parse({varName}, global::System.Globalization.CultureInfo.InvariantCulture)"),
      "byte" => ("byte", $"byte.Parse({varName}, global::System.Globalization.CultureInfo.InvariantCulture)"),
      "sbyte" => ("sbyte", $"sbyte.Parse({varName}, global::System.Globalization.CultureInfo.InvariantCulture)"),
      "ushort" or "uint16" => ("ushort", $"ushort.Parse({varName}, global::System.Globalization.CultureInfo.InvariantCulture)"),
      "uint" or "uint32" => ("uint", $"uint.Parse({varName}, global::System.Globalization.CultureInfo.InvariantCulture)"),
      "ulong" or "uint64" => ("ulong", $"ulong.Parse({varName}, global::System.Globalization.CultureInfo.InvariantCulture)"),
      "float" or "single" => ("float", $"float.Parse({varName}, global::System.Globalization.CultureInfo.InvariantCulture)"),
      "double" => ("double", $"double.Parse({varName}, global::System.Globalization.CultureInfo.InvariantCulture)"),
      "decimal" => ("decimal", $"decimal.Parse({varName}, global::System.Globalization.CultureInfo.InvariantCulture)"),
      "bool" or "boolean" => ("bool", $"bool.Parse({varName})"),
      "char" => ("char", $"{varName}[0]"),

      // PascalCase type names (case-insensitive)
      "guid" => ("global::System.Guid", $"global::System.Guid.Parse({varName})"),
      "datetime" => ("global::System.DateTime", $"global::System.DateTime.Parse({varName}, global::System.Globalization.CultureInfo.InvariantCulture)"),
      "timespan" => ("global::System.TimeSpan", $"global::System.TimeSpan.Parse({varName}, global::System.Globalization.CultureInfo.InvariantCulture)"),
      "dateonly" => ("global::System.DateOnly", $"global::System.DateOnly.Parse({varName}, global::System.Globalization.CultureInfo.InvariantCulture)"),
      "timeonly" => ("global::System.TimeOnly", $"global::System.TimeOnly.Parse({varName}, global::System.Globalization.CultureInfo.InvariantCulture)"),
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
  /// <param name="constraint">The type constraint from the route pattern (e.g., "int", "DateTime", "FileInfo")</param>
  /// <returns>Fully qualified CLR type name or null if unknown</returns>
  public static string? GetClrTypeName(string constraint)
  {
    return constraint.ToLowerInvariant() switch
    {
      // C# primitive keywords and CLR type names
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

      // PascalCase type names (case-insensitive)
      "guid" => "global::System.Guid",
      "datetime" => "global::System.DateTime",
      "datetimeoffset" => "global::System.DateTimeOffset",
      "timespan" => "global::System.TimeSpan",
      "dateonly" => "global::System.DateOnly",
      "timeonly" => "global::System.TimeOnly",
      "uri" => "global::System.Uri",
      "version" => "global::System.Version",
      "fileinfo" => "global::System.IO.FileInfo",
      "directoryinfo" => "global::System.IO.DirectoryInfo",
      "ipaddress" => "global::System.Net.IPAddress",

      _ => null
    };
  }
}
