# Migrate completion engine tests to Jaribu multi-mode (3 files)

## Description

Migrate 3 completion engine tests to support Jaribu multi-mode pattern, enabling them to run both standalone and as part of the CI test suite.

## Files

- `tests/timewarp-nuru-completion-tests/engine/engine-01-input-tokenizer.cs`
- `tests/timewarp-nuru-completion-tests/engine/engine-02-route-match-engine.cs`
- `tests/timewarp-nuru-completion-tests/engine/engine-03-candidate-generator.cs`

## Checklist

- [ ] Add `#if !JARIBU_MULTI` / `return await RunAllTests();` / `#endif` block
- [ ] Remove `[ClearRunfileCache]` attribute
- [ ] Add `[ModuleInitializer]` registration method to each test class
- [ ] Add namespace to avoid type conflicts (e.g., `TimeWarp.Nuru.Tests.Completion.Engine`)
- [ ] Remove explicit `using TimeWarp.Nuru;` and `using Shouldly;` (global usings)
- [ ] Update `tests/ci-tests/Directory.Build.props` to include engine tests
- [ ] Test standalone mode for each file
- [ ] Test CI multi-mode
- [ ] Commit changes

## Notes

These tests use standard Jaribu format and should migrate cleanly. Follow the same pattern used for static and dynamic completion tests.
