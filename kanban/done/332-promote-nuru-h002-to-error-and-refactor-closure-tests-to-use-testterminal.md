# Promote NURU_H002 to Error and refactor closure tests to use TestTerminal

## Summary

The NURU_H002 analyzer correctly detects lambdas with closures but is only a Warning. The generator still produces broken code that fails with CS0103 errors. Promote to Error so builds fail early with a clear message, and refactor all affected tests to use the TestTerminal pattern instead of closures.

## Background

Lambda handlers that capture external variables (closures) cannot be transformed into local functions by the generator because the captured variables don't exist in the generated interceptor scope.

**Current behavior:**
1. Analyzer emits Warning: "Handler lambda captures external variable(s): boundArgs"
2. Generator still produces code referencing `boundArgs`
3. Build fails with cryptic: `error CS0103: The name 'boundArgs' does not exist in the current context`

**Desired behavior:**
1. Analyzer emits Error: "Handler lambda captures external variable(s): boundArgs"
2. Build fails immediately with clear message
3. No broken generated code

## Checklist

### Part 1: Update Diagnostic
- [x] Change `DiagnosticSeverity.Warning` to `DiagnosticSeverity.Error` in `diagnostic-descriptors.handler.cs`
- [x] Update message format to remove "will be skipped" language
- [x] Update `AnalyzerReleases.Unshipped.md` severity from Warning to Error
- [x] Add new analyzer NURU_H006 for discard parameters (`_`) which also don't work

### Part 2: Refactor Tests (start with routing-04-catch-all.cs to verify approach)
- [x] `routing-04-catch-all.cs` (5 tests) - **VERIFIED: 5/5 PASS**
- [x] `routing-05-option-matching.cs` (26 tests) - Refactored (28/31 pass, 3 pre-existing generator bugs)
- [x] `routing-06-repeated-options.cs` (6 tests) - Refactored (has pre-existing generator bug with `{e}*`)
- [x] `routing-08-end-of-options.cs` (4 tests) - Refactored (has pre-existing generator bug with `--`)
- [x] `routing-09-complex-integration.cs` (4 tests) - Refactored
- [x] `routing-16-typed-catch-all.cs` (11 tests) - Refactored
- [x] `routing-17-additional-primitive-types.cs` (16 tests) - Refactored

**Total: 72 test cases across 7 files refactored**

### Part 3: Verification
- [x] `routing-04-catch-all.cs` - 5/5 PASS
- [x] `dotnet build` succeeds with 0 warnings, 0 errors
- [x] NURU_H002 now emits Error when closure detected
- [x] NURU_H006 emits Error when discard parameter detected

## Test Refactoring Pattern

**Before (closure - NOT SUPPORTED):**
```csharp
string[]? boundArgs = null;
NuruCoreApp app = NuruApp.CreateBuilder([])
  .Map("run {*args}").WithHandler((string[] args) => { boundArgs = args; }).AsCommand().Done()
  .Build();
await app.RunAsync(["run", "one", "two"]);
boundArgs.Length.ShouldBe(2);
boundArgs[0].ShouldBe("one");
```

**After (TestTerminal - PREFERRED):**
```csharp
using TestTerminal terminal = new();
NuruCoreApp app = NuruApp.CreateBuilder([])
  .UseTerminal(terminal)
  .Map("run {*args}").WithHandler((string[] args) => string.Join(",", args)).AsCommand().Done()
  .Build();
await app.RunAsync(["run", "one", "two"]);
terminal.OutputContains("one,two").ShouldBeTrue();
```

**Key changes:**
1. Create `TestTerminal` and use `.UseTerminal(terminal)`
2. Handler returns a value instead of mutating captured variable
3. Assert using `terminal.OutputContains(...)` or `terminal.Output`

## Notes

- Discovered while fixing #331 (catch-all parameter variable naming)
- The analyzer already exists and correctly detects closures - just needs severity bump
- See `routing-03-optional-parameters.cs` for examples of the TestTerminal pattern already in use
- For complex assertions (array length, multiple values), format output to be easily parseable

## Files to Modify

**Analyzer:**
- `source/timewarp-nuru-analyzers/diagnostics/diagnostic-descriptors.handler.cs`
- `source/timewarp-nuru-analyzers/AnalyzerReleases.Unshipped.md`

**Tests:**
- `tests/timewarp-nuru-core-tests/routing/routing-04-catch-all.cs`
- `tests/timewarp-nuru-core-tests/routing/routing-05-option-matching.cs`
- `tests/timewarp-nuru-core-tests/routing/routing-06-repeated-options.cs`
- `tests/timewarp-nuru-core-tests/routing/routing-08-end-of-options.cs`
- `tests/timewarp-nuru-core-tests/routing/routing-09-complex-integration.cs`
- `tests/timewarp-nuru-core-tests/routing/routing-16-typed-catch-all.cs`
- `tests/timewarp-nuru-core-tests/routing/routing-17-additional-primitive-types.cs`

## Results

### Completed
1. **NURU_H002 promoted to Error** - Closures in lambda handlers now fail the build immediately with a clear message
2. **Added NURU_H006** - New analyzer for discard parameters (`_`) which also cannot be transformed
3. **7 test files refactored** - Converted from closure pattern to TestTerminal pattern
4. **Build succeeds** - 0 warnings, 0 errors

### Discovered Pre-existing Generator Bugs (separate tasks needed)
During refactoring, we discovered these generator issues that are NOT related to closures:

1. **Repeated options (`{e}*`)** - Generator passes single value instead of array (routing-06)
2. **End-of-options (`--`)** - Routes with `--` separator not being intercepted (routing-08)
3. **Optional flag values** - Some type conversion issues with optional flag values (routing-05)

These are pre-existing bugs in the generator, not related to task #332. The test refactoring exposed them because we can now run the tests without CS0103 errors.

### Files Modified
**Analyzer (3 files):**
- `source/timewarp-nuru-analyzers/analyzers/nuru-handler-analyzer.cs` - Added `HasDiscardParameters()` and NURU_H006 detection
- `source/timewarp-nuru-analyzers/diagnostics/diagnostic-descriptors.handler.cs` - Changed H002 to Error, added H006
- `source/timewarp-nuru-analyzers/AnalyzerReleases.Unshipped.md` - Updated H002 severity, added H006

**Tests (7 files):**
- `tests/timewarp-nuru-core-tests/routing/routing-04-catch-all.cs`
- `tests/timewarp-nuru-core-tests/routing/routing-05-option-matching.cs`
- `tests/timewarp-nuru-core-tests/routing/routing-06-repeated-options.cs`
- `tests/timewarp-nuru-core-tests/routing/routing-08-end-of-options.cs`
- `tests/timewarp-nuru-core-tests/routing/routing-09-complex-integration.cs`
- `tests/timewarp-nuru-core-tests/routing/routing-16-typed-catch-all.cs`
- `tests/timewarp-nuru-core-tests/routing/routing-17-additional-primitive-types.cs`
