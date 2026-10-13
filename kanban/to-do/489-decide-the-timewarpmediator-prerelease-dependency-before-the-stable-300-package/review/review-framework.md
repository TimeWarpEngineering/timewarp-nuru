# Review framework

## Budget (by-diff)

- Lines changed: 333
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Scope — task 489

**Date:** 2026-10-13
**Host task:** kanban/to-do/489-decide-the-timewarpmediator-prerelease-dependency-before-the-stable-300-package/
**Diff scope:** branch `task/489-decide-the-timewarpmediator-prerelease-dependency` vs `master` (`git diff master...HEAD`, head `d70da00b`)
**Plan / brief:** Option 1 + dogfood. Pin `TimeWarp.Mediator.Contracts` / `.Generators` at stable `14.0.0`. Update the changelog,
migration guide, 487 release notes, and 487 launch gate #2. Add no NU5104 suppression.
**Effort:** 2 (by-diff); roster axes: general
**Reviewer roster:** general (review oracle, Claude Opus 5.5)
**Session IDs:** review oracle under `ganda task work` (headless); session id not exposed

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
