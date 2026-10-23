# Review framework

## Budget (by-diff)

- Lines changed: 32
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Scope (round 1)

**Date:** 2026-10-23
**Host task:** kanban/to-do/519-create-nuru-mascot/
**Diff scope:** branch `task/519-create-nuru-mascot` vs `master` (commit aaad9047): five PNGs under `assets/mascot/`, one line in `readme.md`, task file.
**Plan / brief:** copy the five built mascot-kit PNGs byte for byte, add a centered hero to the top of `readme.md`, tick checklist items 1 to 5.
**Effort:** 1 (general only)
**Reviewer roster:** general (review oracle, Claude Opus 5.5, headless)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit. Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Scope (round 2)

**Date:** 2026-10-23
**Diff scope:** delta since round 1, `ecaee5e3..ab4f6f80` (fix pass implement-fix-2): `readme.md` line 1, task file, round-1 ledger.
**Effort:** 1 (general only), budget by diff 32 lines
**Reviewer roster:** general (review oracle, Claude Opus 5.5, headless)
