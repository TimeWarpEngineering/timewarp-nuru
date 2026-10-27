# Review framework

## Budget (by-diff)

- Lines changed: 313
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 537

**Date:** 2026-10-28
**Host task:** kanban/in-progress/537-ci-path-filters-to-skip-the-full-build-for-kanban-only-and-non-code-changes/
**Diff scope:** branch `task/537-ci-path-filters-to-skip-the-full-build-for-kanban` vs merge-base `cafd5fcf` (`master...HEAD`): `.github/workflows/workflow.yml`, `.githooks/pre-push.cs`, and this kitchen `task.md`
**Plan / brief:** Skip the `workflow.yml` build for kanban-only and other non-code pushes and PRs, keep the required `ci` check green when the build is skipped, cancel superseded PR runs only, leave `release` and `workflow_dispatch` ungated, and record NuGet cache hit/miss time in Results
**Effort:** 2 (general only; roster axes are general)
**Reviewer roster:** general
**Session IDs:** session not reported (cursor, grok-4.7, max-turns uncapped, bound 120)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
