# Review framework — task 515

**Date:** 2026-10-21
**Host task:** kanban/to-do/515-unknown-options-are-silently-accepted/
**Diff scope:** branch `task/515-unknown-options-are-silently-accepted` vs `master` (2 commits, 715 lines). Product diff is the unknown-option matcher, runtime helper, tests, changelog, and routing docs. The Peacock settings commit is on the branch to clear the master checkout and is outside the matcher review.
**Plan / brief:** Generated argv matching must fail closed on an option-shaped token the route does not declare, name it, and suggest a close declared form. Negative numbers, a lone `-`, and tokens after `--` stay positionals. Catch-all routes reject unknown options before `--`. Routes with no options keep `Unknown command` for an extra non-option token. REPL uses the same matcher. `Should_ignore_undefined_options` becomes a failure test.
**Effort:** 2 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle grok `01a15edb-f7b0-7011-9e3b-44f7fe830d66` (2026-10-21). Implementer session was not reported.

## Budget (by-diff)

- Lines changed: 715
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

The review oracle wrote the general review directly. Roster is the single general axis.
