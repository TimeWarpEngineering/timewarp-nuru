# Fix Windows Clipboard Set In REPL

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
