# Round 1 — merged findings
**Date:** 2026-09-14
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 1 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru/repl/input/tab-completion-handler.cs:187
- Description: `DisplayCandidates` writes a completion dump then prompt+input without updating `InputStartRow`, `LastCursorVisualIndex`, or `LastDrawnDisplayLength`. `UpdateCursorPosition` now places the cursor at `InputStartRow + rowOffset` instead of live `GetCursorPosition().top`, so after Alt+= the next Left/Right/Home/End jumps back to the pre-completion row. The same desync hits multi-candidate Tab when `RedrawLine` → `ClearOccupiedDisplayRows` derives `startRow` from a stale visual index while the real cursor sits on the newly written prompt line (wrong when that line’s row offset differs from the logical cursor’s). `TestTerminal.Write` / `WriteLine` do not advance `CursorTop`, so repl-43 cannot see this on the fake terminal.
- Suggestion: After an external dump that relocates the physical cursor, re-anchor wrap state from `GetCursorPosition()` and redraw (or `UpdateCursorPosition`) through the reader. Prefer dump-only candidate display plus reader `RedrawLine`, or resync assuming the physical cursor is at the end of the just-written prompt+input. Cover Alt+= then Home/Left so the cursor stays on the post-dump prompt row.
- Source: general
- Disposition notes: Made `DisplayCandidates` dump-only; reader adopts physical cursor as new single-line wrap anchor then `RedrawLine` after Alt+= / multi-candidate Tab dumps.

### M2 — Severity: suggestion — Status: wontfix
- File: source/timewarp-nuru/repl/input/repl-console-reader.search.cs:93
- Description: `ExitSearchMode` calls wrap-aware `RedrawLine` after `RedrawSearchLine` has moved the physical cursor. Reviewer suggested resetting wrap fields to the current top before that redraw.
- Suggestion: Re-sync wrap state before `RedrawLine` on search exit.
- Source: general
- Disposition notes: Search-line redraw is an explicit task deferral. For an in-scope (non-wrapping) search UI, live `currentTop` plus the pre-search `LastCursorVisualIndex` reconstructs `InputStartRow` and correctly clears every wrap row of the original line. Resetting the visual index to 0 at the current top would leave remnants on earlier wrap rows. When search redraw is wrap-fixed later, it should maintain the same wrap fields as single-line redraw. Decider: review orchestrator.

## Duplicates / conflicts

- M1 and M2 share the sticky wrap-state contract, but M2’s suggested reset is the wrong transition for search exit. Kept separate; only M1 is fixed.
