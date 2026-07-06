# Fix Windows CRLF Multiline Cursor Mapping

Parent: 454 (2026-07-06 full code review). Severity: HIGH (Windows only).

## Description

`source/timewarp-nuru/repl/input/multiline-buffer.cs:313,329,349` and
`source/timewarp-nuru/repl/input/repl-console-reader.multiline.cs:70-71` —
`GetFullText(Environment.NewLine)` joins buffer lines with `\r\n` (2 chars) on Windows,
but `CursorToPosition` / `PositionToCursor` / `TotalLength` all assume 1-char newlines
(`+1` per line break). With more than one line, `CursorPosition` no longer indexes
`UserInput` correctly.

Impact (Windows): after Shift+Enter the cursor lands on the wrong column, and a later
`UserInput[..CursorPosition]` slice can throw `ArgumentOutOfRangeException`.
Linux/macOS unaffected (`\n` is 1 char).

## Requirements

- Make the join separator and the cursor math agree. Simplest: always join with `"\n"`
  internally and only translate to Environment.NewLine at the terminal-write boundary;
  otherwise thread the separator length through the mapping functions.

## Checklist

- [ ] Unify newline handling between GetFullText and cursor mapping
- [ ] Unit tests for CursorToPosition/PositionToCursor/TotalLength with 2-char separator
- [ ] Verify multiline REPL editing on Windows (human tester — interactive)

## Notes

Claude cannot run interactive REPL tests; cursor-math unit tests can and should cover this.
