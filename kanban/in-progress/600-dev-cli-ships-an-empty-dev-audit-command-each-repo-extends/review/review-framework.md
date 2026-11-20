# Review framework — task 600

**Date:** 2026-11-21
**Host task:** kanban/in-progress/600-dev-cli-ships-an-empty-dev-audit-command-each-repo-extends/
**Diff scope:** branch `task/600-dev-cli-ships-an-empty-dev-audit-command-each-repo` vs merge-base `9f6abfbc` (master). Product paths: `source/Directory.Build.props`, DevCli `audit` endpoint and three services, DevCli readme, DevCli csproj description, `tests/ci-tests/Directory.Build.props`, `tests/timewarp-nuru-tests/devcli/Directory.Build.props`, `tests/timewarp-nuru-tests/devcli/audit-01-runner.cs`, `tools/dev-cli/dev.cs`. Kanban move of `task.md` is in the same range and is not product behavior. Master is one commit ahead (`9ac74a8d land kanban 600`); that commit is not in this diff.
**Plan / brief:** Ship `dev audit` in TimeWarp.Nuru.DevCli. Empty registry prints that nothing is registered and exits 0. A repo adds `IDevAuditCheck` implementations through `DevAuditChecks.AddRepoChecks` without owning the route. The command prints one line per check with wall time, prints a failing message in full, prints a passed/failed summary, and exits non-zero when any check fails. It never calls `ganda repo audit`.
**Effort:** 2 (by-diff, 653 lines). Roster is general only.
**Reviewer roster:** general
**Session IDs:** reviewer session not reported (print-mode review oracle, 2026-11-21)

## Budget (by-diff)

- Lines changed: 653
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
