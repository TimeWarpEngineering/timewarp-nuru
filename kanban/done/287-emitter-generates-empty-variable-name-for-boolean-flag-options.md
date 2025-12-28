# Emitter generates empty variable name for boolean flag options

## Description

The code emitter generates an empty variable name for boolean flag options, causing a CS1001 "Identifier expected" compiler error.

## Symptom

```csharp
// Generated code with missing identifier:
bool  = Array.Exists(args, a => a == "--" || a == "-b");  // CS1001 - no name!
```

Error:
```
CS1001: Identifier expected
```

## Expected

```csharp
bool b = Array.Exists(args, a => a == "-b");
```

## Root Cause

`OptionDefinition.LongForm` was `string` (non-nullable), but for short-only options like `-b`, the parser correctly returned `null`. The converter then coerced `null` to `""` (empty string) to satisfy the type. The emitter then used `ToCamelCase(option.LongForm)` which produced an empty variable name.

## Solution

1. Changed `OptionDefinition.LongForm` from `string` to `string?`
2. Removed the `?? ""` coercion in `pattern-string-extractor.cs`
3. Updated all emitters to handle null `LongForm` with pattern matching:
   - Fall back to `ShortForm` for variable names
   - Only emit long form check when `LongForm` is not null

## Checklist

- [x] Locate the emitter code that generates boolean flag extraction
- [x] Trace how flag parameter names are extracted from the route pattern
- [x] Fix the extraction/emission to include the parameter name
- [x] Verify bench-nuru-full compiles after fix

## Files Modified

- `source/timewarp-nuru-analyzers/generators/models/segment-definition.cs`
- `source/timewarp-nuru-analyzers/generators/extractors/pattern-string-extractor.cs`
- `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs`
- `source/timewarp-nuru-analyzers/generators/emitters/help-emitter.cs`
- `source/timewarp-nuru-analyzers/generators/emitters/capabilities-emitter.cs`
- `source/timewarp-nuru-analyzers/generators/extractors/attributed-route-extractor.cs`

## Also Fixes

This fix also resolves task #288 (Short-only options parsed incorrectly as empty long option).

## Discovered In

`benchmarks/aot-benchmarks/bench-nuru-full/Program.cs` with pattern `--str {str} -i {i:int} -b`
