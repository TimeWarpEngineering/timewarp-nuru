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

- [x] Split into separate WithArguments elements (superseded by stdin piping — see plan)
- [x] Quoting/escaping verified — moot under stdin piping (no argv embedding at all)
- [ ] Human verification on Windows (interactive — batched on parent 454, steps in Results)

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

## Results (2026-08-05)

Windows clipboard SET fixed (commit `a5e28072`). Cut/copy from the REPL kill ring /
selection now actually reaches the Windows clipboard.

**Implementation** (`repl-console-reader.clipboard.cs`, Windows branches only):
- `SetWindowsClipboardAsync` now pipes the text via **stdin** — `powershell -NoProfile
  -Command "$input | Set-Clipboard"` + `WithStandardInput(text)` — mirroring the file's
  proven Linux pwsh branch instead of the task's suggested two-argument form. This
  eliminates argv quoting/escaping entirely (quotes, apostrophes, newlines, length
  limits) and drops the fragile `'`-doubling. Previously the whole invocation was ONE
  glued argv element, so PowerShell never saw a valid `-Command` switch and the copy
  silently did nothing (M16).
- `GetWindowsClipboardAsync` gained `-NoProfile` (a chatty profile polluted the
  clipboard read's stdout and slowed every call).

**Evidence** (Windows branch can't execute on Linux CI):
- Pattern validated under real pwsh: stdin lines flow through `$input` intact;
  `Set-Clipboard -Value` has `ValueFromPipeline=True`.
- Reviewer verified from PS semantics + decompiled deps: Set-Clipboard accumulates ALL
  pipeline records (CRLF-joined) on both Windows PowerShell 5.1 and pwsh 7 — multi-line
  content correct, not last-line-wins; CliWrap disposes the stdin stream so powershell
  gets EOF and cannot hang; the empty-stdin edge is doubly unreachable; repo-wide sweep
  found no other glued-argv `WithArguments` sites.
- Multi-mode CI: 1401 total / 1394 passed / 7 skipped / 0 failed (unchanged — the diff
  touches only `OperatingSystem.IsWindows()` branches).

**Phase 4b review** — 1 round, single independent reviewer, effort 1. Disposition:
**clean** — 0 actionable findings. 1 LOW/informational accepted: on legacy-codepage
Windows consoles (CP437/850 conhost), non-ASCII content (emoji/CJK) is mangled at the
parent-side encoding step; NOT a regression (the old path copied nothing at all) and
correct under UTF-8 consoles / Windows Terminal. Noted in the human pass below.

### How to validate

**Smoke (automated, this box):**
```bash
dotnet build source/timewarp-nuru/timewarp-nuru.csproj   # compiles clean
printf 'a "b" c\nd' | pwsh -NoProfile -Command '$input | Write-Output'  # pattern proxy
```
**Expect:** build succeeds; the pwsh proxy echoes both lines with quotes intact.

**Automated gate:**
```bash
dotnet run tests/ci-tests/run-ci-tests.cs   # 1401 / 1394 passed / 7 skipped / 0 failed
```

**Human verification pending (Windows, batched on parent 454):**
1. In any `AddRepl()` app on Windows: type `echo "hello world" test`, Shift+Home to
   select, Ctrl+X (cut) → paste into Notepad. Expect the exact text including quotes.
2. Repeat with a line containing an apostrophe: `it's a test` (covers the old escaping
   bug directly).
3. Repeat with a multi-line paste (Ctrl+V of two lines, then cut both) — expect both
   lines, CRLF-separated.
4. Optional: in a legacy conhost (chcp 850), cut `héllo` — expect mangling (known,
   accepted limitation; fine under Windows Terminal / chcp 65001).

**Not in scope:** clipboard READ path behavior change (only -NoProfile added); Linux and
macOS paths untouched.
