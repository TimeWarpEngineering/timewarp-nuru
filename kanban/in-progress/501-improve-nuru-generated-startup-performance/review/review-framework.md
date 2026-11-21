# Review framework — task 501

**Date:** 2026-11-21
**Host task:** kanban/in-progress/501-improve-nuru-generated-startup-performance/
**Diff scope:** branch `task/501-improve-nuru-generated-startup-performance` vs merge-base `740a3661` (master). Four commits, 1164 lines in `git diff --stat`. Product and test diff reviewed; kanban `task.md` included for the restatement check.
**Plan / brief:** Make Nuru's own startup cheaper and measurable (task 501). Implementer restatement is in `task.md`.
**Effort:** 3 (general only; by-diff budget)
**Reviewer roster:** general
**Session IDs:** implementer-claude session 28ec19d3-31b5-4e7e-930c-fcb89c5025ca; reviewer Grok session 01a1fcf7-678e-7302-88c7-a5553cb89ee6

## Budget (by-diff)

- Lines changed: 1164
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
