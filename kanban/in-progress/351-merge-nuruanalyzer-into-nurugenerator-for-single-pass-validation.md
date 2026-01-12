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

The `NuruAnalyzer` should report NURU_R002 for duplicate routes, but it only validates `model.Routes` (fluent routes), not the combined model (fluent + attributed).

## Solution

Merge validation into `NuruGenerator`:
- Single generator builds complete model once
- Validates and reports diagnostics
- Emits code
- Delete separate `NuruAnalyzer`

## Benefits

1. **Performance** - Pipeline runs once, not twice
2. **Simpler architecture** - One generator instead of analyzer + generator
3. **Guaranteed consistency** - Validation happens on exact model being emitted
4. **Less code** - No separate `NuruAnalyzer` class

## Checklist

### Phase 1: Add Route Location Collection to Generator
- [ ] Add pipeline to collect `Map()` call locations (string pattern -> Location)
- [ ] Add pipeline to collect `[NuruRoute]` attribute locations
- [ ] Combine into `ImmutableDictionary<string, Location>` for error reporting

### Phase 2: Switch to ExtractWithDiagnostics
- [ ] Change `AppExtractor.Extract()` to `AppExtractor.ExtractWithDiagnostics()`
- [ ] Capture extraction errors (NURU_P###, NURU_S###, NURU_H### diagnostics)

### Phase 3: Add Validation in RegisterSourceOutput
- [ ] Before emitting code, validate the combined model
- [ ] For each app, combine `app.Routes` with `model.AttributedRoutes`
- [ ] Call `OverlapValidator.Validate()` with route locations
- [ ] Report all diagnostics (extraction + validation) via `ctx.ReportDiagnostic()`

### Phase 4: Delete NuruAnalyzer
- [ ] Remove `source/timewarp-nuru-analyzers/analyzers/nuru-analyzer.cs`
- [ ] Update any tests that specifically test `NuruAnalyzer`
- [ ] Verify all analyzer tests still pass through the generator

### Phase 5: Verify CI Tests
- [ ] Run CI tests: `ganda runfile cache --clear && ./tests/ci-tests/run-ci-tests.cs`
- [ ] Verify NURU_R002 is reported for duplicate `deploy {env}` routes
- [ ] Fix the attributed route scoping issue (separate task if needed)

## Files to Modify

- `source/timewarp-nuru-analyzers/generators/nuru-generator.cs` - Add validation pipeline

## Files to Delete

- `source/timewarp-nuru-analyzers/analyzers/nuru-analyzer.cs`

## Related

- Bug #349: Typed repeated options not converted from string array (discovered this during investigation)
- Task #336: Add analyzer for ambiguous route patterns (created the overlap validator)
- NURU_R001: Overlapping routes with different type constraints
- NURU_R002: Duplicate route pattern (added but not being triggered due to this bug)

## Notes

The `OverlapValidator` and `ModelValidator` classes remain unchanged - they already work correctly. The issue is that they're being called with an incomplete model (missing attributed routes).

Once this is fixed, we'll likely see NURU_R002 errors in CI tests where attributed routes conflict with fluent routes. That will need a follow-up fix to either:
1. Scope attributed routes to their source file
2. Add `App` parameter to `[NuruRoute]` for explicit app association
3. Some other approach to prevent attributed routes from polluting all apps
