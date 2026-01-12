# Bug: Typed repeated options not converted from string array

## Description

When the source generator processes route patterns with typed repeated options (e.g., `{id:int}*`), the generated code passes `string[]` directly to handlers expecting typed arrays like `int[]`, `double[]`, `bool[]`, etc., causing CS1503 type conversion errors.

## Reproduction

**Files affected:** Multiple routing tests in `tests/timewarp-nuru-core-tests/routing/`

**Example patterns:**
```csharp
.Map("process --id {id:int}*").WithHandler((int[] id) => ...)
.Map("calc --values {v:double}*").WithHandler((double[] v) => ...)
.Map("docker run -i -t --env {e}* -- {*cmd}").WithHandler((bool i, bool t, string[] e, string[] cmd) => ...)
```

**Errors:**
```
error CS1503: Argument 1: cannot convert from 'string' to 'string[]'
error CS1503: Argument 1: cannot convert from 'string[]' to 'int[]'
error CS1503: Argument 1: cannot convert from 'string[]' to 'double[]'
```

## Progress

### Completed Fixes

1. **Typed catchall parameters** (`{*numbers:int}`) - Fixed in `route-matcher-emitter.cs`:
   - Added `EmitCatchAllTypeConversion()` method for array type conversion
   - Updated `EmitTypeConversions()` to handle catchall differently
   - **TypedCatchAll tests: 13/13 passing**

2. **Handler invoker for typed catchalls** - Fixed in `handler-invoker-emitter.cs`:
   - Updated variable name selection to use converted variable for typed catchalls
   - Handles `string` typed catchalls specially (no conversion needed)

3. **NURU_R002 diagnostic** - Added duplicate route pattern detection

4. **NURU_R003 diagnostic** - Added unreachable route detection (Task #351)

5. **Endpoint isolation** - Implemented `.DiscoverEndpoints()` and `.Map<T>()` (Task #352)

### Blocking Issue - RESOLVED

~~CI tests still fail (34 failures) even though individual test files pass.~~

**Fixed by Task #351 and #352:**
- Merged validation into NuruGenerator
- Added endpoint isolation - attributed routes no longer auto-included
- CI tests now run: 456 passed, 9 failed (pre-existing), 2 skipped

## Original Root Cause Analysis (for repeated options)

The `EmitValueOptionParsing` method in `route-matcher-emitter.cs`:

1. **Only collects ONE value** - The loop breaks after finding the first occurrence
2. **Declares `string?` not `string[]`** - Single value instead of collection
3. **Doesn't check `option.IsRepeated`** - The property exists but is never used

## Remaining Work

### Repeated Option Fixes Still Needed

- [ ] Add repeated option detection in `EmitValueOptionParsing`
- [ ] Emit `List<string>` collection for repeated options
- [ ] Don't break early - collect all values
- [ ] Add array type conversion in `EmitOptionTypeConversion`

**Note:** 9 pre-existing test failures likely include repeated option tests.

## CI Test Status

```
Passed: 456
Failed: 9 (pre-existing, likely includes repeated option tests)
Skipped: 2
```

## Key Code Locations

- `route-matcher-emitter.cs` - Option parsing and type conversion
- `handler-invoker-emitter.cs` - Builds argument list for handler calls
- `overlap-validator.cs` - Duplicate route detection (NURU_R002, R003)
- `nuru-generator.cs` - Combines fluent + attributed routes

## Files Modified

- `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs`
- `source/timewarp-nuru-analyzers/generators/emitters/handler-invoker-emitter.cs`
- `source/timewarp-nuru-analyzers/diagnostics/diagnostic-descriptors.overlap.cs`
- `source/timewarp-nuru-analyzers/validation/overlap-validator.cs`
- `source/timewarp-nuru-analyzers/AnalyzerReleases.Unshipped.md`

## Related

- Task #351: Merge NuruAnalyzer into NuruGenerator - **DONE**
- Task #352: Add DiscoverEndpoints() and Map<T>() - **DONE**
- Task #336: Add analyzer for ambiguous route patterns
- Bug #346, #347, #348, #350: Related generator fixes (done)
