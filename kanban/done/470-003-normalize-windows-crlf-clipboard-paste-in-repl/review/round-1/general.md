# Round 1 — general
**Date:** 2026-09-14
**Scope reviewed:** branch task/470-003-normalize-windows-crlf-clipboard-paste-in-repl vs origin/feature/overnight-nuru

## Summary

Paste no longer splices raw clipboard bytes into `UserInput` or advances `CursorPosition` by `clipboardText.Length`. `HandlePasteAsync` deletes a clamped selection, then `SyncToMultilineBuffer` → `InsertText` → `SyncFromMultilineBuffer`, so `UserInput` is `GetFullText()` (`\n` joins) and the cursor is `CursorToPosition`. `InsertText` shares `SplitLines` with `SetText` (`["\r\n", "\n", "\r"]`), so a CRLF pair is one break (M36). Risk is low: focused buffer/paste fix with solid unit and TestTerminal coverage. Smoke: `repl-31` **37/37 passed**, `repl-45` **4/4 passed**.

## Issues

