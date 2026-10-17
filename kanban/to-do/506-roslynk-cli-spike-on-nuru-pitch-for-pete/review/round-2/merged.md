# Round 2 — merged findings
**Date:** 2026-10-18
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: kanban/to-do/506-roslynk-cli-spike-on-nuru-pitch-for-pete/task.md (Pitch, Cold versus warm); spikes/roslynk-cli/readme.md; spikes/roslynk-cli/timing/warm-pair.cs
- Description: Warm diagnostics time was a memo hit presented as the general warm cost.
- Source: general (round 1)
- Disposition notes: Fixed in `3cd65ee7`. The text names the memo hit and cites the `refs` 25.9 ms median as the uncached warm cost. Optional changed-key timing not run.

### M2 — Severity: suggestion — Status: fixed
- File: task.md (507 shape, bool row); spikes/roslynk-cli/schema/schema-report.cs
- Description: Six `bool` parameters default to `true` and were not flagged.
- Source: general (round 1)
- Disposition notes: Fixed in `3cd65ee7`. `schema-report` prints `parameters.boolDefaultTrue=6` (re-run in round 2), and the 507 paragraph gives the `bool?` rule.

### M3 — Severity: nit — Status: fixed
- File: task.md (Pitch)
- Description: Option rule and positional-lowercasing rule shared one parenthetical.
- Source: general (round 1)
- Disposition notes: Fixed in `3cd65ee7`. Two sentences now.

## Duplicates / conflicts

- None. No new findings in round 2.
