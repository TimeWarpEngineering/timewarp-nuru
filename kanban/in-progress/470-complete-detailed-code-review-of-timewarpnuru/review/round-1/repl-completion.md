# Round 1 — repl-completion
**Date:** 2026-09-04
**Scope reviewed:** `source/timewarp-nuru/repl/` (session, history, commands, key-bindings/, display/, input/*); `source/timewarp-nuru/completion/`; related tests under `tests/timewarp-nuru-tests/repl/` and `tests/timewarp-nuru-tests/completion/`; samples `endpoints|fluent/10-repl`, `11-completion`, and `samples/aspire-otel/` (REPL/`AddRepl` wiring only)

## Summary

454-done items in this area (006, 007, 017, 018, 020, 021, 030) verify as fixed in the current tree for their original scopes. **454-019** (wrapped-line redraw) remains present and is noted only. New defects found: (1) a post-M20 residual where kill/history/undo replace `UserInput` without clearing selection, while `HandleCharacter` still slices with unclamped `Start`/`End` and can throw; (2) Windows CRLF clipboard paste leaves `\r` in `UserInput` and advances `CursorPosition` in the wrong linear domain versus the multiline `\n` contract. Also: Shift+Enter/`AddLine` is only wired on the Default key-binding profile; bash dynamic completion still word-splits candidates via `compgen -W`. Aspire otel sample REPL wiring looks intact (`AddRepl` on the client). History ignore defaults and `repl-03b` coverage for secrets look sound.

## 454-019 status

**Still present.** `RedrawLine` clears exactly one row with `new string(' ', Terminal.WindowWidth)` at `source/timewarp-nuru/repl/input/repl-console-reader.cs:279`, and `UpdateCursorPosition` skips `SetCursorPosition` when `desiredLeft >= Terminal.WindowWidth` at `:310-314`. Same class of one-row clear also appears in `RedrawLineWithSelection` (`repl-console-reader.selection.cs:307`) and per-line clears in `RedrawMultiline` (`repl-console-reader.multiline.cs:107`) / `UpdateMultilineCursorPosition` col guard (`:160-163`). Tracked by `kanban/to-do/454-019-fix-redraw-of-lines-longer-than-terminal-width.md` — not filed as a new Issue.

## 454 regression check

| 454 ID | Still present? | Evidence |
|--------|----------------|----------|
| 454-006 H6 undo TrimToCapacity reverse | No | `undo-stack.cs:183-201` — enumerate → one `Reverse()` → drop oldest → push oldest-first; second reverse removed. Design comment at `:8-11`. |
| 454-007 H7 Windows CRLF multiline cursor | No (core contract) | `multiline-buffer.cs:81` `GetFullText()` joins with `'\n'`; `TotalLength`/`CursorToPosition` count 1 char/break (`:334`, `:350`, `:370`). `repl-console-reader.multiline.cs:67-70` `SyncFromMultilineBuffer` uses that contract. |
| 454-017 M15 Ctrl+C cancellation | No | `repl-session.cs:267-271` linked per-command CTS published to `CurrentCommandCts`; `OnCancelKeyPress` `:389-417` cancels in-flight command; OCE path returns 130 `:303-325`. Covered by `repl-42-ctrl-c-cancellation.cs`. |
| 454-018 M16 Windows clipboard SET | No | `repl-console-reader.clipboard.cs:95-106` — `SetWindowsClipboardAsync` pipes via stdin (`$input \| Set-Clipboard`), not a glued single argv. |
| 454-019 M17 wrapped-line redraw | **Yes** | See status note — `:279`, `:310`. Do not duplicate. |
| 454-020 M18/M19/M20 | No for original three; residual sibling below | M18: yank-arg defers removal until replacement found (`yank-arg.cs:55-104`). M19: `FindNextMatch(..., includeCurrent: true)` on pattern refine (`search.cs:162-167`, `:189`). M20: cut/paste/delete use `GetClampedBounds` (`selection.cs:193-194`, `:224-225`, `:251-252`; `selection.cs:79-84`). Residual: character-over-selection still unclamped + selection not cleared on history/kill/undo — filed as Issue 1. |
| 454-021 M21 enum Convert.ToInt32 | No | `enum-completion-source.cs:80-85` uses `$"{value:D}"` instead of `Convert.ToInt32`. |
| 454-030 LOW sweep | No | `DetectShell` absent from `install-completion-handler.cs`. History `Items.Clear()` on Load (`repl-history.cs:117-118`) + merge-on-Save (`:158-187`). pwsh AST tokenize + `^:` filter (`pwsh-completion-dynamic.ps1:8-11`, `:42`); fish `^:` only (`fish-completion-dynamic.fish:20`). UTF-16 limitation documented (`word-operations.cs:8-15`). Broad `catch (Exception)` + local session/`CurrentSession` mirror (`repl-session.cs:112-131`, `:327-333`). |

## Issues

### Issue 1 — Severity: bug
- File: `source/timewarp-nuru/repl/input/repl-console-reader.cs:226-228`
- Description: `HandleCharacter` replaces an active selection with `UserInput[..start] + char + UserInput[end..]` using raw `SelectionState.Start`/`End` (no `GetClampedBounds`). Meanwhile several buffer-replacing commands never clear selection: history nav (`repl-console-reader.history.cs:16-19`, `:32-35`, `:41-44`, and siblings), kill-ring ops (e.g. `HandleKillLineToRingAsync` at `repl-console-reader.kill-ring.cs:31-34` shortens the buffer with selection still live), and undo/redo/revert (`repl-console-reader.undo.cs:16-18`, `:32-34`, `:50-52`). Repro: select backward over a suffix (`Shift+Left`), `Ctrl+K` (kills from cursor to EOL → `End` now past `UserInput.Length`), type a character → `ArgumentOutOfRangeException`. Same crash via select-all then Up-arrow to a shorter history entry then type. Cut/paste/delete were clamped for 454-020 M20; the character-insert path and selection-clearing on non-selection mutations were missed. Dead twin: `HandleCharacterWithOverwrite` at `basic-editing.cs:104-106` (unreferenced).
- Suggestion: Use `SelectionState.GetClampedBounds(UserInput.Length)` in `HandleCharacter` (and delete or fix the dead overwrite helper). Clear selection (or clamp+clear) at the start of every path that replaces/shortens `UserInput` without going through selection handlers — at minimum history, kill, yank, undo/redo/revert, and tab-completion apply.
- Status: open

### Issue 2 — Severity: bug
- File: `source/timewarp-nuru/repl/input/repl-console-reader.selection.cs:231-233` (paste insert; replace-selection branch `:225-226` has the same length math)
- Description: Paste concatenates clipboard text into `UserInput` and advances `CursorPosition` by `clipboardText.Length`, then `RedrawLine` may `SyncToMultilineBuffer` (`repl-console-reader.cs:257-260`). On Windows, `Get-Clipboard` returns CRLF. `MultilineBuffer.SetText` correctly splits on `\r\n` (`multiline-buffer.cs:116`), so the display lines are right, but (a) `UserInput` still contains `\r` until a later `SyncFromMultilineBuffer`, and (b) `CursorPosition` counts each `\r`, while the multiline linear domain counts one char per break — cursor lands past the intended point; the next typed character edits the still-CRLF `UserInput`. Distinct from fixed 454-007 (GetFullText/`SyncFromMultilineBuffer` `\n` contract); this is the paste ingress path not normalizing to that contract.
- Suggestion: Before updating `UserInput`/`CursorPosition`, normalize clipboard newlines to `\n` (or route paste through `MultilineBuffer.SetText`/`InsertText` after fixing InsertText — see Issue 5) and set `CursorPosition` from `CursorToPosition` so it matches the `\n`-linear domain; call `SyncFromMultilineBuffer` after paste so `UserInput` stays consistent.
- Status: open

### Issue 3 — Severity: suggestion
- File: `source/timewarp-nuru/repl/key-bindings/default-key-binding-profile.cs:25` (present); missing from `emacs-key-binding-profile.cs`, `vi-key-binding-profile.cs`, `vscode-key-binding-profile.cs` (Enter bindings around emacs `:124`, vi `:103`, vscode `:108` — no `Shift+Enter` → `HandleAddLineAsync`)
- Description: Multiline `AddLine` (`Shift+Enter`) is only registered on the Default profile. On Emacs/Vi/VSCode, `Shift+Enter` has no binding; `KeyChar` is control so the main loop neither submits nor inserts (`repl-console-reader.cs:141-165`) — the chord is swallowed. Users who switch profiles lose the documented multiline gesture with no substitute (Emacs maps `Ctrl+J` to accept-line / exit, not add-line).
- Suggestion: Bind `(ConsoleKey.Enter, ConsoleModifiers.Shift)` → `HandleAddLineAsync` on Emacs, Vi, and VSCode profiles (and document any profile-specific alternative if intentionally omitted).
- Status: open

### Issue 4 — Severity: suggestion
- File: `source/timewarp-nuru/completion/completion/templates/bash-completion-dynamic.sh:31` and `:37`
- Description: `COMPREPLY=($(compgen -W "${suggestions[*]}" -- "$cur"))` joins candidates on IFS and relies on word-splitting. A completion value containing spaces (or glob metacharacters) is corrupted; the outer unquoted `$(...)` also re-splits. pwsh/fish were hardened in 454-030 for quoting/`0` candidates; bash was not.
- Suggestion: Feed candidates to `compgen` in a quoting-safe way (e.g. null-delimited / `printf '%q'` into a quoted `-W` word list, or iterate `compgen -W` per candidate), and assign `COMPREPLY` without unquoted command substitution.
- Status: open

### Issue 5 — Severity: nit
- File: `source/timewarp-nuru/repl/input/multiline-buffer.cs:145-150`
- Description: Public `InsertText` treats every `\r` and every `\n` as a separate `AddLine()`. A Windows `\r\n` sequence therefore inserts a blank line between content lines. `SetText` correctly accepts `\r\n`/`\n`/`\r` (`:115-116`). Only test call sites today, but the public API is inconsistent with `SetText`'s newline contract.
- Suggestion: Mirror `SetText` splitting (or skip a `\n` that follows a just-handled `\r`) so `InsertText("a\r\nb")` yields two lines, not three.
- Status: open

### Issue 6 — Severity: nit
- File: `source/timewarp-nuru/completion/completion/templates/zsh-completion-dynamic.zsh:18-23`
- Description: Zsh template still strips a trailing bare numeric line as an “exit code” (`=~ ^[0-9]+$`). `DynamicCompletionHandler` always emits candidates then a `:directive` line and never a numeric stdout exit line (`dynamic-completion-handler.cs:49-55`), so with a present directive this branch is inert. Unlike the pre-fix fish/pwsh filters it does not currently drop mid-list `"0"` candidates; it is stale protocol assumptions / dead logic.
- Suggestion: Remove the numeric exit-code strip (keep only the `:directive` handling) to match fish/pwsh and the handler protocol.
- Status: open
