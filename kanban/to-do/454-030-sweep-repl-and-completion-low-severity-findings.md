# Sweep REPL And Completion Low Severity Findings

> **Status: PARKED — needs human interactive verification (re-prioritized 2026-07-14).**
> REPL behavior (key handling, cursor/redraw, clipboard, Ctrl-C cancellation) cannot be
> verified by an automated agent in a non-interactive shell. Code may be written and
> compile-checked, but these are held for a human keyboard-verification pass. See the 454
> parent's reprioritization note.

Parent: 454 (2026-07-06 full code review). Severity: LOW (batch).

## Description

Low-severity REPL/completion findings in `source/timewarp-nuru/`:

1. `completion/completion/install-completion-handler.cs:90-138` — private `DetectShell()`
   is never referenced anywhere; shell auto-detection is unreachable dead code. Wire it
   up or delete it.
2. `repl/repl-history.cs:119,155` — Load/Save use ReadAllLines/WriteAllLines with no
   lock/merge; two REPL instances of the same app share
   `~/.nuru/history/<appName>` and the last writer clobbers the other. Also `Load` never
   clears `Items` (:124), so a second Load duplicates entries.
3. Shell completion templates:
   `completion/completion/templates/pwsh-completion-dynamic.ps1:6,10` — `-split ' '` /
   `-join ' '` with no quoting corrupts completion when a preceding token contains
   spaces. `fish-completion-dynamic.fish:17` — filters `^0$`, silently dropping a
   legitimate `0` candidate.
4. Systemic: cursor/index math is UTF-16 char-based; transpose/delete/word operations
   around an astral char (emoji surrogate pair) split the pair. Representative:
   `repl/input/repl-console-reader.word-operations.cs` swap-characters. Decide scope
   (full grapheme handling vs documented limitation).
5. `repl/repl-session.cs:34,101,111` — `CurrentSession` is a mutable static (nested/
   parallel sessions clobber it), and `ExecuteCommandAsync` (:260-267) only catches
   InvalidOperationException/ArgumentException — any other command exception tears down
   the whole REPL. Broaden the catch; reconsider the static.

## Checklist

- [ ] DetectShell wired or deleted
- [ ] History: merge-on-save or lock; Load clears before loading
- [ ] pwsh/fish template quoting + `0` candidate fixes
- [ ] Surrogate-pair decision recorded (fix or document)
- [ ] REPL survives unexpected handler exceptions
- [ ] CI tests green
