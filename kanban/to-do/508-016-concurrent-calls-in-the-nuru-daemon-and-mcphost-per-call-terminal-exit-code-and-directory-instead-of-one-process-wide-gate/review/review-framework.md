# Review framework — task 508-016

**Date:** 2026-10-20
**Host task:** kanban/to-do/508-016-concurrent-calls-in-the-nuru-daemon-and-mcphost-per-call-terminal-exit-code-and-directory-instead-of-one-process-wide-gate/
**Diff scope:** branch `task/508-016-concurrent-calls-in-the-nuru-daemon-and-mcphost-pe` vs `master` (`52fdd906`, 53 files, +2293/−182)
**Plan / brief:** Remove the process-wide daemon and McpHost call gate. Each served call gets its own terminal, exit code, working directory and service scope, with an exclusive opt-in, per-call cancellation, soak coverage, timings, and docs.
**Effort:** 3 (by-diff, 2475 lines; roster axis general only)
**Reviewer roster:** general
**Session IDs:** grok `01a1581e-a3a7-76b2-ba52-ec583804afec` (2026-10-20)

## Budget (by-diff)

Round 2 re-review budget (the M1 fix). Round 1 was 2475 lines, effort 3, turn cap 200.

- Lines changed: 359
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Round 2

**Date:** 2026-10-20
**Diff scope:** `74d7b4cc..f2b2bccc` (15 files, +325/−34). The M1 fix only, not the whole branch.
**Plan / brief:** Render continues the invoke call so the handler's exit code and the call's directory survive. Re-verify M1 and scan the fix delta.
**Effort:** 2 (by-diff, 359 lines; roster axis general only)
**Reviewer roster:** general
**Session IDs:** grok `01a1583d-7723-7650-800d-aa18ddd1df46` (2026-10-20)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
