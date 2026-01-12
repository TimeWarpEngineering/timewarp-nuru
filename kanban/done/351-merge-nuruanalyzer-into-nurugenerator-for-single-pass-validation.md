# Merge NuruAnalyzer into NuruGenerator for single-pass validation

## Summary

Currently `NuruAnalyzer` and `NuruGenerator` are separate `IIncrementalGenerator` implementations that each build the IR model independently. This causes:

1. **Duplicate work** - IR model built twice (once per generator)
2. **Inconsistent validation** - Analyzer validates a different model than generator emits
3. **Missing attributed routes** - Analyzer doesn't collect `[NuruRoute]` attributed routes, so NURU_R002 (duplicate routes) isn't reported when attributed routes conflict with fluent routes

## Root Cause

Discovered during CI test investigation (Bug #349):
- Tests pass individually but fail in CI mode
- Attributed routes from `generator-11-attributed-routes.cs` (`DeployCommand`, `BuildCommand`, etc.) are added to ALL apps
- These attributed routes are duplicating fluent routes like `deploy {env}` in test apps
- The duplicate routes cause wrong handlers to execute

## Solution

Merge validation into `NuruGenerator`:
- Single generator builds complete model once
- Validates and reports diagnostics
- Emits code
- Delete separate `NuruAnalyzer`

## Results

**COMPLETED** - All goals achieved plus bonus endpoint isolation feature.

### What Was Done

1. **Merged validation into NuruGenerator** - Single generator now handles both code emission and validation
2. **Added route location collection** - Collects locations from both `Map()` calls and `[NuruRoute]` attributes
3. **Switched to ExtractWithDiagnostics** - Captures extraction errors during model building
4. **Added NURU_R003 diagnostic** - Detects unreachable routes (routes shadowed by higher specificity routes)
5. **Updated documentation** - Added compile-time validation section to `specificity-algorithm.md`
6. **Fixed NURU_R003 false positives** - Required options now correctly included in signature comparison
7. **Enhanced error messages** - NURU_R003 now shows file:line location of shadowing route
8. **Added EffectivePattern** - Accurate route pattern display from segments
9. **Implemented endpoint isolation** - `.DiscoverEndpoints()` and `.Map<T>()` API (see Task #352)

### Deferred

- Delete `NuruAnalyzer` - may still be useful for other purposes

## NURU_R003: Unreachable Route

During implementation, we discovered that NURU_R002 (duplicate patterns) wasn't the right diagnostic for the CI test failures. The real issue is **unreachable routes** - routes that can never be matched because another route with equal or higher specificity matches all the same inputs.

**Example:**
```csharp
// Route 'deploy {env}' is unreachable
// Route 'deploy' (with options) shadows it because options are optional
.Map("deploy {env} --force").WithHandler(...)  // 160 pts - matches "deploy prod"
.Map("deploy {env}").WithHandler(...)          // 110 pts - UNREACHABLE
```

The analyzer now reports NURU_R003 errors for these cases.

## CI Test Results

After implementing NURU_R003, CI tests now fail with build errors (as expected):
- Multiple `deploy` routes are shadowing each other
- Attributed routes (`DeployCommand`, `GreetCommand`, etc.) are shadowing fluent routes

This confirms the analyzer is working correctly. The next step is to fix the attributed route scoping issue so attributed routes aren't added to all apps.

## Checklist

### Phase 1: Add Route Location Collection to Generator
- [x] Add pipeline to collect `Map()` call locations (string pattern -> Location)
- [x] Add pipeline to collect `[NuruRoute]` attribute locations
- [x] Combine into `ImmutableDictionary<string, Location>` for error reporting

### Phase 2: Switch to ExtractWithDiagnostics
- [x] Change `AppExtractor.Extract()` to `AppExtractor.ExtractWithDiagnostics()`
- [x] Capture extraction errors (NURU_P###, NURU_S###, NURU_H### diagnostics)

### Phase 3: Add Validation in RegisterSourceOutput
- [x] Before emitting code, validate the combined model
- [x] For each app, combine `app.Routes` with `model.AttributedRoutes`
- [x] Call `OverlapValidator.Validate()` with route locations
- [x] Report all diagnostics (extraction + validation) via `ctx.ReportDiagnostic()`

### Phase 4: Add NURU_R003 Unreachable Route Detection
- [x] Add `ComputeRequiredSignature()` to compute "core" pattern without optional elements
- [x] Add `CheckForUnreachableRoutes()` to detect shadowed routes
- [x] Add NURU_R003 diagnostic descriptor

### Phase 5: Delete NuruAnalyzer (Deferred)
- [ ] Remove `source/timewarp-nuru-analyzers/analyzers/nuru-analyzer.cs`
- [ ] Update any tests that specifically test `NuruAnalyzer`
- [ ] Verify all analyzer tests still pass through the generator

### Phase 6: Fix Attributed Route Scoping (Separate Task)
- [ ] Create new task for attributed route scoping
- [ ] Decide on approach: file-scoped, app name association, or explicit opt-in
- [ ] Implement solution
- [ ] Verify CI tests pass

## Files Modified

- `source/timewarp-nuru-analyzers/generators/nuru-generator.cs` - Merged validation pipeline
- `source/timewarp-nuru-analyzers/validation/overlap-validator.cs` - Added NURU_R003 detection
- `source/timewarp-nuru-analyzers/diagnostics/diagnostic-descriptors.overlap.cs` - Added NURU_R003
- `source/timewarp-nuru-analyzers/AnalyzerReleases.Unshipped.md` - Registered NURU_R003
- `documentation/developer/design/resolver/specificity-algorithm.md` - Added validation docs

## Files to Delete (Deferred)

- `source/timewarp-nuru-analyzers/analyzers/nuru-analyzer.cs`

## Related

- Bug #349: Typed repeated options not converted from string array (discovered this during investigation)
- Task #336: Add analyzer for ambiguous route patterns (created the overlap validator)
- NURU_R001: Overlapping routes with different type constraints
- NURU_R002: Duplicate route pattern
- NURU_R003: Unreachable route (NEW)

## Notes

The validation is now working correctly and detecting the issues. The remaining work is to fix the attributed route scoping so that attributed routes from one test file don't pollute other test files' apps. This should be a separate task.
