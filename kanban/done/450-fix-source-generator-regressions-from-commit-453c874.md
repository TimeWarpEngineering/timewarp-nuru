# Fix source generator regressions from commit 453c874

## Description

Commit `453c874` (refactor AOT DI for framework service registration) introduced two source generator regressions that break 93% of generated apps (54/58). CI fails at verify-samples. Any consumer app hitting these code paths will fail to compile.

## Checklist

- [x] Fix A: Remove early-return guard in `EmitServiceFields` (interceptor-emitter.cs:342-344) so `EnsureServicesInitialized` method and framework fields are always emitted
- [x] Fix B: Change behavior pipeline emission to use `.Value.HandleAsync` instead of `.HandleAsync` on `Lazy<T>` fields (behavior-emitter.cs:305, 310)
- [x] `dotnet build` — analyzer project compiles
- [x] `ganda runfile cache --clear`
- [x] `dev verify-samples` — 64/64 passing
- [x] `dotnet run tests/ci-tests/run-ci-tests.cs` — full CI test suite passes
- [x] Spot-check a generated `NuruGenerated.g.cs` to confirm method definition present and `.Value.HandleAsync` used

## Notes

### Root Cause

Commit `453c874` changed service resolution from `app` parameter to `__fw_NuruApp` static field and introduced `EnsureServicesInitialized` pattern. Two defects:

**Regression A: `EnsureServicesInitialized` call without method definition**
- `EmitMethodBody` (interceptor-emitter.cs:889-891) unconditionally emits call to `EnsureServicesInitialized(app, configuration)`
- `EmitServiceFields` (interceptor-emitter.cs:325-378) has early-return at line 343 when zero cached services and zero framework service types — skips emitting the method definition
- Result: `CS0103: The name 'EnsureServicesInitialized' does not exist in the current context`

**Regression B: `Lazy<T>.HandleAsync` invocation**
- Behavior fields declared as `Lazy<TBehavior>` (behavior-emitter.cs:41-42)
- Pipeline invocation calls `.HandleAsync(...)` directly on `Lazy<T>` wrapper (lines 305, 310)
- Should call `.Value.HandleAsync(...)`
- Result: `CS1061: 'Lazy<T>' does not contain a definition for 'HandleAsync'`

### Fix A — Exact Change

Delete lines 342-344 in `interceptor-emitter.cs`:
```csharp
    // Only emit if there are cached services OR framework service types needed
    if (cachedServices.Length == 0 && frameworkServiceTypes.Count == 0)
      return;
```

Downstream code handles empty collections correctly (foreach over empty arrays, topological sort of empty input). The emitted method will set `__fw_NuruApp = app` and `__fw_ITerminal = app.Terminal` with zero user service lines.

### Fix B — Exact Change

In `behavior-emitter.cs`, two lines:
- Line 305: `{fieldName}.HandleAsync(` → `{fieldName}.Value.HandleAsync(`
- Line 310: `{fieldName}.HandleAsync(` → `{fieldName}.Value.HandleAsync(`

### Blast Radius

| Category | Count |
|---|---|
| A-only (missing method def) | 40 |
| A+B (both bugs) | 14 |
| Unaffected | 4 |
| **Total affected** | **54/58 (93%)** |

### Files to Edit

1. `source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs` — delete 3 lines (342-344)
2. `source/timewarp-nuru-analyzers/generators/emitters/behavior-emitter.cs` — change 2 lines (305, 310)

### Diagnostic Reports

- `.agent/workspace/2026-03-24T00-49-56Z_diagnosis-source-generator-regression-453c874.md`
- `.agent/workspace/2026-03-24T01-05-00_diagnosis-blast-radius-generator-regression-453c874.md`

## Results

- Removed early-return guard in `EmitServiceFields` (interceptor-emitter.cs:342-344) — `EnsureServicesInitialized` method and framework fields now always emitted
- Changed behavior pipeline emission to use `.Value.HandleAsync()` on `Lazy<T>` fields (behavior-emitter.cs:305, 310)
- 2 files changed, 2 insertions, 6 deletions
- verify-samples: 64/64 passing (was 7/64)
- CI tests: 1120 passed, 7 skipped, 0 failed
- Commit: 72aad9dd
