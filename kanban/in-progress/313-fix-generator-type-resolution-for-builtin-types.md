# Fix generator type resolution for built-in types

## Summary

The generator only handles 7 built-in types but the runtime supports 21. Missing types like `FileInfo`, `DirectoryInfo`, `IPAddress`, `DateOnly`, `TimeOnly` cause CS0400 errors because the generator emits unqualified type names.

## Scope

This task covers **built-in types only** (21 types). Custom type converters (`AddTypeConverter()`) are out of scope - see Related Tasks.

## Affected Samples

- `samples/10-type-converters/01-builtin-types.cs` - uses all 21 built-in types
- `samples/10-type-converters/02-custom-type-converters.cs` - uses custom types (blocked until separate task)

## Errors

When running the built-in types sample, the generator produces invalid code:

1. **Types not fully qualified:**
   ```
   CS0400: The type or namespace name 'DirectoryInfo' could not be found in the global namespace
   CS0400: The type or namespace name 'FileInfo' could not be found in the global namespace
   CS0400: The type or namespace name 'IPAddress' could not be found in the global namespace
   ```

2. **Case-sensitive constraint names not normalized:**
   ```
   CS0400: The type or namespace name 'ipaddress' could not be found
   CS0400: The type or namespace name 'fileinfo' could not be found
   CS0400: The type or namespace name 'DIRECTORYINFO' could not be found
   ```

3. **Access modifier issues:**
   ```
   CS1527: Elements defined in a namespace cannot be explicitly declared as private
   ```

## Root Cause

The generator's `route-matcher-emitter.cs` has two switch statements (lines ~190 and ~401) that only handle 7 types:
- `int`, `long`, `double`, `decimal`, `bool`, `guid`, `datetime`

The runtime `DefaultTypeConverters.cs` supports 21 types. The generator needs parity.

## Implementation Plan

### 1. Create shared type conversion mapping

New file: `source/timewarp-nuru-analyzers/generators/emitters/type-conversion-map.cs`

```csharp
internal static class TypeConversionMap
{
  public static (string ClrType, string ParseExpr)? GetBuiltInConversion(string constraint, string varName)
  {
    return constraint.ToLowerInvariant() switch
    {
      // Primitives (7)
      "int" => ("int", $"int.Parse({varName}, global::System.Globalization.CultureInfo.InvariantCulture)"),
      "long" => ("long", $"long.Parse({varName}, ...)"),
      "double" => ("double", $"double.Parse({varName}, ...)"),
      "decimal" => ("decimal", $"decimal.Parse({varName}, ...)"),
      "bool" => ("bool", $"bool.Parse({varName})"),
      "byte" => ("byte", $"byte.Parse({varName}, ...)"),
      "sbyte" => ("sbyte", $"sbyte.Parse({varName}, ...)"),
      "short" => ("short", $"short.Parse({varName}, ...)"),
      "ushort" => ("ushort", $"ushort.Parse({varName}, ...)"),
      "uint" => ("uint", $"uint.Parse({varName}, ...)"),
      "ulong" => ("ulong", $"ulong.Parse({varName}, ...)"),
      "float" => ("float", $"float.Parse({varName}, ...)"),
      "char" => ("char", $"{varName}[0]"),
      
      // System types (5)
      "guid" => ("global::System.Guid", $"global::System.Guid.Parse({varName})"),
      "datetime" => ("global::System.DateTime", $"global::System.DateTime.Parse({varName}, ...)"),
      "timespan" => ("global::System.TimeSpan", $"global::System.TimeSpan.Parse({varName}, ...)"),
      "dateonly" => ("global::System.DateOnly", $"global::System.DateOnly.Parse({varName}, ...)"),
      "timeonly" => ("global::System.TimeOnly", $"global::System.TimeOnly.Parse({varName}, ...)"),
      
      // Reference types (4)
      "uri" => ("global::System.Uri", $"new global::System.Uri({varName}, global::System.UriKind.RelativeOrAbsolute)"),
      "fileinfo" => ("global::System.IO.FileInfo", $"new global::System.IO.FileInfo({varName})"),
      "directoryinfo" => ("global::System.IO.DirectoryInfo", $"new global::System.IO.DirectoryInfo({varName})"),
      "ipaddress" => ("global::System.Net.IPAddress", $"global::System.Net.IPAddress.Parse({varName})"),
      
      _ => null
    };
  }
}
```

### 2. Update route-matcher-emitter.cs

Replace switch statements in:
- `EmitTypeConversions()` (line ~190)
- `EmitOptionTypeConversion()` (line ~401)

Use the shared `TypeConversionMap.GetBuiltInConversion()` instead.

### 3. Fix access modifier issue

Investigate and fix the `CS1527: private at namespace level` error in generated code structure.

## Checklist

- [ ] Create `type-conversion-map.cs` with all 21 built-in types
- [ ] Update `EmitTypeConversions()` to use shared mapping
- [ ] Update `EmitOptionTypeConversion()` to use shared mapping
- [ ] Fix access modifier emission issue
- [ ] Verify `01-builtin-types.cs` sample compiles and runs
- [ ] Add generator test for new types (FileInfo, IPAddress, etc.)

## Files to Modify

- `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs`
- New: `source/timewarp-nuru-analyzers/generators/emitters/type-conversion-map.cs`

## All 21 Built-in Types

| Category | Constraint | Fully Qualified CLR Type |
|----------|------------|--------------------------|
| Primitives | `int` | `int` |
| | `long` | `long` |
| | `double` | `double` |
| | `decimal` | `decimal` |
| | `bool` | `bool` |
| | `byte` | `byte` |
| | `sbyte` | `sbyte` |
| | `short` | `short` |
| | `ushort` | `ushort` |
| | `uint` | `uint` |
| | `ulong` | `ulong` |
| | `float` | `float` |
| | `char` | `char` |
| System | `guid` | `global::System.Guid` |
| | `datetime` | `global::System.DateTime` |
| | `timespan` | `global::System.TimeSpan` |
| | `dateonly` | `global::System.DateOnly` |
| | `timeonly` | `global::System.TimeOnly` |
| Reference | `uri` | `global::System.Uri` |
| | `fileinfo` | `global::System.IO.FileInfo` |
| | `directoryinfo` | `global::System.IO.DirectoryInfo` |
| | `ipaddress` | `global::System.Net.IPAddress` |

## Related Tasks

- **TODO:** Create new task for custom type converter support in generator (`AddTypeConverter()`)
  - Will need to detect `AddTypeConverter()` calls
  - Decide: runtime lookup vs compile-time inlining
  - `02-custom-type-converters.cs` sample blocked until this is done

## Notes

The runtime `DefaultTypeConverters.cs` is the source of truth for which types are supported. The generator should mirror this list.
