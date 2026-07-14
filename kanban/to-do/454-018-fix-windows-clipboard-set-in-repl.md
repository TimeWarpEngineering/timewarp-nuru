# Fix Windows Clipboard Set In REPL

> **Status: PARKED — needs human interactive verification (re-prioritized 2026-07-14).**
> REPL behavior (key handling, cursor/redraw, clipboard, Ctrl-C cancellation) cannot be
> verified by an automated agent in a non-interactive shell. Code may be written and
> compile-checked, but these are held for a human keyboard-verification pass. See the 454
> parent's reprioritization note.

Parent: 454 (2026-07-06 full code review). Severity: MEDIUM (M16, Windows only).

## Description

`source/timewarp-nuru/repl/input/repl-console-reader.clipboard.cs:96` —
`SetWindowsClipboardAsync` glues the whole PowerShell invocation into ONE argv element:
`.WithArguments($"-command \"Set-Clipboard -Value '{...}'\"")`. TimeWarp.Amuru's
`WithArguments` passes each element as a single literal argument with no shell splitting,
so PowerShell never sees a valid `-command` switch — cut/copy to system clipboard (kill
ring) silently does nothing on Windows.

The READ path at line ~86 does it correctly with two separate arguments — mirror that.

## Checklist

- [ ] Split `-command` and the script into separate WithArguments elements
- [ ] Verify quoting/escaping of clipboard content (quotes, newlines) in the script arg
- [ ] Human verification on Windows (interactive)

## Verification protocol (reviewer, 2026-07-07)

Implement now with TestTerminal-based unit coverage of the state/logic layer; do NOT
block on interactive verification. Interactive confirmation is batched into ONE human
REPL verification session tracked on parent task 454 (together with 454-007's pending
Windows multiline check). Leave a "Human verification pending" line in this task's
Results listing exactly what the human should try.
