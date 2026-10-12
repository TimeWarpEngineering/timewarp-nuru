# Round 1 — general
**Date:** 2026-10-12
**Scope reviewed:** commit `573541d4` (handler-validator containment, generator-54, CiTestExcludes, changelog) and the standalone CI list in `tests/ci-tests/run-ci-tests.cs`.

## Summary

`DetectClosures` and `DetectAnonymousMethodClosures` now treat an `ILocalSymbol` or `IParameterSymbol` as a capture only when `IsDeclaredWithin` fails. Declaring-syntax span containment is the right check for nested lambda parameters, handler locals, and pattern variables, and it still reports an outer local used only inside a nested lambda. generator-54 encodes those three cases, and its positive assertion requires the diagnostic argument to be exactly `offset` (the period in `variable(s): offset.` does not match a list that also names `i`). The regression test is then left out of the runner that is supposed to execute every Roslyn-hosted file excluded from the multi-mode assembly.

## Issues

### Issue 1 — Severity: bug
- File: tests/ci-tests/run-ci-tests.cs:25
- Description: `Directory.Build.props` adds generator-54 to `CiTestExcludes` and says to run it standalone, matching generator-28..53. `run-ci-tests.cs` is the second phase that actually executes those files, and its `standaloneTests` array still ends at generator-53. The comment above the array still says `generator-28..53`. `dotnet run tests/ci-tests/run-ci-tests.cs` therefore exits without compiling or running the new regression, so a green CI run does not cover the NURU_H002 fix.
- Suggestion: Append `generator-54-h002-nested-lambda-parameter.cs` to `standaloneTests` and extend the comment to `generator-28..54`.
- Status: open
