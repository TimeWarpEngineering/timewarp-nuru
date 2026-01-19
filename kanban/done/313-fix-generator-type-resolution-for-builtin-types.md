# Fix generator type resolution for built-in types

## Status: COMPLETE ✅

All blockers resolved. Sample verified working.

## Summary

The generator now correctly resolves all 21 built-in types to fully-qualified CLR names:
- `FileInfo` → `global::System.IO.FileInfo` ✅
- `DirectoryInfo` → `global::System.IO.DirectoryInfo` ✅  
- `IPAddress` → `global::System.Net.IPAddress` ✅
- `DateOnly` → `global::System.DateOnly` ✅
- `TimeOnly` → `global::System.TimeOnly` ✅

## What Was Done

1. **Created `type-conversion-map.cs`** - Centralized type conversion mappings for all 21 built-in types
2. **Refactored `route-matcher-emitter.cs`** - Replaced 2 duplicate switch statements with calls to `TypeConversionMap`
3. **Refactored `pattern-string-extractor.cs`** - Uses shared `TypeConversionMap.GetClrTypeName()` for CLR type resolution
4. **Fixed `command-class-emitter.cs`** - `ToPascalCase` now handles kebab-case (`dry-run` → `DryRun`)

## Checklist

- [x] Create `type-conversion-map.cs` with all 21 built-in types
- [x] Update `EmitTypeConversions()` to use shared mapping
- [x] Update `EmitOptionTypeConversion()` to use shared mapping
- [x] Update `ResolveClrTypeName()` in pattern-string-extractor.cs
- [x] Fix `ToPascalCase()` to handle kebab-case property names
- [x] Verify type resolution is correct in generated code
- [x] Verify existing generator tests pass
- [x] Sample `01-builtin-types.cs` compiles and runs

## Files Modified

- `source/timewarp-nuru-analyzers/generators/emitters/type-conversion-map.cs` (NEW)
- `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs`
- `source/timewarp-nuru-analyzers/generators/emitters/command-class-emitter.cs`
- `source/timewarp-nuru-analyzers/generators/extractors/pattern-string-extractor.cs`

## Test Results

All commands in `samples/10-type-converters/01-builtin-types.cs` verified working:
- `delay 100` ✅ (int)
- `price 19.99 3` ✅ (double, int)
- `schedule Meeting 2024-12-25T14:30:00` ✅ (string, DateTime)
- `fetch https://example.com/api/data` ✅ (Uri)
- `read /etc/passwd` ✅ (FileInfo)
- `list /tmp` ✅ (DirectoryInfo)
- `ping 192.168.1.1` ✅ (IPAddress)
- `connect 10.0.0.1 8080` ✅ (IPAddress, int)
- `report 2024-12-25` ✅ (DateOnly)
- `alarm 07:30:00` ✅ (TimeOnly)
- `sync /source /destination` ✅ (DirectoryInfo, DirectoryInfo)
- `backup /data --dest /backup --config config.json` ✅ (DirectoryInfo, optional DirectoryInfo, optional FileInfo)

## Previously Blocked By (Now Resolved)

- #323 - Fix C# keyword escaping ✅ (completed)
- #324 - Fix handler parameter vs service injection ✅ (completed)

## Related Tasks

- **TODO:** Create task for custom type converter support in generator
