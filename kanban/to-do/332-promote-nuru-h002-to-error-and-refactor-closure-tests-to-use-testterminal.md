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
- [ ] Change `DiagnosticSeverity.Warning` to `DiagnosticSeverity.Error` in `diagnostic-descriptors.handler.cs`
- [ ] Update message format to remove "will be skipped" language
- [ ] Update `AnalyzerReleases.Unshipped.md` severity from Warning to Error

### Part 2: Refactor Tests (start with routing-04-catch-all.cs to verify approach)
- [ ] `routing-04-catch-all.cs` (5 tests) - **START HERE to verify approach**
- [ ] `routing-05-option-matching.cs` (26 tests)
- [ ] `routing-06-repeated-options.cs` (6 tests)
- [ ] `routing-08-end-of-options.cs` (4 tests)
- [ ] `routing-09-complex-integration.cs` (4 tests)
- [ ] `routing-16-typed-catch-all.cs` (11 tests)
- [ ] `routing-17-additional-primitive-types.cs` (16 tests)

**Total: 72 test cases across 8 files**

### Part 3: Verification
- [ ] All routing tests pass
- [ ] `dotnet build` succeeds with no warnings/errors
- [ ] Verify NURU_H002 emits Error when closure detected

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
