# Review framework

## Budget (by-diff)

- Lines changed: 460
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Scope — task 591-006

**Date:** 2026-11-19
**Host task:** kanban/in-progress/591-006-generated-code-breaks-on-some-member-and-type-names/
**Diff scope:** branch `task/591-006-generated-code-breaks-on-some-member-and-type`, commits after
`7496e7ce` (move to in-progress): `43d0e297` (fix) and `b630b502` (kanban land). 13 files, +370/-54.
The merge-base against local `master` also pulls in already-merged 591-003..005 work; that is out of scope.
**Plan / brief:** task.md `## Goal (implementer restatement)` — keyword property names must not splice
`@` into compound locals; generated return types use the stored `global::` name.
**Effort:** 2, general only
**Reviewer roster:** general (review oracle, claude-opus-5-5, headless)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading
  the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to
  the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the
  command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
