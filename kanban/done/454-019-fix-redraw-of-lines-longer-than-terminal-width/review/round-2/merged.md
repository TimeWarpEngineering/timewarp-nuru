# Round 2 — merged findings
**Date:** 2026-09-14
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 1 |
| nit | 0 | 0 | 0 |

## Prior IDs

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru/repl/input/tab-completion-handler.cs:167
- Description: Completion dumps rewrote prompt+input without updating wrap fields, so post-dump cursor motion targeted the pre-dump row.
- Source: general (round 1)
- Disposition notes: `DisplayCandidates` is dump-only. The reader adopt-anchors from the physical cursor then `RedrawLine` after Alt+= and first multi-candidate Tab. Single-candidate and cycling Tab do not adopt. Round 2 re-verified; still fixed.

### M2 — Severity: suggestion — Status: wontfix
- File: source/timewarp-nuru/repl/input/repl-console-reader.search.cs:93
- Description: Suggested resetting wrap fields before `RedrawLine` on search exit.
- Source: general (round 1)
- Disposition notes: Unchanged. For in-scope non-wrapping search UI, live `currentTop` plus pre-search `LastCursorVisualIndex` reconstructs `InputStartRow`. Adopt-at-current-top would leave remnants on earlier wrap rows. Search-line wrap remains out of scope. Round 2: wontfix stands. Decider: review orchestrator.

## Issues

No new issues.

## Duplicates / conflicts

None. Round 2 carried M1/M2 only.
