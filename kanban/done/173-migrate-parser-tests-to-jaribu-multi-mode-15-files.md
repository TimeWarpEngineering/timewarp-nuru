# Migrate Parser Tests To Jaribu Multi Mode (15 Files)

## Description

Migrate the 15 parser test files in `tests/timewarp-nuru-tests/parsing/parser/` to support Jaribu multi-mode pattern, enabling them to run in the consolidated CI test runner.

## Files Migrated

- [x] `parser-01-basic-parameters.cs` (4 tests)
- [x] `parser-02-typed-parameters.cs` (6 tests)
- [x] `parser-03-optional-parameters.cs` (6 tests)
- [x] `parser-04-duplicate-parameters.cs` (6 tests)
- [x] `parser-05-consecutive-optionals.cs` (7 tests)
- [x] `parser-06-catchall-position.cs` (7 tests)
- [x] `parser-07-catchall-optional-conflict.cs` (7 tests)
- [x] `parser-08-option-modifiers.cs` (17 tests)
- [x] `parser-09-end-of-options.cs` (12 tests)
- [x] `parser-10-specificity-ranking.cs` (10 tests)
- [x] `parser-11-complex-integration.cs` (7 tests)
- [x] `parser-12-error-reporting.cs` (30 tests)
- [x] `parser-13-syntax-errors.cs` (6 tests)
- [x] `parser-14-mixed-modifiers.cs` (10 tests)
- [x] `parser-15-custom-type-constraints.cs` (9 tests)

## Migration Pattern

For each test file:
1. Add `#if !JARIBU_MULTI` / `return await RunAllTests();` / `#endif` block
2. Wrap types in namespace block (`TimeWarp.Nuru.Tests.Parser`)
3. Add `[ModuleInitializer]` registration method
4. Remove `[ClearRunfileCache]` attribute
5. Remove explicit `using` statements that are already global

## Checklist

- [x] Migrate all 15 parser test files
- [x] Update Directory.Build.props to include glob pattern
- [x] Test standalone mode for each file
- [x] Test multi-mode: `dotnet tests/ci-tests/run-ci-tests.cs`
- [x] Commit changes

## Results

- **CI test count before:** 880 tests
- **CI test count after:** 1024 tests
- **Tests added:** 144 parser tests
- All parser tests pass in standalone mode
- All parser tests run in consolidated CI runner
- 8 pre-existing test failures unrelated to migration (KeyBindingProfile, InteractiveRouteExecution)
