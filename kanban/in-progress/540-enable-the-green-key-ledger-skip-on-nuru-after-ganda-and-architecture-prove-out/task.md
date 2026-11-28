# Enable the green-key ledger skip on nuru after ganda and architecture prove out

## Description

Phase 2 of the green-key ledger for nuru: skip a CI run whose content key already passed in an earlier green run. This is the last repo in the rollout (ganda, then architecture, then nuru). Ganda and architecture both run the skip now (see Notes), so the cross-repo gates are met.

Source: [timewarp-intel PR #75](https://github.com/TimeWarpEngineering/timewarp-intel/pull/75), action A as reworked by Busara (shadow mode first; then ganda → architecture → nuru; after nuru 537; Steven's yes first).

**Savings are estimates** from intel PR #74/#75: over nuru's last 96 push/PR runs, 70 (923 run-wall-min, 72%) had a code-input key that had already passed, but most of those are kanban-only pushes that 537 already removes. Beyond 537, at least the 17 tree-equal master pushes, about 263 min per week. Not measured.

**That master figure is likely too high.** On ganda (470) a master push skips only when another green master run with the same key still has a live `Packages-*` to carry. The usual hit, the merge of a green PR, has none, so it builds. The saving that showed up on ganda is on PR runs: a re-push that changes only kanban or results keeps the key. On nuru, 537 already skips a PR whose files are all non-code, so measure what is left in the shadow window before quoting a number.

Filed 2026-10-25 at Steven's request ("File tasks to do this", 23:44 Asia/Bangkok).

## Depends on

- 537

## Requirements

- Port the ledger into nuru's `tools/dev-cli/`. Each repo has its own dev CLI. Two references exist: ganda (tasks 469 and 470; `tools/dev-cli/green-key-workflow.cs`, `tools/dev-cli/endpoints/workflow/green-key-skip-planner.cs`, `green-key-skip-pipeline.cs`) and architecture (task 504; `tools/dev-cli/services/green-key.cs`, `green-key-ledger.cs`, `release-packages.cs`).
- Run nuru in shadow mode first (log hit or miss, skip nothing) for a short window, then compare with the estimate. A false skip (a hit whose full run failed) blocks enabling when the key missed an input. A flaky test on a tree that had just passed is not a key defect: record the run and do not change the key for it (ganda 470; Steven waived two such runs on 2026-11-23).
- Same key and rules as ganda: code inputs including lint inputs jobs read, SDK version, `global.json`, workflow file and scripts, runner image version, container image digests; same-repo master/PR runs write markers, never fork PRs; scheduled, `release` and `workflow_dispatch` runs never skip.
- On master, skip only when a green master run with the same key still has a live `Packages-*` to carry forward, and republish those bytes on the skipped run (ganda 470's rule). Otherwise build. Release must still find a `Packages-*` for every master commit it can promote.
- A skipped run still reports success on the stable required `ci` check.
- Record skipped runs and minutes saved over several weeks in Results.

## Goal (implementer restatement)

Nuru's `dev workflow` grows a green-key ledger with the same rules as timewarp-ganda: a versioned key over the code inputs this CI actually reads, the SDK, `global.json`, the workflow file and its scripts, the runner image, and any container image digest. Same-repo master pushes and same-repo pull requests may look up and, after a full success, write a marker. Fork pull requests may look up and never write. `release`, `schedule`, and `workflow_dispatch` are not candidates.

This change runs that ledger in shadow mode. Every candidate run logs hit or miss, including the phase-2 decision (a pull request hit would skip; a master hit would skip only when a green run for the key still has a live `Packages-*`, otherwise it would pack only). Nothing is skipped. The stage jobs still build and test, and the required check stays `ci`. A full success stages `artifacts/green-key/marker.json` for an `actions/upload-artifact` step. A hit whose full run then fails is `false_skip=true` in the log. That is the signal that blocks turning the skip on when the key missed an input. A flaky test on a tree that had just passed is recorded and does not change the key.

The skip itself stays off (`GreenKeyShadow.Enabled`). Enabling it, proving a tree-equal master push stays green, proving release still finds `Packages-*`, and recording minutes saved all wait on the shadow window this logger starts once it is on the workflow. The workflow edit is in this branch. Notes still require Steven's yes before that edit merges.

## Checklist

- [x] Confirm the gates in Notes are met (2026-11-28)
- [x] Port the ledger into `tools/dev-cli/` and log hit or miss without skipping
- [ ] Run a short shadow window and compare it with the estimate (starts once this is on the workflow)
- [ ] Enable the skip; prove a tree-equal master push is skipped and `ci` is green
- [ ] Prove release still finds a `Packages-*` artifact after a skipped commit
- [ ] Record skips and minutes saved
- [ ] Open a PR with run links as proof

## Notes

- Cross-repo gates, checked 2026-11-28, all met:
  - nuru 537 (path filters): done.
  - timewarp-ganda 469 (shadow pilot): done. About 80 runs, hit rate 37.8%. Two `false_skip=true` runs, both flaky tests on a tree that had just passed; Steven waived them on 2026-11-23.
  - timewarp-ganda 470 (skip on ganda): done.
  - Skip on architecture: shipped under timewarp-architecture 504 (wslc runners plus green-key skip). Architecture 416, the card this gate first named, is still in that repo's backlog and is superseded by 504.
- 539 (wslc runner) is done, so same-repo runs key on the wslc image. That image publishes no image id into the job; ganda uses the SHA-256 of `/var/lib/dpkg/status` as its version. Fork PRs stay on hosted runners and never write markers.
- ganda 709 tallies skips and minutes saved over two weeks; use its numbers when sizing the nuru window.
- Steven's yes before any workflow edit lands. The nuru repo bot owns the edit.

## Session

- Created: 988895 (2026-10-25)
- 2026-11-28: backlog review; gates rechecked and spec refreshed (Steven: "update 540"). Claude session 06092692.
- 2026-11-28: shadow port of the green-key ledger. Same-repo runs log hit or miss and the phase-2 would-line (`shadow=log-only`). The stage matrix still builds and tests. `GreenKeyShadow.Enabled` stays true, so a skip is not wired. The shadow window, the enable proof, and the minutes tally stay open until this is on the workflow.

## Results

Shadow mode only. The ledger is in `tools/dev-cli/services/` and `dev workflow` logs it. A pull request observes inside the existing classify step (same wslc runner as the stages). Every other event observes in a parallel `green-key` job that does not sit in front of the matrix. The `ci` job logs the end line, including `false_skip`, and uploads `green-key-v1-<key>` after a same-repo full success. Nothing is skipped. Enabling the skip, proving a tree-equal master push, proving release still finds `Packages-*`, and recording minutes saved wait on the shadow window after merge.

### How to validate

Smoke: Open a pull request whose files are code. In the classify log, find `green-key-v1 shadow=log-only` and a `would=` line. The build, samples, and test stages still run. The required check is still `ci`.

Expect: A same-repo pull request logs `outcome=hit` or `outcome=miss`. A hit logs `would=skip` and still builds and tests. A master push logs `would=skip` with `carry_run` only when a green run for that key still has a live `Packages-*`; otherwise it logs `would=pack-only` or `would=full`, and the build stage still runs. A hit whose full run then fails logs `false_skip=true`. Fork, release, schedule, and `workflow_dispatch` runs do not upload a marker. `GreenKeyShadow.Enabled` is true.

Gates:

- `./bin/dev build` — succeeded, 0 warnings
- `dotnet run --file tests/timewarp-nuru-tests/devcli/green-key-01-rules.cs` — 14 passed. Standalone runfile (`#if !JARIBU_MULTI`); CI runs it from `tests/ci-standalone/run-standalone-tests.cs`
- `dotnet run --file tools/dev-cli/dev.cs -- --help` — succeeded. Type-check of the dev CLI runfile, which `./bin/dev build` does not compile
- `ganda repo audit` — passed, 43 passed, 0 failed, 1 warning (`vscode-window-icon`, exit 0)
