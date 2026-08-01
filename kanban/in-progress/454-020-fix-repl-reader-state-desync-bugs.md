# Fix REPL Reader State Desync Bugs

Parent: 454 (2026-07-06 full code review). Severity: MEDIUM (M18, M19, M20).

## Description

Three related buffer/screen/state desync bugs in the REPL console reader partials
(`source/timewarp-nuru/repl/input/`):

- **M18 yank-arg** (`repl-console-reader.yank-arg.cs:59-63,103-106`): on a consecutive
  Alt+. that finds no older args-bearing history entry, the previously-yanked text has
  already been deleted (line ~61) but the fall-through reset path returns WITHOUT
  RedrawLine() and without re-inserting — the yanked argument vanishes from UserInput
  while still shown on screen. Silent text loss + desync.
- **M19 incremental search** (`repl-console-reader.search.cs:162-164,200,217`): extending
  the pattern (`SearchPattern += keyChar`) calls FindNextMatch starting at
  `SearchMatchIndex ± 1` without first re-testing whether the CURRENT match still
  satisfies the longer pattern. "gi" matching "git status", then typing "t", jumps to an
  older entry (or "no match") even though the current line still matches. Diverges from
  readline.
- **M20 selection clamp** (`repl-console-reader.selection.cs:194,226,253`): cut/paste/
  delete slice `UserInput[end..]` with unclamped `end = SelectionState.End`, while
  `GetSelectedText` (:62) clamps internally and the render path guards with
  `IsValidFor`/`Math.Min`. A stale selection whose End outruns a now-shorter UserInput
  passes the IsNullOrEmpty guard then throws ArgumentOutOfRangeException on the slice.

## Checklist

- [ ] M18: reset path re-inserts or redraws; no silent loss
- [ ] M19: re-test current match before advancing
- [ ] M20: clamp Selection End in all mutation paths (share one helper with GetSelectedText)
- [ ] Unit tests for each via the reader/state classes

## Verification protocol (reviewer, 2026-07-07)

Implement now with TestTerminal-based unit coverage of the state/logic layer; do NOT
block on interactive verification. Interactive confirmation is batched into ONE human
REPL verification session tracked on parent task 454 (together with 454-007's pending
Windows multiline check). Leave a "Human verification pending" line in this task's
Results listing exactly what the human should try.
