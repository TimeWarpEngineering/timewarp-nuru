# Normalize Windows CRLF clipboard paste in REPL

Parent: 470 (2026-09-04 full-repo review). Severity: bug (M5). Nit folded: M36.

## Description

Paste concatenates clipboard text into `UserInput` and advances `CursorPosition` by raw `clipboardText.Length` (`repl-console-reader.selection.cs:231-233`). On Windows, `Get-Clipboard` returns CRLF. `MultilineBuffer.SetText` splits on `\r\n` (`multiline-buffer.cs:116`) so display lines are right, but `UserInput` still contains `\r` and cursor math counts each `\r` while the multiline linear domain counts one char per break.

Distinct from fixed 454-007 (GetFullText / SyncFromMultilineBuffer `\n` contract).

M36: public `InsertText` treats every `\r` and every `\n` as a separate `AddLine()`, so `\r\n` inserts a blank line. `SetText` already splits correctly.

## Requirements

- Normalize clipboard newlines to `\n` before updating `UserInput`/`CursorPosition`.
- Set cursor from the multiline linear domain; sync `UserInput` after paste.
- Make `InsertText` match `SetText` newline splitting (M36).
- Tests (TestTerminal / buffer unit tests). Windows human check can batch with 454-019.

## Checklist

- [ ] Paste newline normalization
- [ ] InsertText \r\n (M36)
- [ ] Tests

## Notes

Evidence: parent 470 `review/round-1/merged.md` M5, M36.
