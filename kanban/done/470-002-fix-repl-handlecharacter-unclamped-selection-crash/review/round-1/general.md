# Round 1 — general
**Date:** 2026-09-14
**Scope reviewed:** branch task/470-002-fix-repl-handlecharacter-unclamped-selection-crash vs origin/feature/overnight-nuru

## Summary

The change correctly clamps `HandleCharacter` via `SelectionState.GetClampedBounds(UserInput.Length)` and clears selection on the required buffer-replacing paths (history, kill/yank/yank-pop, undo/redo/revert, tab-completion, plus yank-arg and i-search exit). Dead `HandleCharacterWithOverwrite` is fully removed with no remaining callers. The three TestTerminal repros pass and the history case (`hix` not `x`) proves clear-vs-clamp; remaining `UserInput =` sites either already route through selection handlers, run only when selection is inactive, or are length-preserving by design. Overall risk is low.

## Issues
