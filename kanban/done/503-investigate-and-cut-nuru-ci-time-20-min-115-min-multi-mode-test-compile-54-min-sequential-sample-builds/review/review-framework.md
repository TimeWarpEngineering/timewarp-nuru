# Review framework — task 503

## Budget (by-diff)

- Lines changed: 380
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-10-14
**Host task:** kanban/to-do/503-investigate-and-cut-nuru-ci-time-20-min-115-min-multi-mode-test-compile-54-min-sequential-sample-builds/
**Diff scope:** branch `task/503-investigate-and-cut-nuru-ci-time-20-min-115-min-mu` vs `master` (d96fa689).
Product files: `.github/workflows/workflow.yml`, `tests/Directory.Build.props`, `tests/ci-tests/run-ci-tests.cs`,
`tools/dev-cli/endpoints/verify-samples-command.cs`, `tools/dev-cli/endpoints/workflow-command.cs`.
**Plan / brief:** cut CI time. NuGet cache on setup-dotnet. Analyzers and code style off for the test compile.
Samples build in parallel after the shared projects build once. Standalone tests run in parallel. Each step prints its duration.
**Effort:** 2 (by diff)
**Reviewer roster:** general (review oracle, Claude Opus 5.5)
**Session IDs:** review oracle under `ganda task work` (headless); vendor session id not exposed

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
