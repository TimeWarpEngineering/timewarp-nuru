# Fix generator type resolution for built-in types

## Status: BLOCKED

Type resolution implementation is complete, but sample verification is blocked by other bugs.

**Blocked by:**
- #323 - Fix C# keyword escaping in generated parameter names
- #324 - Fix handler parameter vs service injection confusion
- #325 - Fix block body handler indentation in generated code

## Summary

The generator now correctly resolves all 21 built-in types to fully-qualified CLR names:
- `FileInfo` → `global::System.IO.FileInfo` ✅
- `DirectoryInfo` → `global::System.IO.DirectoryInfo` ✅  
- `IPAddress` → `global::System.Net.IPAddress` ✅
- `DateOnly` → `global::System.DateOnly` ✅
- `TimeOnly` → `global::System.TimeOnly` ✅

**Commit:** `7efef874`

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
- [ ] Sample `01-builtin-types.cs` compiles and runs (BLOCKED)

## Files Modified

- `source/timewarp-nuru-analyzers/generators/emitters/type-conversion-map.cs` (NEW)
- `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs`
- `source/timewarp-nuru-analyzers/generators/emitters/command-class-emitter.cs`
- `source/timewarp-nuru-analyzers/generators/extractors/pattern-string-extractor.cs`

## Affected Samples

- `samples/10-type-converters/01-builtin-types.cs` - Type resolution works, but sample blocked by #323, #324, #325
- `samples/10-type-converters/02-custom-type-converters.cs` - Blocked by custom type converter support (future task)

## Related Tasks

- **TODO:** Create task for custom type converter support in generator
