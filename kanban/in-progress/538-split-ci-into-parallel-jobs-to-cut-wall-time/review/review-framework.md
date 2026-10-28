# Review framework — task 538

**Date:** 2026-10-28
**Host task:** kanban/in-progress/538-split-ci-into-parallel-jobs-to-cut-wall-time/
**Diff scope:** branch `task/538-split-ci-into-parallel-jobs-to-cut-wall-time` vs merge-base `46dabc6de2d2e6b4f67086da7eb74a0c348ab602` (`master`). Product paths: `.github/workflows/workflow.yml`, `.github/actions/setup-dotnet/action.yml`, `tools/dev-cli/endpoints/workflow-command.cs`, `tools/dev-cli/endpoints/test-command.cs`, `tests/ci-tests/run-ci-tests.cs`, `tests/ci-standalone/run-standalone-tests.cs`, `.agent/local/nuru-specific.md`.
**Plan / brief:** Split the single `ci` job into parallel stages (build, samples, test-multi, test-standalone), keep a final job named `ci` so branch protection is unchanged, preserve `Packages-*` promotion, and record wall time and runner-minutes.
**Effort:** 3 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle session not reported (Cursor, max-turns uncapped; review budget 200)

## Budget (by-diff)

- Lines changed: 954
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
