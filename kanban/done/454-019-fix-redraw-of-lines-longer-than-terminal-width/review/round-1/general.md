# Round 1 — general
**Date:** 2026-09-14
**Scope reviewed:** wrap-aware single-line RedrawLine / UpdateCursorPosition vs origin/feature/overnight-nuru (d3701b68)

## Summary

The change extracts `WrappedLineLayout` for wrap math and teaches single-line `RedrawLine` / `UpdateCursorPosition` (plus selection redraw and Ctrl+L) to clear `max(previous, next)` occupied rows and map the logical cursor across wraps, including the old `desiredLeft >= WindowWidth` skip. Core row/col math matches the stated model, `repl-43` is **13/13** green on re-run, and production still passes `ReplOptions.Prompt` into `ReadLineAsync` so prompt-length accounting stays consistent. Dominant risk is the new sticky `InputStartRow` / `LastCursorVisualIndex` contract: any path that moves the physical cursor without refreshing those fields now mis-targets later cursor updates and clears (old `UpdateCursorPosition` used live `GetCursorPosition().top`).

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru/repl/input/tab-completion-handler.cs:187
- Description: `DisplayCandidates` writes a completion dump then `PromptFormatter.Format` + `currentInput` without updating `InputStartRow`, `LastCursorVisualIndex`, or `LastDrawnDisplayLength`. `UpdateCursorPosition` now places the cursor at `InputStartRow + rowOffset` (repl-console-reader.cs:338) instead of the previous live `GetCursorPosition().top`, so after Alt+= (`HandlePossibleCompletionsAsync` → `ShowPossibleCompletions`, repl-console-reader.cs:217-220) the next Left/Right/Home/End jumps back to the pre-completion row. The same desync hits multi-candidate Tab when `RedrawLine` → `ClearOccupiedDisplayRows` derives `startRow` from a stale `LastCursorVisualIndex` while the real cursor sits at the end of the newly written prompt+input (wrong when that end’s row offset differs from the logical cursor’s — wrapped mid-line completion). `TestTerminal.Write` / `WriteLine` do not advance `CursorTop`, so repl-43 cannot see this; it shows up on a real `Console`-backed terminal.
- Suggestion: After rewriting the prompt/input below the candidate list, re-sync wrap state from the physical cursor (same idea as `InitializeSingleLineDisplay`), set `LastDrawnDisplayLength = prompt.Length + input.Length`, then call `UpdateCursorPosition()` so the cursor returns to `CursorPosition`. Prefer routing that redraw through the reader’s clear/write helpers (or a small shared sync API) from both `HandlePossibleCompletionsAsync` and the post-`DisplayCandidates` Tab path. Add a TestTerminal regression once Write advances the cursor, or an integration assert that Alt+= then Left stays on the post-completion prompt row.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/timewarp-nuru/repl/input/repl-console-reader.search.cs:93
- Description: Search-line redraw itself is explicitly out of scope, but `ExitSearchMode` still calls the new wrap-aware `RedrawLine` after `RedrawSearchLine` has moved the physical cursor and left `LastCursorVisualIndex` / `InputStartRow` untouched. `ClearOccupiedDisplayRows` then computes `startRow` from a pre-search visual index and a post-search cursor top, which can clear/redraw the wrong rows when the search UI wrapped or was drawn on a non-start wrap row. This is the same state-contract footgun as Issue 1, on the deferred Ctrl+R path.
- Suggestion: Before `RedrawLine()` on search exit, re-sync `InputStartRow` / `LastCursorVisualIndex` / `LastDrawnDisplayLength` to the current physical cursor (or reset visual index to 0 and treat current top as the line start for the transition). When search redraw is later wrap-fixed, have it maintain the same fields as single-line redraw.
- Status: open
