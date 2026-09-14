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

- [x] Paste newline normalization
- [x] InsertText \r\n (M36)
- [x] Tests

## Notes

Evidence: parent 470 `review/round-1/merged.md` M5, M36.

## Session

- Implementer: grok session 01a0a0ab-c78d-7230-90b6-c39767bf01cf (2026-09-14)

## Results

Clipboard paste no longer splices raw clipboard bytes into `UserInput` or advances `CursorPosition` by `clipboardText.Length`. `HandlePasteAsync` deletes an active selection (clamped), syncs into `MultilineBuffer`, `InsertText`s the clipboard (or kill-ring fallback), then `SyncFromMultilineBuffer()` so `UserInput` is `GetFullText()` (`\n` joins) and the cursor is `CursorToPosition`. `InsertText` shares `SetText`'s splitter (`\r\n`, `\n`, `\r`); a CRLF pair is one break, not a blank line (M36).

**Files**

- `source/timewarp-nuru/repl/input/multiline-buffer.cs` — shared `SplitLines`; `InsertText` splits then `AddLine`+segment instead of per-char `\r`/`\n`
- `source/timewarp-nuru/repl/input/repl-console-reader.selection.cs` — paste via multiline buffer + `SyncFromMultilineBuffer`
- `tests/timewarp-nuru-tests/repl/repl-31-multiline-buffer.cs` — InsertText CRLF / CR / mid-line / linear-domain tests
- `tests/timewarp-nuru-tests/repl/repl-45-crlf-clipboard-paste.cs` — TestTerminal paste path (cut/paste, cursor after paste, selection replace, multiline)

**Decisions**

- Normalize by inserting through the buffer rather than a string `Replace` on `UserInput`. `InsertText` is the public API that was wrong (M36); paste then adopts the existing 454-007 `\n` contract via `SyncFromMultilineBuffer`.
- Shared `SplitLines` so `SetText` and `InsertText` cannot drift.
- New `repl-45-*` file for the paste command path; CRLF splitting stays in `repl-31` (buffer unit tests). Does not touch `repl-43` (454-019).
- Windows TTY paste of `Get-Clipboard` CRLF is still a human check; batch with 454-019 as the brief allows.

**Tests** — `repl-31-multiline-buffer.cs`: **37/37 passed** (7 new InsertText cases). `repl-45-crlf-clipboard-paste.cs`: **4/4 passed**. Related green: repl-32 (9), repl-28 (19), repl-26 (17).

### How to validate

**Smoke**

```bash
dotnet run tests/timewarp-nuru-tests/repl/repl-31-multiline-buffer.cs
dotnet run tests/timewarp-nuru-tests/repl/repl-45-crlf-clipboard-paste.cs
```

**Expect**

- `repl-31` grand total **37 passed / 0 failed**. New cases: `Should_insert_crlf_as_one_break_not_blank_line` (LineCount 2, not 3), `Should_insert_trailing_crlf_without_extra_blank_line` (one empty line, not two), `Should_keep_linear_domain_in_sync_after_crlf_insert` (`GetFullText()` is `prefixa\nb\nc` with no `\r`, cursor at `TotalLength`).
- `repl-45` grand total **4 passed / 0 failed**. `Cut_then_paste_inserts_at_cursor` prints `PASTE-OK`; `Typing_after_paste_appends_at_end_of_pasted_text` prints `PASTE-CURSOR-OK` (`echo hel` + typed `lo`); `Paste_replaces_an_active_selection` prints `PASTE-REPLACE-OK`; `Multiline_cut_then_paste_executes_as_one_command` prints `PASTE-MULTILINE-OK`.

**Automated gate**

```bash
dotnet run tests/timewarp-nuru-tests/repl/repl-31-multiline-buffer.cs
dotnet run tests/timewarp-nuru-tests/repl/repl-45-crlf-clipboard-paste.cs
dotnet run tests/timewarp-nuru-tests/repl/repl-32-multiline-editing.cs
dotnet run tests/timewarp-nuru-tests/repl/repl-28-text-selection.cs
dotnet run tests/timewarp-nuru-tests/repl/repl-26-kill-ring.cs
```

Expect each file's jaribu summary to report all passed / 0 failed.

**Not in scope:** live Windows `Get-Clipboard` CRLF paste in a real TTY (batch with 454-019); wrapped-line redraw (454-019).
