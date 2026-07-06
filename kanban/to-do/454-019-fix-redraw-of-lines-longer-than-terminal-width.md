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
