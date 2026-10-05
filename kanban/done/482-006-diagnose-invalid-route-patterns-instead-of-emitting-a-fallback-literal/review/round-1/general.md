# Round 1 — general
**Date:** 2026-10-05
**Scope reviewed:** commit 50e2787f (task 482-006) vs its parent

## Summary

`NURU_P008`, `NURU_P009`, and `NURU_P010` are Error / `RoutePattern.Syntax` descriptors, match `AnalyzerReleases.Unshipped.md`, and are mapped from `InvalidIdentifierError`, `InvalidModifierCombinationError`, and `AdjacentParametersError`. A failed parse returns no segments, and both app `Map` and group `Map` pass `parseResult.Success` so `IrRouteBuilder.Done()` does not register the route; valid patterns still come back from `ExtractSegments` as real segments. `generator-53` asserts the three ids plus the existing unbalanced-brace and bad-option cases. Two defects: that test is excluded from the CI multi assembly and is not on the standalone list, and the adjacent-parameter message is shown with doubled braces.

## Issues

### Issue 1 — Severity: bug
- File: tests/ci-tests/run-ci-tests.cs:54
- Description: `generator-53-invalid-route-patterns.cs` is in `CiTestExcludes` (`tests/ci-tests/Directory.Build.props:89-92`) with the comment "Run standalone instead", but `standaloneTests` still ends at `generator-52-configuration-detection.cs` (the header comment on line 25 still says `generator-28..52`). The second phase is the only place Roslyn-hosted generator tests run. A green `dotnet run tests/ci-tests/run-ci-tests.cs` does not compile or execute these five tests.
- Suggestion: Append `generator-53-invalid-route-patterns.cs` to `standaloneTests` and extend the `generator-28..52` comment, same as the other excluded generator files.
- Status: open

### Issue 2 — Severity: bug
- File: source/timewarp-nuru-analyzers/diagnostics/diagnostic-descriptors.syntax.cs:92
- Description: `AdjacentParameters` has no `{0}` placeholder, and `MapParseErrorToDiagnostic` calls `Diagnostic.Create` with no message arguments (`dsl-interpreter.cs:875-876`). Roslyn 5.6 `GetMessage` returns the format string unchanged when there are no arguments, so `{{` is not unescaped. The compiler shows `e.g., '{{a}} {{b}}' rather than '{{a}}{{b}}'` instead of `{a} {b}` / `{a}{b}`. `Diagnostic.Create` does not throw. `NURU_P008` and `NURU_P009` format correctly. `generator-53` only checks that the message is non-whitespace, so it does not catch this.
- Suggestion: Pass the two examples as real format arguments (for example `'{0}' rather than '{1}'` with `"{a} {b}"` and `"{a}{b}"`), and assert that rendered text in the adjacent-parameter test.
- Status: open
