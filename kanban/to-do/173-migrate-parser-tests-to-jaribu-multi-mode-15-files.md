# Migrate Parser Tests To Jaribu Multi Mode (15 Files)

## Description

Migrate the 15 parser test files in `tests/timewarp-nuru-tests/parsing/parser/` to support Jaribu multi-mode pattern, enabling them to run in the consolidated CI test runner.

## Files to Migrate

- [ ] `parser-01-basic-parameters.cs`
- [ ] `parser-02-typed-parameters.cs`
- [ ] `parser-03-optional-parameters.cs`
- [ ] `parser-04-duplicate-parameters.cs`
- [ ] `parser-05-consecutive-optionals.cs`
- [ ] `parser-06-catchall-position.cs`
- [ ] `parser-07-catchall-optional-conflict.cs`
- [ ] `parser-08-option-modifiers.cs`
- [ ] `parser-09-end-of-options.cs`
- [ ] `parser-10-specificity-ranking.cs`
- [ ] `parser-11-complex-integration.cs`
- [ ] `parser-12-error-reporting.cs`
- [ ] `parser-13-syntax-errors.cs`
- [ ] `parser-14-mixed-modifiers.cs`
- [ ] `parser-15-custom-type-constraints.cs`

## Migration Pattern

For each test file:
1. Add `#if !JARIBU_MULTI` / `return await RunAllTests();` / `#endif` block
2. Wrap types in namespace block (e.g., `TimeWarp.Nuru.Tests.Parser`)
3. Add `[ModuleInitializer]` registration method
4. Remove `[ClearRunfileCache]` attribute
5. Remove explicit `using` statements that are already global

## Checklist

- [ ] Migrate all 15 parser test files
- [ ] Update Directory.Build.props to include glob pattern
- [ ] Test standalone mode for each file
- [ ] Test multi-mode: `dotnet tests/ci-tests/run-ci-tests.cs`
- [ ] Commit changes

## Notes

- Current CI test count before migration: 880 tests
- These tests use `[ClearRunfileCache]` attribute which should be removed
- Use namespace `TimeWarp.Nuru.Tests.Parser` for all files
- Refer to `tests/timewarp-nuru-core-tests/` for migration examples
