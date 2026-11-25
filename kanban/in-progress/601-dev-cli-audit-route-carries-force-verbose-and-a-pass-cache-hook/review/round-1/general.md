# Round 1 — general
**Date:** 2026-11-25
**Scope reviewed:** branch `task/601-dev-cli-audit-route-carries-force-verbose-and-a` vs merge-base `2d2dde35` (master). DevCli audit route, pass cache, tree stamp, subset selection, tests, docs, version bump, and the recorded audit fixes (line-cap baseline, changelog split, proof pin).

## Summary

The shared `dev audit` route takes `--force` and `--verbose`/`-v` and hands each check a `DevAuditContext` (repo root, force, verbose). `IDevAuditCheck.RunAsync` takes that context. A repo opts into a pass cache with `DevAuditChecks.ConfigurePassCache`. `DevAuditTreeStamp.Create()` is the ready-made cache: the same key rule and `artifacts/dev-audit/passed.key` path as ganda's `DevAuditStamp`, with the ignored top-level names configurable (default `kanban`). A recorded pass and no `--force` prints the skip line and runs nothing. The key is recorded only when every check passed. `DevAuditChecks.Named` / `Select` pick checks by name for an in-process `DevAuditRunner.RunAsync` that passes `passCache: null`, so that run neither reads nor records a pass. Version is `4.0.0-beta.18`. The readme, `dev-audit.md`, and the changelog describe the break and the migration.

The implementer restatement matches the task. The original allows a pass-cache hook or a DevCli-owned stamp; this change ships both, which the restatement and the checklist name. "A DevCli release carries the change" is the version bump, with `dev release` left as a follow-up after merge.

`## Results` records `./bin/dev build`, the four audit runfiles (6/6, 4/4, 14/14, 6/6), a compile of `tests/ci-tests/run-ci-tests.cs`, self-install plus `./bin/dev audit --force -v`, and `ganda repo audit` after the line-cap and pin fixes. Those runs cover this change. `audit-02-endpoint.cs` is excluded from the multi-mode assembly and listed in `tests/ci-standalone/run-standalone-tests.cs`, same pattern as the check-version endpoint tests. The service files are on both test props compiles; `audit-command.cs` is only on the standalone devcli props, so the multi assembly does not collect the route.

One suspected bug was checked and did not reproduce. Task 578 failed because ready-made internal types in the always-compiled `services/*.cs` glob tripped CA1812 in `tests/devcli-consumer-check`. `DevAuditTreeStamp` lives on that glob. Command: `dotnet build tests/devcli-consumer-check/devcli-consumer-check.csproj --nologo -v q`. Reason: a consumer that never calls `Create()` might still fail CA1812, and `./bin/dev build` does not compile that project. Result: Build succeeded, 0 warnings, 0 errors. The assembly contains `DevAuditTreeStamp`. `Create` holds the `new`, which is the opt-in factory, so a repo that does not turn the cache on still compiles.

The skip, force, record-only-on-pass, null key, throwing `KeyAsync`, empty list, subset, and tree-stamp rules in the runner and stamp match the tests and `dev-audit.md`. `Git.FindRoot` resolves a linked worktree (`.git` file) to that worktree, which is the root the stamp should use. The line-cap baseline records the 23 still-over-cap Markdown files at the lengths the check accepts; `changelog.md` was split under the cap, and the `*-iteration-N.md` debate files were renamed off the part-N rule.

## Issues

<!-- None. -->
