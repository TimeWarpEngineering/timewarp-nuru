# Round 2 — merged findings
**Date:** 2026-11-28
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: .github/workflows/workflow.yml:184-219
- Description (round 1): the stage job `if` read `matrix`, so GitHub rejected the workflow file.
- Disposition notes: fixed in `2eb06261`. The job `if` keeps only the `!= 'skip'` gate. Pack-only selects `["build"]` in `strategy.matrix` (the `needs` context is allowed there). The test checks that the job `if` does not contain `matrix`. Results record actionlint 1.7.12 exit 0, and exit 1 on the old expression.

### M2 — Severity: suggestion — Status: fixed
- File: .github/workflows/workflow.yml:140-168; task.md `## Results`
- Description (round 1): the tally ignored the `green-key` job wall that every push now waits for.
- Disposition notes: fixed in `2eb06261`. The `green-key` job logs `green-key-v1 green-key-seconds=` on every push, cancelled runs excepted. The Results tally subtracts it on every push.

## New findings

- None.

## Duplicates / conflicts

- None.
