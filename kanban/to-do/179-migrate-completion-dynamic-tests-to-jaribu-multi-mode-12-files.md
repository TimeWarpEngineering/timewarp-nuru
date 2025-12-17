# Migrate Completion Dynamic Tests to Jaribu Multi-Mode (12 Files)

## Description

Migrate the 12 remaining completion dynamic test files in `tests/timewarp-nuru-completion-tests/dynamic/` to support Jaribu multi-mode pattern. Currently only `completion-26-enum-partial-filtering.cs` is included in CI tests.

## Files to Migrate

- [ ] `completion-14-dynamic-handler.cs`
- [ ] `completion-15-completion-registry.cs`
- [ ] `completion-16-default-source.cs`
- [ ] `completion-17-enum-source.cs`
- [ ] `completion-18-parameter-detection.cs`
- [ ] `completion-19-endpoint-matching.cs`
- [ ] `completion-20-dynamic-script-gen.cs`
- [ ] `completion-21-integration-enabledynamic.cs`
- [ ] `completion-22-callback-protocol.cs`
- [ ] `completion-23-custom-sources.cs`
- [ ] `completion-24-context-aware.cs`
- [ ] `completion-25-output-format.cs`

Note: `completion-26-enum-partial-filtering.cs` is already migrated.

## Migration Pattern

For each test file:
1. Add `#if !JARIBU_MULTI` / `return await RunAllTests();` / `#endif` block at top
2. Wrap types in namespace `TimeWarp.Nuru.Tests.Completion.Dynamic`
3. Add `[ModuleInitializer]` registration method to each test class
4. Remove `[ClearRunfileCache]` attribute if present

## Checklist

- [ ] Migrate all 12 completion dynamic test files with Jaribu multi-mode pattern
- [ ] Update ci-tests/Directory.Build.props to include completion dynamic tests glob pattern
- [ ] Test standalone mode for each file
- [ ] Test multi-mode: `dotnet tests/ci-tests/run-ci-tests.cs`
- [ ] Commit changes

## Notes

- Tests stay in `timewarp-nuru-completion-tests/dynamic/` (not moved)
- Use namespace `TimeWarp.Nuru.Tests.Completion.Dynamic` for all files
- Project reference to timewarp-nuru-completion already exists in ci-tests
