# Fix generator type resolution for built-in types

## Status: COMPLETE (with caveats)

**Commit:** `7efef874` - Type resolution for all 21 built-in types is now working.

The sample `01-builtin-types.cs` still fails to compile due to **separate pre-existing bugs** (see Remaining Issues below).

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
- [ ] Sample `01-builtin-types.cs` blocked by separate bugs

## Remaining Issues (Separate Bugs)

The sample `01-builtin-types.cs` cannot compile due to **pre-existing generator bugs** not related to type resolution:

### 1. C# Keyword Escaping
Parameter named `event` generates `string event = ...` instead of `string @event = ...`

**Route:** `schedule {event} {when:DateTime}`
**Error:** CS0065 - `event` is a C# keyword

### 2. Handler Parameter vs Service Injection Confusion  
Handler parameters of types like `IPAddress` are incorrectly treated as services requiring injection.

**Route:** `connect {host:ipaddress} {port:int}`
**Error:** Generator emits both route binding AND service injection for `host`

### 3. Block Body Handler Formatting
Multi-line lambda handlers have incorrect indentation in generated local functions.

**These issues should be tracked as separate tasks.**

## Files Modified

- `source/timewarp-nuru-analyzers/generators/emitters/type-conversion-map.cs` (NEW)
- `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs`
- `source/timewarp-nuru-analyzers/generators/emitters/command-class-emitter.cs`
- `source/timewarp-nuru-analyzers/generators/extractors/pattern-string-extractor.cs`

## Related Tasks

- **TODO:** Create task for C# keyword escaping in parameter names
- **TODO:** Create task for handler parameter vs service injection logic
- **TODO:** Create task for custom type converter support in generator
