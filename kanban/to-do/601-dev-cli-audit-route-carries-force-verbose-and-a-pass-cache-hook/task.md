# DevCli audit route carries force, verbose and a pass-cache hook

## Description

`TimeWarp.Nuru.DevCli` 4.0.0-beta.17 ships a shared `audit` route. A repo adds
checks through `IDevAuditCheck` and `DevAuditChecks.AddRepoChecks`. The route
takes no options and has no way to skip a run.

timewarp-ganda already owns `dev audit` in `tools/dev-cli/endpoints/audit-command.cs`.
With DevCli beta.17 its dev-cli no longer compiles (`NURU_R003`, unreachable
route). It cannot move onto the shared route, because its command does three
things the shared one cannot express:

- `--force` and `--verbose,-v` options.
- A whole-run pass stamp (ganda task 707, `DevAuditStamp`): skip every check
  when this tree already passed, and record the pass only when all checks pass.
- A deps-only run that another command calls in process (ganda task 710,
  `DevAuditScope.DepsCheckOnly`, called from `pr-pipeline.cs`).

Source: timewarp-ganda task 705, `## Refactor proposal`, part 1 (2026-11-25).
timewarp-ganda task 725 moves ganda onto this route after the release.

## Wanted

- The shared `audit` route accepts `--force` and `--verbose,-v` and passes them
  to each check through a context argument (for example
  `RunAsync(DevAuditContext, CancellationToken)`).
- The runner has an optional pass-cache hook, or DevCli owns the tree-key
  stamp, so a repo gets "skip when this tree already passed" without its own
  command. `--force` bypasses it. The pass is recorded only when every check
  passes.
- A repo can run a named subset of its checks in process without going through
  the route.
- A DevCli release carries the change.

## Checklist

- [ ] `--force` and `--verbose,-v` on the shared `audit` route, passed to checks through a context
- [ ] Pass-cache hook or DevCli-owned tree stamp; `--force` bypasses it
- [ ] In-process entry point that runs a named subset of checks
- [ ] Tests for the options, the skip, and the subset run
- [ ] DevCli template and docs updated

## Session

- Created: 5e357388-dba6-4ffe-89e8-92df174ea48a (2026-11-25)

## Notes

Steven chose option (b) on timewarp-ganda 705 on 2026-11-25: ganda ships Nuru
beta.17 now and holds DevCli at beta.16 until this lands.

The names `IDevAuditCheck`, `DevAuditChecks.AddRepoChecks` and `DevAuditRunner`
are from the ganda 705 implementer's report. Check them against the DevCli
source before starting.
