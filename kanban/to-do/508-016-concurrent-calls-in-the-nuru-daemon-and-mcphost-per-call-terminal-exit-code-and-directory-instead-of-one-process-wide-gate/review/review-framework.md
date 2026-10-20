# Review framework — task 508-016

**Date:** 2026-10-20
**Host task:** kanban/to-do/508-016-concurrent-calls-in-the-nuru-daemon-and-mcphost-per-call-terminal-exit-code-and-directory-instead-of-one-process-wide-gate/
**Diff scope:** branch `task/508-016-concurrent-calls-in-the-nuru-daemon-and-mcphost-pe` vs `master` (`52fdd906`, 53 files, +2293/−182)
**Plan / brief:** Remove the process-wide daemon and McpHost call gate. Each served call gets its own terminal, exit code, working directory and service scope, with an exclusive opt-in, per-call cancellation, soak coverage, timings, and docs.
**Effort:** 3 (by-diff, 2475 lines; roster axis general only)
**Reviewer roster:** general
**Session IDs:** grok `01a1581e-a3a7-76b2-ba52-ec583804afec` (2026-10-20)

## Budget (by-diff)

- Lines changed: 2475
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
