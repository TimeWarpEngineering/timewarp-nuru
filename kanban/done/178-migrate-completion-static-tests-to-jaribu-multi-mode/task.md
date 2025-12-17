# Migrate Completion Static Tests to Jaribu Multi-Mode (12 Files)

## Description

Migrate the 12 remaining completion static test files in `tests/timewarp-nuru-completion-tests/static/` to support Jaribu multi-mode pattern. Currently only `completion-11-enum-completion.cs` is included in CI tests.

## Files to Migrate

- [ ] `completion-01-command-extraction.cs`
- [ ] `completion-02-option-extraction.cs`
- [ ] `completion-03-parameter-type-detection.cs`
- [ ] `completion-04-cursor-context.cs`
- [ ] `completion-05-bash-script-generation.cs`
- [ ] `completion-06-zsh-script-generation.cs`
- [ ] `completion-07-powershell-script-generation.cs`
- [ ] `completion-08-fish-script-generation.cs`
- [ ] `completion-09-integration-enablecompletion.cs`
- [ ] `completion-10-route-analysis.cs`
- [ ] `completion-12-edge-cases.cs`
- [ ] `completion-13-template-loading.cs`

Note: `completion-11-enum-completion.cs` is already migrated.

## Migration Pattern

For each test file:
1. Add `#if !JARIBU_MULTI` / `return await RunAllTests();` / `#endif` block at top
2. Wrap types in namespace `TimeWarp.Nuru.Tests.Completion.Static`
3. Add `[ModuleInitializer]` registration method to each test class
4. Remove `[ClearRunfileCache]` attribute if present

## Checklist

- [ ] Migrate all 12 completion static test files with Jaribu multi-mode pattern
- [ ] Update ci-tests/Directory.Build.props to include completion static tests glob pattern
- [ ] Test standalone mode for each file
- [ ] Test multi-mode: `dotnet tests/ci-tests/run-ci-tests.cs`
- [ ] Commit changes

## Notes

- Tests stay in `timewarp-nuru-completion-tests/static/` (not moved)
- Use namespace `TimeWarp.Nuru.Tests.Completion.Static` for all files
- Project reference to timewarp-nuru-completion already exists in ci-tests
