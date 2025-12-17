# Migrate Remaining timewarp-nuru-tests to Jaribu Multi-Mode (6 Files)

## Description

Migrate the remaining 6 test files in `tests/timewarp-nuru-tests/` to support Jaribu multi-mode pattern. This completes the consolidation of all tests into the multi-mode test framework.

## Files to Migrate

### Configuration (1 file)
- [ ] `configuration/configuration-01-validate-on-start.cs`

### Factory (1 file)
- [ ] `factory/factory-01-static-methods.cs`

### Options (3 files)
- [ ] `options/test-mixed-required-optional.cs`
- [ ] `options/test-optional-flag-optional-value.cs`
- [ ] `options/nuru-context-non-implemented-feature/test-nurucontext.cs`

### Type Conversion (1 file)
- [ ] `type-conversion/type-conversion-01-builtin-types.cs`

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

- [ ] Migrate all 6 test files with Jaribu multi-mode pattern
- [ ] Move files to timewarp-nuru-core-tests subdirectories
- [ ] Update Directory.Build.props to include new glob patterns
- [ ] Test standalone mode for each file
- [ ] Test multi-mode: `dotnet tests/ci-tests/run-ci-tests.cs`
- [ ] Delete empty directories from timewarp-nuru-tests/
- [ ] Commit changes

## Notes

- Use namespaces matching subdirectory: `TimeWarp.Nuru.Tests.Configuration`, `TimeWarp.Nuru.Tests.Factory`, `TimeWarp.Nuru.Tests.Options`, `TimeWarp.Nuru.Tests.TypeConversion`
- The `nuru-context-non-implemented-feature` folder may need special handling (check if it's a placeholder for future work)
- After this migration, `timewarp-nuru-tests/` should only contain `Directory.Build.props` and `test-plan-overview.md`
- Refer to `tests/timewarp-nuru-core-tests/routing/` for migration examples
