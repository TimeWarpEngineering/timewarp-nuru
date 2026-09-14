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

- [x] RedrawLine clears all occupied rows
- [x] UpdateCursorPosition supports positions beyond one row
- [x] Unit-test the row/col math (TestTerminal); human check for visuals

## Verification protocol (reviewer, 2026-07-07)

Implement now with TestTerminal-based unit coverage of the state/logic layer; do NOT
block on interactive verification. Interactive confirmation is batched into ONE human
REPL verification session tracked on parent task 454 (together with 454-007's pending
Windows multiline check). Leave a "Human verification pending" line in this task's
Results listing exactly what the human should try.

## Session

- Implementer: grok session 01a0a068-79ac-7531-9999-0ae9c614eeb9 (2026-09-14)

## Results

Single-line REPL redraw now clears every terminal row the previous display occupied and
maps the logical cursor onto wrapped `(column, row)` coordinates. The old one-row blank
plus `desiredLeft >= WindowWidth` skip is gone, so a command longer than the window no
longer leaves stale wrap remnants or strands the cursor in the TestTerminal logic layer.

**Files**

- `source/timewarp-nuru/repl/input/wrapped-line-layout.cs` — row/col math
- `source/timewarp-nuru/repl/input/repl-console-reader.cs` — `RedrawLine` / `UpdateCursorPosition`
- `source/timewarp-nuru/repl/input/repl-console-reader.selection.cs` — same clear path for selection redraw
- `source/timewarp-nuru/repl/input/repl-console-reader.basic-editing.cs` — Ctrl+L reset of wrap state
- `tests/timewarp-nuru-tests/repl/repl-43-wrapped-line-redraw.cs` — 13 tests
- `source/timewarp-nuru/internals-visible-to.g.cs` — standalone test friend

**Decisions**

- Extracted `WrappedLineLayout` (internal) so occupancy, cursor map, start-row, and
  rows-to-clear are unit-tested without a real TTY. Visible length is
  `prompt.Length + input.Length` (ANSI highlight codes do not count).
- Track `InputStartRow`, `LastCursorVisualIndex`, and `LastDrawnDisplayLength` on the
  reader. Clear uses `max(previous, next)` occupied rows so shrinking a wrapped line
  still blanks the vacated row.
- `UpdateCursorPosition` always `SetCursorPosition`s, including when the visual index
  is an exact multiple of `WindowWidth` (cursor on column 0 of the next row).
- `RedrawLineWithSelection` shares the multi-row clear. Multiline per-logical-line wrap
  and i-search redraw are the same class of one-row clear but are outside this task's
  reported single-line `RedrawLine` / `UpdateCursorPosition` surface.

**Tests** — `repl-43-wrapped-line-redraw.cs`: **13/13 passed**. Related regressions
green: repl-05 (10), repl-15 including narrow window (8), repl-21 (3), repl-28 (19),
repl-32 (9).

**Human verification pending** (batch on parent 454 with 454-007's Windows multiline
check). In a real TTY, not TestTerminal:

1. `dotnet run samples/fluent/10-repl/fluent-repl-basic.cs` (or any `AddRepl()` sample)
   and shrink the window to ~40 columns.
2. Type a command longer than the width until it wraps onto a second row.
3. Move with Home / End / Left / Right across the wrap; the cursor must sit on the
   correct row and column, not freeze at the right edge.
4. Backspace until the line fits on one row; the second row must go blank (no leftover
   characters).
5. Type past the wrap again, press Escape; both rows of input must clear and the cursor
   must return to just after the prompt.
6. Enter a long wrapped command and confirm it still executes.

### How to validate

**Smoke**

```bash
dotnet run tests/timewarp-nuru-tests/repl/repl-43-wrapped-line-redraw.cs
```

**Expect**

- Grand total **13 passed / 0 failed**.
- `Cursor_at_exact_window_width_is_positioned_not_skipped` asserts cursor `(0, 1)` when
  prompt-plus-input length equals `WindowWidth` (the old skip path).
- `Home_after_wrap_returns_cursor_to_prompt_row` asserts `(2, 0)` after wrapping then Home.
- `Shrinking_below_window_width_moves_cursor_back_to_the_prompt_row` asserts row 0 after
  backspacing a wrapped line down to one row.

**Automated gate**

```bash
dotnet run tests/timewarp-nuru-tests/repl/repl-43-wrapped-line-redraw.cs
# expect: 13 passed
```

**Not in scope:** live TTY look-and-feel (see Human verification pending). Multiline
logical lines that themselves wrap, and Ctrl+R search-line redraw.
