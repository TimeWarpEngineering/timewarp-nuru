# Review framework

## Budget (by-diff)

- Lines changed: 144
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-10-13
**Host task:** kanban/to-do/498-update-nuget-packages-to-latest-timewarpmediator-1400-beta5-dogfood/
**Diff scope:** branch `task/498-update-nuget-packages-to-latest-timewarpmediator-1` vs `master` (29f581fc..c74bcdb4)
**Plan / brief:** bump TimeWarp.Mediator Contracts/Generators to 14.0.0-beta.5; take other pins to latest or record holds; dogfood verdict
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle claude (claude-opus-5-5), session id not exposed

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
