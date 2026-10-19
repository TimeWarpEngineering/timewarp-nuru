# Review framework — task 508-014

**Date:** 2026-10-20
**Host task:** kanban/to-do/508-014-read-the-contract-operation-group-as-grouppath-without-prefixing-the-route/
**Diff scope:** branch `task/508-014-read-the-contract-operation-group-as-grouppath-wit` vs `master` (commit `1b8041d6`)
**Plan / brief:** Pin TimeWarp.Contracts 2.0.0-beta.23 and read `[Operation(Group)]` as metadata `groupPath` without prefixing the route or the command.
**Effort:** 2 (general only)
**Reviewer roster:** general
**Session IDs:** grok review `01a15569-6033-7f90-bda6-6da34e87d296`; implementer session not reported

## Budget (by-diff)

- Lines changed: 401
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
