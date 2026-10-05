# Round 2 — general
**Date:** 2026-10-05
**Scope reviewed:** fix delta for M1 and M2 (task 482-006)

## Summary

The uncommitted fix covers both round-1 bugs and does not add a new defect. `generator-53-invalid-route-patterns.cs` is now the last entry in `standaloneTests`, and the header comment says `generator-28..53`. It remains in `CiTestExcludes`, which is correct for a Roslyn-hosted test. `NURU_P010` formats through real `{0}` and `{1}` placeholders, the adjacent-parameter arm passes `"{a} {b}"` and `"{a}{b}"`, and `generator-53` asserts the rendered single-brace fragment.

## Resolved prior

### M1 — Severity: bug — Status: fixed
- File: tests/ci-tests/run-ci-tests.cs:25, tests/ci-tests/run-ci-tests.cs:55, tests/ci-tests/Directory.Build.props:89-92
- Description: `standaloneTests` lists `generator-53-invalid-route-patterns.cs`, the header comment is `generator-28..53`, and `CiTestExcludes` still excludes that file.
- Status: fixed

### M2 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/diagnostics/diagnostic-descriptors.syntax.cs:92, source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs:875-876, tests/timewarp-nuru-tests/generator/generator-53-invalid-route-patterns.cs:90-97
- Description: `AdjacentParameters` messageFormat is `'{0}'` / `'{1}'`, `AdjacentParametersError` passes `"{a} {b}"` and `"{a}{b}"`, and the test asserts `(e.g., '{a} {b}' rather than '{a}{b}')`.
- Status: fixed
