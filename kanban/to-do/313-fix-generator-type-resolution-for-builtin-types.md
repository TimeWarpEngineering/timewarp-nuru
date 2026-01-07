# Fix generator type resolution for built-in types

## Summary

The generator fails to properly resolve and emit code for built-in types like `FileInfo`, `DirectoryInfo`, `IPAddress`, `DateOnly`, `TimeOnly`. These types need to be fully qualified in generated code.

## Affected Samples

- `samples/10-type-converters/01-builtin-types.cs`
- `samples/10-type-converters/02-custom-type-converters.cs`

## Errors

When running these samples, the generator produces invalid code:

1. **Types not fully qualified:**
   ```
   CS0400: The type or namespace name 'DirectoryInfo' could not be found in the global namespace
   CS0400: The type or namespace name 'FileInfo' could not be found in the global namespace
   CS0400: The type or namespace name 'IPAddress' could not be found in the global namespace
   CS0400: The type or namespace name 'DateOnly' could not be found in the global namespace
   CS0400: The type or namespace name 'TimeOnly' could not be found in the global namespace
   ```

2. **Case-sensitive constraint names not normalized:**
   ```
   CS0400: The type or namespace name 'ipaddress' could not be found
   CS0400: The type or namespace name 'dateonly' could not be found
   CS0400: The type or namespace name 'fileinfo' could not be found
   CS0400: The type or namespace name 'DIRECTORYINFO' could not be found
   ```

3. **Access modifier issues:**
   ```
   CS1527: Elements defined in a namespace cannot be explicitly declared as private
   ```

## Root Cause

The generator emits type names as they appear in the route pattern constraint (e.g., `{path:FileInfo}`) instead of:
1. Normalizing the case
2. Looking up the actual fully-qualified type name (e.g., `global::System.IO.FileInfo`)

## Checklist

- [ ] Fix type resolution to use fully-qualified names for built-in types
- [ ] Normalize constraint names to proper casing before type lookup
- [ ] Fix access modifier emission for generated types
- [ ] Verify `01-builtin-types.cs` sample runs correctly
- [ ] Verify `02-custom-type-converters.cs` sample runs correctly

## Technical Notes

### Built-in types that need full qualification

| Constraint | Fully Qualified Type |
|------------|---------------------|
| `FileInfo` | `global::System.IO.FileInfo` |
| `DirectoryInfo` | `global::System.IO.DirectoryInfo` |
| `IPAddress` | `global::System.Net.IPAddress` |
| `DateOnly` | `global::System.DateOnly` |
| `TimeOnly` | `global::System.TimeOnly` |
| `Uri` | `global::System.Uri` |
| `Guid` | `global::System.Guid` |
| `DateTime` | `global::System.DateTime` |
| `TimeSpan` | `global::System.TimeSpan` |

### Key files to investigate

- `source/timewarp-nuru-analyzers/generators/emitters/` - Where types are emitted
- `source/timewarp-nuru-core/routing/type-converters/` - Built-in type converter registry
