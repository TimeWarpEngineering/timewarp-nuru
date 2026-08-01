# Fix Redraw Of Lines Longer Than Terminal Width

Parent: 454 (2026-07-06 full code review). Severity: MEDIUM (M17).

## Description

`source/timewarp-nuru/repl/input/repl-console-reader.cs:279,310` — `RedrawLine` clears
exactly one row (`new string(' ', Terminal.WindowWidth)`) and `UpdateCursorPosition`
skips positioning entirely when `desiredLeft >= WindowWidth`. A command longer than the
terminal width leaves stale wrapped characters on screen and strands the cursor — editing
long single-line commands is visually broken.

## Requirements

- Handle wrapped lines: clear all rows the previous content occupied, and map logical
  cursor position to (row, col) across wraps.

## Checklist

- [ ] RedrawLine clears all occupied rows
- [ ] UpdateCursorPosition supports positions beyond one row
- [ ] Unit-test the row/col math (TestTerminal); human check for visuals

## Verification protocol (reviewer, 2026-07-07)

Implement now with TestTerminal-based unit coverage of the state/logic layer; do NOT
block on interactive verification. Interactive confirmation is batched into ONE human
REPL verification session tracked on parent task 454 (together with 454-007's pending
Windows multiline check). Leave a "Human verification pending" line in this task's
Results listing exactly what the human should try.
