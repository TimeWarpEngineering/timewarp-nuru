# Review framework

## Budget (by-diff)

- Lines changed: 673
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Scope (round 1)

**Date:** 2026-11-28
**Host task:** kanban/in-progress/603-read-nurus-green-key-shadow-window-and-enable-the-skip/
**Diff scope:** branch `task/603-read-nurus-green-key-shadow-window-and-enable-the` vs `master` (merge base), product files: `.github/workflows/workflow.yml`, `tools/dev-cli/endpoints/workflow-command.cs`, `tools/dev-cli/endpoints/workflow-command.green-key.cs` (new), `tools/dev-cli/services/green-key-{ledger,ledger-io,log,skip}.cs`, `tests/timewarp-nuru-tests/devcli/green-key-01-rules.cs`
**Plan / brief:** task.md `## Goal (implementer restatement)`: turn the green-key skip on (PR skip, master skip only with a live `Packages-*` carry, pack-only builds, carry-unknown builds)
**Reviewer roster:** general (review oracle, Claude Opus 5.5)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Zero issues is a valid outcome; do not invent findings
- Verify against the diff and the kitchen `## Results` / `### How to validate` record; no full build, test run, audit, or hooks
- Prior rounds are immutable; new work goes in `round-(N+1)/`
