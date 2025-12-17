# Migrate Routing Tests To Jaribu Multi Mode (21 Files)

## Description

Migrate the 21 routing test files in `tests/timewarp-nuru-tests/routing/` to support Jaribu multi-mode pattern, then move them to `tests/timewarp-nuru-core-tests/routing/`.

## Files to Migrate

- [ ] `routing-01-basic-matching.cs`
- [ ] `routing-02-parameter-binding.cs`
- [ ] `routing-03-optional-parameters.cs`
- [ ] `routing-04-catch-all.cs`
- [ ] `routing-05-option-matching.cs`
- [ ] `routing-06-repeated-options.cs`
- [ ] `routing-07-route-selection.cs`
- [ ] `routing-08-end-of-options.cs`
- [ ] `routing-09-complex-integration.cs`
- [ ] `routing-10-error-cases.cs`
- [ ] `routing-11-delegate-mediator.cs`
- [ ] `routing-12-colon-filtering.cs`
- [ ] `routing-13-negative-numbers.cs`
- [ ] `routing-14-option-order-independence.cs`
- [ ] `routing-15-help-route-priority.cs`
- [ ] `routing-16-typed-catch-all.cs`
- [ ] `routing-17-additional-primitive-types.cs`
- [ ] `routing-18-option-alias-with-description.cs`
- [ ] `routing-20-version-route-override.cs`
- [ ] `routing-21-check-updates-version-comparison.cs`
- [ ] `routing-22-async-task-int-return.cs`
- [ ] `routing-test-plan.md` (move only, not a test file)

## Migration Pattern

For each test file:
1. Add `#if !JARIBU_MULTI` / `return await RunAllTests();` / `#endif` block
2. Wrap types in namespace block (`TimeWarp.Nuru.Tests.Routing`)
3. Add `[ModuleInitializer]` registration method
4. Remove `[ClearRunfileCache]` attribute if present

## Checklist

- [ ] Migrate all 21 routing test files
- [ ] Move files to `tests/timewarp-nuru-core-tests/routing/`
- [ ] Move `routing-test-plan.md` to new location
- [ ] Update Directory.Build.props to include glob pattern
- [ ] Test standalone mode for each file
- [ ] Test multi-mode: `dotnet tests/ci-tests/run-ci-tests.cs`
- [ ] Commit changes

## Notes

- Current CI test count: 1024 tests
- Use namespace `TimeWarp.Nuru.Tests.Routing` for all files
- Note: routing-19 is missing from the sequence (file doesn't exist)
- Refer to `tests/timewarp-nuru-core-tests/parser/` for migration examples
