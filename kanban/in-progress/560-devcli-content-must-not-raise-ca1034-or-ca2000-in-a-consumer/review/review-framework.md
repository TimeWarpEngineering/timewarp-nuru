# Review framework — task 560

**Date:** 2026-10-31
**Host task:** kanban/in-progress/560-devcli-content-must-not-raise-ca1034-or-ca2000-in-a-consumer/
**Diff scope:** branch `task/560-devcli-content-must-not-raise-ca1034-or-ca2000-in` vs `master` (product commit `3914c2a6`, kitchen land `5f3e5e2c`)
**Plan / brief:** task.md Requirements and Goal — DevCli content types `internal`, CA2000 ownership the analyzer can see, no suppression
**Effort:** 2 (general only)
**Reviewer roster:** general
**Session IDs:** implementer `796401fe-ef87-4a30-82af-04402009babd`; review oracle this session (no separate vendor session id)

## Budget (by-diff)

- Lines changed: 449
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
