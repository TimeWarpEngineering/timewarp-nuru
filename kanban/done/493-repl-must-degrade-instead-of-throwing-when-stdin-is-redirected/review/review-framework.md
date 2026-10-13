# Review framework

## Budget (by-diff)

- Lines changed: 467
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 493

**Date:** 2026-10-12
**Host task:** kanban/in-progress/493-repl-must-degrade-instead-of-throwing-when-stdin-is-redirected/
**Diff scope:** branch task/493-repl-must-degrade-instead-of-throwing-when-stdin-i vs origin/master (source, generator emitters, changelog, repl-49 test)
**Plan / brief:** line-mode REPL fallback when stdin is redirected; catch ReadKey InvalidOperationException; propagate REPL exit code through `RunReplAsync` (`Task<int>`)
**Effort:** 2
**Reviewer roster:** general
**Session IDs:** claude review oracle (ganda task work, 2026-10-12)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
