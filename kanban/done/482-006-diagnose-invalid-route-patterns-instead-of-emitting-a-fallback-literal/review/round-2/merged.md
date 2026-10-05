# Round 2 — merged findings
**Date:** 2026-10-05
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Resolved prior

### M1 — Severity: bug — Status: fixed
- File: tests/ci-tests/run-ci-tests.cs:25, tests/ci-tests/run-ci-tests.cs:55
- Description: `standaloneTests` lists `generator-53-invalid-route-patterns.cs`. The header comment is `generator-28..53`. `CiTestExcludes` still excludes that file so the Roslyn-hosted test runs in the second phase.
- Suggestion: (round 1) Append the file to `standaloneTests`.
- Source: general
- Disposition notes: Fixed on this task before round 2. Re-verified.

### M2 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/diagnostics/diagnostic-descriptors.syntax.cs:92
- Description: `NURU_P010` messageFormat uses `'{0}'` and `'{1}'`. `AdjacentParametersError` passes `"{a} {b}"` and `"{a}{b}"`. `generator-53` asserts the rendered fragment `(e.g., '{a} {b}' rather than '{a}{b}')`.
- Suggestion: (round 1) Pass the examples as format arguments and assert the rendered text.
- Source: general
- Disposition notes: Fixed on this task before round 2. Standalone `generator-53`: 5 passed. Re-verified.

## Issues

None.

## Duplicates / conflicts

- Round 2 carried M1 and M2. No new findings. No overlaps.
