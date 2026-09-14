# Review framework — task 454-019

**Date:** 2026-09-14
**Host task:** kanban/in-progress/454-019-fix-redraw-of-lines-longer-than-terminal-width/
**Diff scope:** branch `task/454-019-019-fix-redraw-of-lines-longer-than-terminal-width` vs `origin/feature/overnight-nuru` (product commits `d3701b68` + kanban results `1366759e`). Uncommitted `.gitignore` memsearch ignore is out of scope.
**Plan / brief:** Parent 454 M17 — `RedrawLine` used to blank one row and `UpdateCursorPosition` skipped `SetCursorPosition` when `desiredLeft >= WindowWidth`. Implementation extracts `WrappedLineLayout` for wrap math, clears `max(previous, next)` occupied rows, and always maps the cursor, including exact `WindowWidth` wrap. TestTerminal coverage in `repl-43-wrapped-line-redraw.cs` (13 tests). Human TTY check is batched on parent 454; do not block on it.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle grok session 01a0a075-1128-7073-83cc-46b4ff2088a7 (2026-09-14)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
- Requirements: handle wrapped lines — clear all rows the previous content occupied, and map logical cursor position to (row, col) across wraps
- Out of scope per task: live TTY look-and-feel; multiline logical lines that themselves wrap; Ctrl+R search-line redraw

## Round 2

Re-review the M1 fix (dump-only `DisplayCandidates` + `AdoptPhysicalCursorAsNewSingleLineAnchor` before `RedrawLine` on Alt+= / multi-candidate Tab). Carry M1 (fixed) and M2 (wontfix). Scan the fix delta for new defects. Do not clobber `review/round-1/`.
