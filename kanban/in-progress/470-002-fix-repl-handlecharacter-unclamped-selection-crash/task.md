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

- [x] Clamp HandleCharacter
- [x] Clear selection on buffer-replacing commands
- [x] Regression test
- [x] Do not duplicate 454-019 (wrapped-line redraw)

## Notes

Evidence: parent 470 `review/round-1/merged.md` M4. Area file `review/round-1/repl-completion.md` Issue 1.

## Session

- Implementer: grok session 01a0a092-35ca-71f0-8288-0d7ef6017da3 (2026-09-14)

## Results

`HandleCharacter` now replaces an active selection with `SelectionState.GetClampedBounds(UserInput.Length)` so a stale End past the buffer cannot throw `ArgumentOutOfRangeException`. Dead `HandleCharacterWithOverwrite` (duplicate of `HandleCharacter`, no callers) is deleted. Buffer-replacing commands that skip selection handlers now `ClearSelection()` before mutating `UserInput`.

**Files**

- `source/timewarp-nuru/repl/input/repl-console-reader.cs` — clamp in `HandleCharacter`; clear on tab-completion apply
- `source/timewarp-nuru/repl/input/repl-console-reader.basic-editing.cs` — deleted unused `HandleCharacterWithOverwrite`
- `source/timewarp-nuru/repl/input/repl-console-reader.history.cs` — clear on previous/next/beginning/end/prefix-search
- `source/timewarp-nuru/repl/input/repl-console-reader.kill-ring.cs` — clear on kill/yank/yank-pop
- `source/timewarp-nuru/repl/input/repl-console-reader.undo.cs` — clear on undo/redo/revert
- `source/timewarp-nuru/repl/input/repl-console-reader.yank-arg.cs` — clear on yank-last/yank-nth
- `source/timewarp-nuru/repl/input/repl-console-reader.search.cs` — clear on search exit (accept or cancel)
- `tests/timewarp-nuru-tests/repl/repl-44-stale-selection-character-insert.cs` — TestTerminal repros

**Decisions**

- Belt and suspenders: clamp the insert path (so any remaining stale selection is slice-safe) and drop selection on non-selection mutations (so history/kill/undo do not leave End past `UserInput.Length`).
- Delete `HandleCharacterWithOverwrite` rather than fix it — `HandleCharacter` already implements overwrite mode and the overwrite helper had zero references.
- Yank-arg and i-search exit are the same class of buffer replace as yank/history, so they clear too. Word case/transpose keep length and were left alone.
- New `repl-44-*` file on purpose: does not touch `repl-43-wrapped-line-redraw.cs` (454-019).

**Tests** — `repl-44-stale-selection-character-insert.cs`: **3/3 passed**. Related green: repl-28 (19), repl-26 (17), repl-27 (15), repl-40 (4), repl-03 (8), repl-33 (20), repl-25 (14), repl-06 (8).

### How to validate

**Smoke**

```bash
dotnet run tests/timewarp-nuru-tests/repl/repl-44-stale-selection-character-insert.cs
```

**Expect**

- Grand total **3 passed / 0 failed**.
- `Typing_after_shift_left_then_kill_line_does_not_throw` executes `hellx` and prints `KILL-REPRO-OK` (Shift+Left, Ctrl+K, type `x` — the old `UserInput[end..]` crash).
- `Typing_after_select_all_then_shorter_history_does_not_throw` executes `hix` and prints `HISTORY-REPRO-OK` (select-all, Up to shorter `hi`, type `x`). That output is `hix` not `x`, which proves selection was cleared rather than clamped-and-replaced.
- `Typing_after_select_all_then_undo_to_shorter_buffer_does_not_throw` executes `abx` and prints `UNDO-REPRO-OK`.

**Automated gate**

```bash
dotnet run tests/timewarp-nuru-tests/repl/repl-44-stale-selection-character-insert.cs
dotnet run tests/timewarp-nuru-tests/repl/repl-28-text-selection.cs
dotnet run tests/timewarp-nuru-tests/repl/repl-26-kill-ring.cs
dotnet run tests/timewarp-nuru-tests/repl/repl-27-undo-redo.cs
dotnet run tests/timewarp-nuru-tests/repl/repl-40-reader-state-desync.cs
```

Expect each file's jaribu summary to report all passed / 0 failed.

**Not in scope:** 454-019 wrapped-line redraw; live TTY visual check of selection highlighting.
