# Review framework

## Budget (by-diff)

- Lines changed: 165
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Scope (round 1)

**Date:** 2026-11-28
**Host task:** kanban/in-progress/603-read-nurus-green-key-shadow-window-and-enable-the-skip/
**Diff scope:** branch `task/603-read-nurus-green-key-shadow-window-and-enable-the` vs `master` (merge base), product files: `.github/workflows/workflow.yml`, `tools/dev-cli/endpoints/workflow-command.cs`, `tools/dev-cli/endpoints/workflow-command.green-key.cs` (new), `tools/dev-cli/services/green-key-{ledger,ledger-io,log,skip}.cs`, `tests/timewarp-nuru-tests/devcli/green-key-01-rules.cs`
**Plan / brief:** task.md `## Goal (implementer restatement)`: turn the green-key skip on (PR skip, master skip only with a live `Packages-*` carry, pack-only builds, carry-unknown builds)
**Reviewer roster:** general (review oracle, Claude Opus 5.5)

## Scope (round 2)

**Date:** 2026-11-28
**Diff scope:** fix commit `2eb06261` (fix: keep the stage matrix out of the job if): `.github/workflows/workflow.yml` (`green-key` and `stage` jobs), `tests/timewarp-nuru-tests/devcli/green-key-01-rules.cs`, task.md Goal / Results / How to validate
**Plan / brief:** re-verify round-1 M1 and M2; scan the fix delta for new defects
**Effort:** 1 (by-diff 165 lines)
**Reviewer roster:** general (review oracle, Claude Opus 5.5)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Zero issues is a valid outcome; do not invent findings
- Verify against the diff and the kitchen `## Results` / `### How to validate` record; no full build, test run, audit, or hooks
- Prior rounds are immutable; new work goes in `round-(N+1)/`
