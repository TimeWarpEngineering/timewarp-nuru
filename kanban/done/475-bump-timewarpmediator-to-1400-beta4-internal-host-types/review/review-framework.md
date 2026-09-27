# Review framework — task 475

**Date:** 2026-09-27
**Host task:** kanban/to-do/475-bump-timewarpmediator-to-1400-beta4-internal-host-types/
**Diff scope:** branch `task/475-bump-timewarpmediator-to-1400-beta4-internal-host` vs `origin/master` (`6164cefa`, `21a5d58c`)
**Plan / brief:** Pin TimeWarp.Mediator Contracts and Generators to 14.0.0-beta.4. Keep or drop `RemoveTimeWarpMediatorGenerator` only if apps still call exactly one `AddGeneratedMediator()`. Record CS0436 before/after. Changelog under Unreleased.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review-oracle (ganda task work, tw-implementation-review, Cursor implementer-cursor profile, headless)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
