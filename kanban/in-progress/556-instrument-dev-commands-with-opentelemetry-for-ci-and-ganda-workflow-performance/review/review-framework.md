# Review framework

## Budget (by-diff)

- Lines changed: 1587
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Scope — task 556

**Date:** 2026-10-30
**Host task:** kanban/in-progress/556-instrument-dev-commands-with-opentelemetry-for-ci-and-ganda-workflow-performance/
**Diff scope:** branch `task/556-instrument-dev-commands-with-opentelemetry-for-ci` vs `master` (merge base), implementation commit 507051ba. Product files: `tools/dev-cli/dev.cs`, `tools/dev-cli/Directory.Build.props`, `tools/dev-cli/endpoints/{incoming-trace-context,workflow-stage-span,workflow-command}.cs`, `tools/dev-cli/telemetry.md`, `tools/dev-cli/telemetry-proof/{proof.cs,Directory.Build.props}`.
**Plan / brief:** task.md `## Goal (implementer restatement)` and `## Requirements`. Turn on `UseTelemetry()` in `dev`, add a `TimeWarp.Nuru` child span per workflow stage, prove export with a local OTLP sink, and document the env vars.
**Effort:** 3 (roster axes: general)
**Reviewer roster:** general (reviewer-claude, the review oracle)
**Session IDs:** reviewer-claude 2cfdcf90-5860-4c1f-ad6d-1819705fab5d

## Ground rules

- Reviewers are read-only on product code. They write only under `review/round-N/`.
- Severity: bug | suggestion | nit. Status starts as open.
- Do not invent issues to fill space. Zero issues is a valid outcome.
- Review the diff and the call sites around it. Re-check falsifiable claims against the repo.
- Prior rounds are immutable. New work goes in `round-(N+1)/`.
