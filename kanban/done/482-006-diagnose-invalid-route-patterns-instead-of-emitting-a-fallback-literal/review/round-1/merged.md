# Round 1 — merged findings
**Date:** 2026-10-05
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: tests/ci-tests/run-ci-tests.cs:54
- Description: `generator-53-invalid-route-patterns.cs` is in `CiTestExcludes` (`tests/ci-tests/Directory.Build.props:89-92`) with the comment "Run standalone instead", but `standaloneTests` still ends at `generator-52-configuration-detection.cs`. The header comment still says `generator-28..52`. The second phase is the only place Roslyn-hosted generator tests run. A green `dotnet run tests/ci-tests/run-ci-tests.cs` does not compile or execute these five tests.
- Suggestion: Append `generator-53-invalid-route-patterns.cs` to `standaloneTests` and extend the `generator-28..52` comment, same as the other excluded generator files.
- Source: general
- Disposition notes: `generator-53-invalid-route-patterns.cs` appended to `standaloneTests`. Header comment now says `generator-28..53`.

### M2 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/diagnostics/diagnostic-descriptors.syntax.cs:92
- Description: `AdjacentParameters` has no `{0}` placeholder, and `MapParseErrorToDiagnostic` calls `Diagnostic.Create` with no message arguments (`dsl-interpreter.cs:875-876`). Roslyn 5.6 `GetMessage` returns the format string unchanged when there are no arguments, so `{{` is not unescaped. The compiler shows `e.g., '{{a}} {{b}}' rather than '{{a}}{{b}}'` instead of `{a} {b}` / `{a}{b}`. Reproduced with `Microsoft.CodeAnalysis.CSharp` 5.6.0: no-arg `GetMessage` kept the doubled braces; the same text passed as `{0}` and `{1}` rendered single braces. `NURU_P008` and `NURU_P009` format correctly. `generator-53` only checks that the message is non-whitespace.
- Suggestion: Pass the two examples as real format arguments (`'{0}'` / `'{1}'` with `"{a} {b}"` and `"{a}{b}"`), and assert that rendered text in the adjacent-parameter test.
- Source: general
- Disposition notes: Message format is `'{0}'` / `'{1}'` with arguments `"{a} {b}"` and `"{a}{b}"`. `generator-53` asserts the rendered fragment. Standalone run: 5 passed.

## Duplicates / conflicts

- One general reviewer. No overlaps. Both claims were re-checked against the tree and, for M2, against Roslyn 5.6 `Diagnostic.GetMessage`.
