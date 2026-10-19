# Review framework — task 508-006

**Date:** 2026-10-19
**Host task:** kanban/to-do/508-006-operation-endpoints-with-camelcase-properties-bind-by-declared-identifier-plus-the-roslynk-cli-findings/
**Diff scope:** branch `task/508-006-operation-endpoints-with-camelcase-properties-bind` vs `master` (commits `d7428936`, `b47130b2`)
**Plan / brief:** camelCase `[Operation]` members bind by the declared identifier; SYSLIB1031 collisions get a `TypeInfoPropertyName`; findings 2–6 are fixed here or filed
**Effort:** 2 (general only)
**Reviewer roster:** general
**Session IDs:** grok review `01a15255-516b-7a92-8d0d-869c54c346d5`; implementer session not reported

## Budget (by-diff)

- Lines changed: 647
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
