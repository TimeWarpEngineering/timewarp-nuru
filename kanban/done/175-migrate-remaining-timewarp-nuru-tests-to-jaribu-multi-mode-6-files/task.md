# Migrate Remaining timewarp-nuru-tests to Jaribu Multi-Mode (6 Files)

## Description

Migrate the remaining 6 test files in `tests/timewarp-nuru-tests/` to support Jaribu multi-mode pattern. This completes the consolidation of all tests into the multi-mode test framework.

## Files to Migrate

### Configuration (1 file)
- [x] `configuration/configuration-01-validate-on-start.cs`

### Factory (1 file)
- [x] `factory/factory-01-static-methods.cs` (standalone only - Mediator.SourceGenerator conflict)

### Options (3 files)
- [x] `options/test-mixed-required-optional.cs` → renamed to `options-01-mixed-required-optional.cs`
- [x] `options/test-optional-flag-optional-value.cs` → renamed to `options-02-optional-flag-optional-value.cs`
- [x] `options/nuru-context-non-implemented-feature/test-nurucontext.cs` → renamed to `options-03-nuru-context.cs` (standalone only - NuruContext not implemented)

### Type Conversion (1 file)
- [x] `type-conversion/type-conversion-01-builtin-types.cs`

## Migration Pattern

For each test file:
1. Add `#if !JARIBU_MULTI` / `return await RunAllTests();` / `#endif` block at top
2. Wrap types in appropriate namespace (e.g., `TimeWarp.Nuru.Tests.Configuration`)
3. Add `[ModuleInitializer]` registration method to each test class
4. Remove `[ClearRunfileCache]` attribute if present
5. Move file to corresponding location in `tests/timewarp-nuru-core-tests/`

## Destination Locations

- `configuration/*.cs` → `timewarp-nuru-core-tests/configuration/`
- `factory/*.cs` → `timewarp-nuru-core-tests/factory/`
- `options/*.cs` → `timewarp-nuru-core-tests/options/`
- `type-conversion/*.cs` → `timewarp-nuru-core-tests/type-conversion/`

## Checklist

- [x] Migrate all 6 test files with Jaribu multi-mode pattern
- [x] Move files to timewarp-nuru-core-tests subdirectories
- [x] Update Directory.Build.props to include new glob patterns
- [x] Test standalone mode for each file
- [x] Test multi-mode: `dotnet tests/ci-tests/run-ci-tests.cs`
- [x] Delete empty directories from timewarp-nuru-tests/
- [ ] Commit changes

## Notes

- Use namespaces matching subdirectory: `TimeWarp.Nuru.Tests.Configuration`, `TimeWarp.Nuru.Tests.Factory`, `TimeWarp.Nuru.Tests.Options`, `TimeWarp.Nuru.Tests.TypeConversion`
- Options tests were renamed to follow naming convention (options-01, options-02, options-03)
- Factory tests excluded from multi-mode due to Mediator.SourceGenerator conflicts
- Options-03 (NuruContext) excluded from multi-mode as NuruContext is not yet implemented
- Configuration tests have 4 pre-existing failures (test logic issues, not migration)
- CI test count increased from 1192 to 1218 (added 26 tests from migrated files)
- After this migration, `timewarp-nuru-tests/` only contains `Directory.Build.props` and `test-plan-overview.md`
