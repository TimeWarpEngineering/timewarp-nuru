# Migrate Routing Tests To Jaribu Multi Mode (21 Files)

## Description

Migrate the 21 routing test files in `tests/timewarp-nuru-tests/routing/` to support Jaribu multi-mode pattern, then move them to `tests/timewarp-nuru-core-tests/routing/`.

## Files to Migrate

- [x] `routing-01-basic-matching.cs`
- [x] `routing-02-parameter-binding.cs`
- [x] `routing-03-optional-parameters.cs`
- [x] `routing-04-catch-all.cs`
- [x] `routing-05-option-matching.cs`
- [x] `routing-06-repeated-options.cs`
- [x] `routing-07-route-selection.cs`
- [x] `routing-08-end-of-options.cs`
- [x] `routing-09-complex-integration.cs`
- [x] `routing-10-error-cases.cs`
- [x] `routing-11-delegate-mediator.cs` (standalone only - Mediator conflict)
- [x] `routing-12-colon-filtering.cs`
- [x] `routing-13-negative-numbers.cs`
- [x] `routing-14-option-order-independence.cs`
- [x] `routing-15-help-route-priority.cs`
- [x] `routing-16-typed-catch-all.cs`
- [x] `routing-17-additional-primitive-types.cs`
- [x] `routing-18-option-alias-with-description.cs`
- [x] `routing-20-version-route-override.cs`
- [x] `routing-21-check-updates-version-comparison.cs`
- [x] `routing-22-async-task-int-return.cs` (standalone only - Mediator conflict)
- [x] `routing-test-plan.md` (move only, not a test file)

## Migration Pattern

For each test file:
1. Add `#if !JARIBU_MULTI` / `return await RunAllTests();` / `#endif` block
2. Wrap types in namespace block (`TimeWarp.Nuru.Tests.Routing`)
3. Add `[ModuleInitializer]` registration method
4. Remove `[ClearRunfileCache]` attribute if present

## Checklist

- [x] Migrate all 21 routing test files
- [x] Move files to `tests/timewarp-nuru-core-tests/routing/`
- [x] Move `routing-test-plan.md` to new location
- [x] Update Directory.Build.props to include glob pattern
- [x] Test standalone mode for each file
- [x] Test multi-mode: `dotnet tests/ci-tests/run-ci-tests.cs`
- [ ] Commit changes

## Notes

- Original CI test count: 1024 tests
- Final CI test count: 1192 tests (added ~168 routing tests)
- Use namespace `TimeWarp.Nuru.Tests.Routing` for all files
- Note: routing-19 is missing from the sequence (file doesn't exist)
- `routing-11` and `routing-22` excluded from multi-mode due to Mediator.SourceGenerator conflicts
- Refer to `tests/timewarp-nuru-core-tests/parser/` for migration examples
