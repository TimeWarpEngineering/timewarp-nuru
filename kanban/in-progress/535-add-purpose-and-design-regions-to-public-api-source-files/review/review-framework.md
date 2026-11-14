# Review framework — task 535

**Date:** 2026-11-14
**Host task:** kanban/in-progress/535-add-purpose-and-design-regions-to-public-api-source-files/
**Diff scope:** `81633070` (`docs(source): add Design regions to public-API source files (535)`) against its parent, plus the kitchen land `e95f5752`. Merged `origin/master` history on this branch is out of scope.
**Plan / brief:** Comment-only `#region Design` (and `#region Open Questions` where intent is unsettled) on public-API source files. Goal restatement in `task.md`. Format from `tw-agent-context-regions`.
**Effort:** 3 (general only; by-diff, 1097 lines)
**Reviewer roster:** general
**Session IDs:** review oracle session not reported (cursor, grok 4.7)

## Budget (by-diff)

- Lines changed: 1097
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
