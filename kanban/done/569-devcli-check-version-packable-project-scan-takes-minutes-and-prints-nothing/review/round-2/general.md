# Round 2 — general
**Date:** 2026-11-09
**Scope reviewed:** fix delta 93650965..9fff2abb (product files: packable-project-service.cs and five devcli test files)

## Summary

The fix pass addresses all three round-1 findings and adds nothing else to the product code.
`PackableProjectService` now takes an `ITerminal` in its constructor and writes the progress line
through it. The Nuru generator resolves that dependency as `__fw_ITerminal` in both `tools/dev-cli/dev.cs`
(rebuilt during this review) and `tests/devcli-consumer-check`, the same way it already resolves
`RepoCleanService`. The parse fixture constants are renamed to `Stdout` and the IDE1006 pragma is gone.
The multi-failure `InvalidOperationException` carries an inner `AggregateException`. The tests check
both the progress line on `TestTerminal` and the inner exceptions. No new issues.

## Verification

- `packable-projects-01-parse` 21/21, `-02-derivation-fixture` 4/4, `-03-parallel` 8/8,
  `check-version-04-endpoint-zero-package` 1/1, `check-version-07-endpoint-fail-closed` 11/11 (all run standalone, warnings as errors).
- `dotnet build tools/dev-cli/dev.cs` succeeded; generated resolver is `new global::DevCli.PackableProjectService(__fw_ITerminal)`.
- No `#pragma` or lowercase `const string stdout` left in `tests/timewarp-nuru-tests/devcli/packable-projects-0*.cs`.
- Every `new PackableProjectService(...)` call site in the repo passes a terminal; the DI sites
  (`tools/dev-cli/dev.cs`, `tests/devcli-consumer-check/program.cs`, readme sample) use `AddSingleton<IPackableProjectService, PackableProjectService>()`, which the generator resolves.

## Prior findings

- M1 (bug) — fixed: constants renamed, pragma and its comment removed.
- M2 (suggestion) — fixed: injected `ITerminal`; fixture asserts `Evaluating 3 projects…`, `Evaluating 1 project…`, and no line when `source/` is absent.
- M3 (nit) — fixed: `new AggregateException(failures)` as inner exception; `-03-parallel` asserts two inner exceptions naming both paths.

## Issues

None.
