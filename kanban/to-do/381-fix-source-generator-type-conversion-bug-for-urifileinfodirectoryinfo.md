# Fix Source Generator Type Conversion Bug for Uri/FileInfo/DirectoryInfo

## Problem

The source generator fails to generate type conversion code for `Uri`, `FileInfo`, and `DirectoryInfo` type constraints, causing CS0103 errors (undefined variables) at compile time.

**Affected samples:**
- `samples/10-type-converters/01-builtin-types.cs`
- `samples/10-type-converters/02-custom-type-converters.cs`

**Error pattern:**
```
error CS0103: The name 'target' does not exist in the current context
error CS0103: The name 'source' does not exist in the current context
error CS0103: The name 'dest' does not exist in the current context
error CS0103: The name 'file' does not exist in the current context
```

## Root Cause

**Location:** `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs` method `EmitTypeConversions()`

**Issue:** `TypeConversionMap.cs` returns `null` for `Uri`, `FileInfo`, `DirectoryInfo` (line 53):
```csharp
"uri" or "fileinfo" or "directoryinfo" => null,
```

The comment says "use try/catch wrapper pattern" but this is not implemented. When `GetBuiltInTryConversion` returns `null`, the code falls through to custom converter lookup, which also fails (no custom converters registered), resulting in only a warning comment being emitted:

```csharp
// WARNING: No converter found for type constraint 'DirectoryInfo'
// Register a converter with: builder.AddTypeConverter<YourConverter>();
```

**No actual conversion code is generated**, so the handler receives raw strings but expects typed parameters.

## Solution

Add special case handling in `EmitTypeConversions()` for these three types BEFORE the custom converter lookup.

### For Uri:
```csharp
if (baseType.Equals("uri", StringComparison.OrdinalIgnoreCase))
{
  sb.AppendLine($"{indentStr}global::System.Uri {escapedVarName};");
  sb.AppendLine($"{indentStr}if (!global::System.Uri.TryCreate({uniqueVarName}, global::System.UriKind.RelativeOrAbsolute, out global::System.Uri? {escapedVarName}Uri) || {escapedVarName}Uri is null)");
  sb.AppendLine($"{indentStr}{{");
  sb.AppendLine($"{indentStr}  app.Terminal.WriteLine($\"Error: Invalid value '{{{{{uniqueVarName}}}}}' for parameter '{param.Name}'. Expected: Uri\");");
  sb.AppendLine($"{indentStr}  return 1;");
  sb.AppendLine($"{indentStr}}}");
  sb.AppendLine($"{indentStr}{escapedVarName} = {escapedVarName}Uri;");
  return; // Skip further processing
}
```

### For FileInfo:
```csharp
if (baseType.Equals("fileinfo", StringComparison.OrdinalIgnoreCase))
{
  sb.AppendLine($"{indentStr}global::System.IO.FileInfo {escapedVarName};");
  sb.AppendLine($"{indentStr}try");
  sb.AppendLine($"{indentStr}{{");
  sb.AppendLine($"{indentStr}  {escapedVarName} = new global::System.IO.FileInfo({uniqueVarName});");
  sb.AppendLine($"{indentStr}}}");
  sb.AppendLine($"{indentStr}catch (global::System.ArgumentException)");
  sb.AppendLine($"{indentStr}{{");
  sb.AppendLine($"{indentStr}  app.Terminal.WriteLine($\"Error: Invalid value '{{{{{uniqueVarName}}}}}' for parameter '{param.Name}'. Expected: FileInfo\");");
  sb.AppendLine($"{indentStr}  return 1;");
  sb.AppendLine($"{indentStr}}}");
  return; // Skip further processing
}
```

### For DirectoryInfo:
```csharp
if (baseType.Equals("directoryinfo", StringComparison.OrdinalIgnoreCase))
{
  sb.AppendLine($"{indentStr}global::System.IO.DirectoryInfo {escapedVarName};");
  sb.AppendLine($"{indentStr}try");
  sb.AppendLine($"{indentStr}{{");
  sb.AppendLine($"{indentStr}  {escapedVarName} = new global::System.IO.DirectoryInfo({uniqueVarName});");
  sb.AppendLine($"{indentStr}}}");
  sb.AppendLine($"{indentStr}catch (global::System.ArgumentException)");
  sb.AppendLine($"{indentStr}{{");
  sb.AppendLine($"{indentStr}  app.Terminal.WriteLine($\"Error: Invalid value '{{{{{uniqueVarName}}}}}' for parameter '{param.Name}'. Expected: DirectoryInfo\");");
  sb.AppendLine($"{indentStr}  return 1;");
  sb.AppendLine($"{indentStr}}}");
  return; // Skip further processing
}
```

## Files to Modify

- `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs`
  - Method: `EmitTypeConversions()` (around line 640)
  - Add cases after the `if (conversion is var (clrType, tryParseCondition))` block
  - Add cases BEFORE the custom converter lookup

## Checklist

- [ ] Add Uri type conversion handling in EmitTypeConversions()
- [ ] Add FileInfo type conversion handling in EmitTypeConversions()
- [ ] Add DirectoryInfo type conversion handling in EmitTypeConversions()
- [ ] Verify samples/10-type-converters/01-builtin-types.cs builds
- [ ] Verify samples/10-type-converters/02-custom-type-converters.cs builds
- [ ] Run verify-samples to confirm all pass

## Notes

Priority: high

The fix mirrors the runtime `DefaultTypeConverters.cs` approach (Uri.TryCreate, FileInfo/DirectoryInfo constructor with try/catch) but needs to be implemented at the source generator level since it generates conversion code at compile time.
