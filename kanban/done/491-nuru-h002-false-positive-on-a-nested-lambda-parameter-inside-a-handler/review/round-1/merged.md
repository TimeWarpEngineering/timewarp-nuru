# Round 1 — merged findings
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
- Description: generator-54 is in `CiTestExcludes` (CS0433, same as the other Roslyn-hosted generator tests) but is not in the `standaloneTests` array. The second-phase comment still says `generator-28..53`. The official CI runner never executes the new NURU_H002 regression.
- Suggestion: Append `generator-54-h002-nested-lambda-parameter.cs` to `standaloneTests` and extend the comment to `generator-28..54`.
- Source: general
- Disposition notes: Fixed on this id. `standaloneTests` now ends with generator-54, and the comment says `generator-28..54`.

## Duplicates / conflicts

- Single reviewer. Nothing to collapse.
