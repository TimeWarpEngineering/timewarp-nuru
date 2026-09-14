# Review framework — task 470-002

**Date:** 2026-09-14
**Host task:** kanban/in-progress/470-002-fix-repl-handlecharacter-unclamped-selection-crash/
**Diff scope:** branch `task/470-002-fix-repl-handlecharacter-unclamped-selection-crash` vs `origin/feature/overnight-nuru` (product `12a20c5a`, kanban results `7c2c4960`, chore `8fce1c1c`). Product surface is REPL selection clamp + ClearSelection on buffer-replacing commands + `repl-44` tests. `.gitignore` memsearch ignore is incidental to this task.
**Plan / brief:** Parent 470 M4 — `HandleCharacter` replaced an active selection with unclamped `SelectionState.Start`/`End`. History, kill-ring, and undo/redo replace or shorten `UserInput` without clearing selection. Repro: Shift+Left then Ctrl+K then type → `ArgumentOutOfRangeException`. Same crash via select-all then Up to a shorter history entry then type. 454-020 clamped cut/paste/delete (`GetClampedBounds`); this task clamps the character-insert path and clears selection on non-selection mutations. Dead `HandleCharacterWithOverwrite` deleted. TestTerminal coverage in `repl-44-stale-selection-character-insert.cs` (3 tests).
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle grok session 01a0a09d-54e4-7ae0-8f1d-c59cf3465e38 (2026-09-14)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
- Requirements: clamp `HandleCharacter` via `GetClampedBounds`; clear selection at the start of every path that replaces/shortens `UserInput` without going through selection handlers (history, kill, yank, undo/redo/revert, tab-completion apply); TestTerminal coverage of the repro; delete or fix dead `HandleCharacterWithOverwrite`
- Out of scope per task: 454-019 wrapped-line redraw; live TTY visual check of selection highlighting; word case/transpose (length-preserving)
