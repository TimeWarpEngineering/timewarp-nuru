# Fix Windows CRLF Multiline Cursor Mapping

Parent: 454 (2026-07-06 full code review). Severity: HIGH (Windows only).

## Description

`source/timewarp-nuru/repl/input/multiline-buffer.cs` +
`repl-console-reader.multiline.cs` — the reader synced
`UserInput = MultilineInput.GetFullText(Environment.NewLine)` (2-char `\r\n` on
Windows) while `CursorToPosition` / `PositionToCursor` / `TotalLength` all count ONE
character per line break. With more than one line, `CursorPosition` no longer indexed
`UserInput` correctly: after Shift+Enter the cursor landed on the wrong column, and
`UserInput[..CursorPosition]` slices (kill-ring, word ops) could throw
`ArgumentOutOfRangeException`. Linux/macOS unaffected.

## Checklist

- [x] Unify newline handling: `GetFullText()` now joins with `'\n'` (matching the
      cursor math) and documents the 1-char-separator contract; the reader sync uses
      it. `GetFullText(string)` remains for display/output separators. `SetText`
      already accepted any newline style on input.
- [x] Purpose/Design context regions added to multiline-buffer.cs recording the
      linear-position contract
- [x] Unit tests: GetFullText joins with `\n` on every platform; TotalLength ==
      GetFullText().Length; CursorToPosition indexes line starts correctly;
      PositionToCursor round-trips; interior-slice does not throw
      (`Should_keep_linear_positions_consistent_with_full_text` in repl-31)
- [ ] Verify multiline REPL editing on Windows (human tester — interactive)

## Results

- Rendering was never affected (RedrawMultiline renders from `MultilineInput.Lines`,
  not UserInput), and `SetText` splits on `\r\n`/`\n`/`\r`, so the fix is contained to
  the join side: two lines changed plus documentation.
- Honest limitation: this Linux CI cannot reproduce the Windows failure
  (Environment.NewLine == "\n" here). The new tests instead PIN the one-char-separator
  contract on all platforms — reintroducing Environment.NewLine into GetFullText() or
  the reader sync now fails CI everywhere.
- Full CI: 1282 multi-mode tests, 1275 passed, 0 failed, 7 pre-existing skips + both
  standalone Roslyn phases 4/4, exit 0. repl-31 30/30, repl-32 9/9.

## Notes

Human verification on Windows still recommended (interactive Shift+Enter editing);
Claude cannot run interactive REPL sessions.

## Session

- Created: 2026-07-06 (full-repo review session)
- Implementation: 2026-07-06 (same session)
