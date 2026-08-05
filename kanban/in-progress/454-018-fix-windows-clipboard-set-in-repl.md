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

## Verification protocol (reviewer, 2026-07-07)

Implement now with TestTerminal-based unit coverage of the state/logic layer; do NOT
block on interactive verification. Interactive confirmation is batched into ONE human
REPL verification session tracked on parent task 454 (together with 454-007's pending
Windows multiline check). Leave a "Human verification pending" line in this task's
Results listing exactly what the human should try.

## Implementation Plan (2026-08-05)

The file already contains the right pattern: `SetLinuxClipboardAsync`'s pwsh branch pipes
the text via **stdin** (`-NoProfile -Command '$input | Set-Clipboard'` +
`WithStandardInput`). Mirroring THAT (rather than the read path's two-argument form the
task suggested) fixes the bug and eliminates argv quoting entirely — quotes, newlines,
`''`-escaping and command-length limits all become moot. Validated under real pwsh:
stdin lines flow through `$input` intact, and `Set-Clipboard -Value` is pipeline-enabled
(`ValueFromPipeline=True`; Set-Clipboard has accepted pipeline input since PS 5.0, so
Windows PowerShell 5.1 works too).

**Changes (`repl-console-reader.clipboard.cs`):**
1. `SetWindowsClipboardAsync` → `Shell.Builder("powershell").WithArguments("-NoProfile",
   "-Command", "$input | Set-Clipboard").WithStandardInput(text)`. Drops the broken
   single-glued-argv invocation AND the fragile `'` doubling.
2. `GetWindowsClipboardAsync` → add `-NoProfile` (a chatty profile pollutes stdout —
   Get-Clipboard's result would be corrupted — and slows every clipboard read).

**Tests:** the Windows branch cannot execute on this Linux box (`powershell` absent) and
the clipboard layer intentionally swallows failures, so no meaningful new automated test
exists beyond compile; the cut/copy state layer is already covered (repl-26/28). Evidence
gathered instead: the exact pipeline pattern runs correctly under real pwsh here.

**Human verification pending (batched on parent 454):** on Windows, in any AddRepl app:
type `echo "hello world" test`, select with Shift+Home, Ctrl+X (cut), paste into Notepad →
expect the exact text including quotes; repeat with a line containing an apostrophe (') to
cover the old escaping bug.
