# Migrate Analyzer Tests to Jaribu Multi-Mode (3 Files)

## Description

Migrate the 3 analyzer test files in `tests/timewarp-nuru-analyzers-tests/auto/` to support Jaribu multi-mode pattern.

## Files to Migrate

- [ ] `nuru-invoker-generator-01-basic.cs`
- [ ] `delegate-signature-01-models.cs`
- [ ] `endpoint-generator-01-basic.cs`

## Migration Pattern

For each test file:
1. Add `#if !JARIBU_MULTI` / `return await RunAllTests();` / `#endif` block at top
2. Wrap types in namespace `TimeWarp.Nuru.Tests.Analyzers`
3. Add `[ModuleInitializer]` registration method to each test class
4. Remove `[ClearRunfileCache]` attribute if present

## Checklist

- [ ] Migrate all 3 analyzer test files with Jaribu multi-mode pattern
- [ ] Update ci-tests/Directory.Build.props to include analyzer tests glob pattern
- [ ] Add project reference to timewarp-nuru-analyzers if needed
- [ ] Test standalone mode for each file
- [ ] Test multi-mode: `dotnet tests/ci-tests/run-ci-tests.cs`
- [ ] Commit changes

## Notes

- Analyzer tests may require special handling due to source generator dependencies
- Tests stay in `timewarp-nuru-analyzers-tests/auto/` (not moved)
- Use namespace `TimeWarp.Nuru.Tests.Analyzers` for all files
- May need to check if analyzers can run in multi-mode without conflicts
