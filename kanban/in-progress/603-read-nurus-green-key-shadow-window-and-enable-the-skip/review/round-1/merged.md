# Round 1 — merged findings
**Date:** 2026-11-28
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: .github/workflows/workflow.yml:172
- Description: The `stage` job-level `if` uses `matrix.stage`. GitHub does not offer the `matrix` context in `jobs.<job_id>.if` (only `github`, `needs`, `vars`, `inputs`), so GitHub rejects the workflow file. No job runs, the required `ci` check is never reported, and master produces no `Packages-*`. The test pins the invalid expression. The recorded gates never parse the workflow.
- Suggestion: Choose the legs in `strategy.matrix` from `needs` (pack-only gives `["build"]`, otherwise all four). Keep only the `!= 'skip'` gate in the job `if`. Update the test pin. Record a workflow-parse check in `### How to validate`.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: .github/workflows/workflow.yml:165; task.md `## Results` subtraction rule
- Description: On a push, the stage matrix now waits for the `green-key` job, so every push pays that job's wall time. The minutes-saved rule subtracts only `ci-finish-seconds`, so the tally will overstate the saving.
- Suggestion: Count the `green-key` job wall on every push run as cost in the tally rule. Optionally log its seconds the way `ci-finish-seconds` is logged.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None.
