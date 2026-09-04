# Fix REPL HandleCharacter unclamped selection crash

Parent: 470 (2026-09-04 full-repo review). Severity: bug (M4).

## Description

`source/timewarp-nuru/repl/input/repl-console-reader.cs:226-228` — `HandleCharacter` replaces an active selection with unclamped `SelectionState.Start`/`End`. History, kill-ring, and undo/redo replace or shorten `UserInput` without clearing selection.

Repro: select a suffix (`Shift+Left`), `Ctrl+K` (End now past `UserInput.Length`), type a character → `ArgumentOutOfRangeException`. Same crash via select-all then Up-arrow to a shorter history entry then type.

454-020 clamped cut/paste/delete (`GetClampedBounds`); the character-insert path and selection-clearing on non-selection mutations were missed.

## Requirements

- Use `SelectionState.GetClampedBounds(UserInput.Length)` in `HandleCharacter` (and fix or delete dead `HandleCharacterWithOverwrite`).
- Clear selection at the start of every path that replaces/shortens `UserInput` without going through selection handlers (history, kill, yank, undo/redo/revert, tab-completion apply).
- TestTerminal coverage of the repro.

## Checklist

- [ ] Clamp HandleCharacter
- [ ] Clear selection on buffer-replacing commands
- [ ] Regression test
- [ ] Do not duplicate 454-019 (wrapped-line redraw)

## Notes

Evidence: parent 470 `review/round-1/merged.md` M4. Area file `review/round-1/repl-completion.md` Issue 1.
