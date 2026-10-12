# Round 2 — merged findings
**Date:** 2026-10-12
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: tests/ci-tests/run-ci-tests.cs:25
- Description: generator-54 was excluded from the multi-mode assembly and missing from the standalone runner.
- Suggestion: Append it to `standaloneTests` and extend the comment to `generator-28..54`.
- Source: general
- Disposition notes: Re-verified in round 2. The array entry and the comment match `CiTestExcludes`.

## Duplicates / conflicts

- Prior M1 carried forward. No new findings.
