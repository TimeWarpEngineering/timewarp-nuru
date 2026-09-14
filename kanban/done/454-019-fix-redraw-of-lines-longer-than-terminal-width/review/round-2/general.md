# Round 2 — general
**Date:** 2026-09-14
**Scope reviewed:** M1 fix delta (dump-only completions + wrap re-anchor) plus re-verify M1/M2

## Summary

The M1 fix correctly makes `DisplayCandidates` dump-only and re-anchors wrap state via `AdoptPhysicalCursorAsNewSingleLineAnchor` only after Alt+= / first multi-candidate Tab dumps, then routes redraw through the reader’s `RedrawLine`. Single-candidate and cycling Tab return `DisplayedCandidates: false` so they do not adopt from an unmoved cursor. `ExitSearchMode` is unchanged (M2 wontfix stands). `repl-43` is **15/15** green; the new Alt+= Home tests assert `CursorLeft` and dump output without assuming `TestTerminal.WriteLine` advances `CursorTop`.

## Prior IDs

- M1: fixed — `DisplayCandidates` no longer rewrites prompt+input (`tab-completion-handler.cs:167-196`); `HandleTab` / `ShowPossibleCompletions` report dump occurrence; reader adopts physical top then `RedrawLine` only when dumped (`repl-console-reader.cs:199-228`, `295-301`). After adopt, `LastCursorVisualIndex = 0` / `LastDrawnDisplayLength = 0` so `ClearOccupiedDisplayRows` / `UpdateCursorPosition` target the post-dump prompt row.
- M2: wontfix stands — `ExitSearchMode` still calls bare `RedrawLine()` with no adopt (`repl-console-reader.search.cs:92-93`); search.cs is outside the fix delta. For in-scope non-wrapping search UI, live `currentTop` plus pre-search `LastCursorVisualIndex` still reconstructs `InputStartRow` and clears original wrap rows; adopting at the search-prompt row would leave remnants on earlier wrap rows of the original line.

## Issues
