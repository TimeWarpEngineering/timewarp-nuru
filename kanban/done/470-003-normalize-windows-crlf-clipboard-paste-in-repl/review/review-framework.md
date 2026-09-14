# Review framework — task 470-003

**Date:** 2026-09-14
**Host task:** kanban/in-progress/470-003-normalize-windows-crlf-clipboard-paste-in-repl/
**Diff scope:** branch `task/470-003-normalize-windows-crlf-clipboard-paste-in-repl` vs `origin/feature/overnight-nuru` (product `0a0d880c`, kanban results `b0dae0e2`, chore `3e44ab77`). Product surface is REPL clipboard paste via `MultilineBuffer.InsertText` + `SyncFromMultilineBuffer`, shared `SplitLines` for `SetText`/`InsertText`, `repl-31` InsertText tests, and `repl-45` TestTerminal paste-path tests.
**Plan / brief:** Parent 470 M5 — paste concatenated raw clipboard into `UserInput` and advanced `CursorPosition` by `clipboardText.Length`. Windows `Get-Clipboard` returns CRLF; `SetText` splits `\r\n` as one break, but `UserInput` kept `\r` and cursor math counted each `\r` while the multiline linear domain counts one char per break. Distinct from 454-007 (`GetFullText` / `SyncFromMultilineBuffer` `\n` contract). Folded nit M36: public `InsertText` treated every `\r` and every `\n` as a separate `AddLine()`, so `\r\n` inserted a blank line.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle grok session 01a0a0b5-d92d-7d53-add7-6f39a6e9942d (2026-09-14)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
- Requirements: normalize clipboard newlines before `UserInput`/`CursorPosition`; set cursor from the multiline linear domain and sync `UserInput` after paste; make `InsertText` match `SetText` newline splitting; tests (TestTerminal / buffer unit tests)
- Out of scope per task: live Windows TTY paste of `Get-Clipboard` CRLF (batch with 454-019); wrapped-line redraw (454-019)
