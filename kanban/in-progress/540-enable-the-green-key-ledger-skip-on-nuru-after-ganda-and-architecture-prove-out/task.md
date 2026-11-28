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

The skip itself stays off (`GreenKeyShadow.Enabled`). Enabling it, proving a tree-equal master push stays green, proving release still finds `Packages-*`, and recording minutes saved wait on the shadow window. Those items move to a follow-up card that depends on this one. This branch's workflow edit is the shadow logger. The walk that is landing it is the yes for that edit.

Review round 1 is in scope. A green-key failure must not turn a green master run into a failed run, because release promote lists only successful runs. The green-key job continues on error. The ci job's checkout and setup continue on error, and the finish step runs only when the run is not cancelled and both of those steps succeeded, so a flake there still uploads `Packages-*`. A cancelled stage is `pipeline=cancelled` with `rate=out` and `false_skip=false`. Carry probes keep master rows, so a pull-request marker cannot use up the eight-probe limit. The green-key runner expression does not mention `pull_request`, because that job does not run for one. Results record the minutes these ci steps add, which a later tally subtracts.

## Checklist

- [x] Confirm the gates in Notes are met (2026-11-28)
- [x] Port the ledger into `tools/dev-cli/` and log hit or miss without skipping
- [x] Moved to 603: run a short shadow window and compare it with the estimate
- [x] Moved to 603: enable the skip; prove a tree-equal master push is skipped and `ci` is green
- [x] Moved to 603: prove release still finds a `Packages-*` artifact after a skipped commit
- [x] Moved to 603: record skips and minutes saved
- [x] Moved to 603: open a PR with run links as proof

## Review

- Round 1: Blocked — open M1, M2, M3, M4, M5
- Round 2: Done
## Notes

- Cross-repo gates, checked 2026-11-28, all met:
  - nuru 537 (path filters): done.
  - timewarp-ganda 469 (shadow pilot): done. About 80 runs, hit rate 37.8%. Two `false_skip=true` runs, both flaky tests on a tree that had just passed; Steven waived them on 2026-11-23.
  - timewarp-ganda 470 (skip on ganda): done.
  - Skip on architecture: shipped under timewarp-architecture 504 (wslc runners plus green-key skip). Architecture 416, the card this gate first named, is still in that repo's backlog and is superseded by 504.
- 539 (wslc runner) is done, so same-repo runs key on the wslc image. That image publishes no image id into the job; ganda uses the SHA-256 of `/var/lib/dpkg/status` as its version. Fork PRs stay on hosted runners and never write markers.
- ganda 709 tallies skips and minutes saved over two weeks; use its numbers when sizing the nuru window.
- The shadow workflow edit lands with this walk. Enabling the skip is [task 603](https://github.com/TimeWarpEngineering/timewarp-nuru/blob/master/kanban/to-do/603-read-nurus-green-key-shadow-window-and-enable-the-skip/task.md), which waits on Steven's yes after the shadow window is read. The nuru repo bot owns workflow edits.
- Deferred checklist items 3–7 (shadow window, enable, release proof, minutes saved, proof PR) are task 603, which depends on 540.

## Session

- Created: 988895 (2026-10-25)
- 2026-11-28: backlog review; gates rechecked and spec refreshed (Steven: "update 540"). Claude session 06092692.
- 2026-11-28: shadow port of the green-key ledger. Same-repo runs log hit or miss and the phase-2 would-line (`shadow=log-only`). The stage matrix still builds and tests. `GreenKeyShadow.Enabled` stays true, so a skip is not wired. The shadow window, the enable proof, and the minutes tally stay open until this is on the workflow.
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a21f96-b680-7262-a431-e0bfbe7c5a25, max-turns 500, Done — 2026-11-28T01:25:20Z
- Review oracle round 1 (2026-11-28): reviewer general (Claude Opus 5.5, claude-opus-5-5), effort 3, turn cap 200. 5 open findings (M1 bug, M2 suggestion, M3–M5 nit). Fix pass requested. See `review/round-1/merged.md`.
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 2ebf0ecc-6285-4173-a0f2-1f822ba12aa8, max-turns 200, artifacts general.md, merged.md — 2026-11-28T01:29:41Z
- Fix pass (implement-fix-2, 2026-11-28): M1–M5. Follow-up card is 603. The green-key job and the ci checkout and setup continue on error. Finish runs only when the run is not cancelled and those two steps succeeded. A cancelled stage is `pipeline=cancelled`. Carry probes keep master rows. The green-key runner expression no longer mentions `pull_request`.
- Review oracle round 2 (2026-11-28): reviewer general (Claude Opus 5.5, claude-opus-5-5), effort 2, turn cap 120. Re-verified M1–M5 on delta 85363b84..ac2b28c6, all fixed, with no new findings. Disposition clean. See `review/round-2/merged.md` and `review/disposition.md`.

## Results

Shadow mode only. The ledger is in `tools/dev-cli/services/` and `dev workflow` logs it. A pull request observes inside the existing classify step (same wslc runner as the stages). Every other event observes in a parallel `green-key` job that does not sit in front of the matrix. The `ci` job logs the end line, including `false_skip`, and uploads `green-key-v1-<key>` after a same-repo full success. Nothing is skipped. Enabling the skip, proving a tree-equal master push, proving release still finds `Packages-*`, and recording minutes saved are task 603, which depends on 540.

Review round 1 fixes: the `green-key` job continues on error, so a ledger failure cannot turn a green master run into a failed run. The `ci` checkout and setup continue on error, and finish runs only after both succeeded, so a flake there still uploads `Packages-*`. Finish also requires `!cancelled()`, and a cancelled stage that still reaches it logs `pipeline=cancelled` with `rate=out` and `false_skip=false`. Carry probes keep `head_branch=master` rows. The `green-key` runner expression has no `pull_request` clause.

Added `ci` critical path, measured 2026-11-28, about 26 seconds in front of the existing job:

- Checkout about 4s and Setup .NET about 7s on ubuntu-latest, from release run https://github.com/TimeWarpEngineering/timewarp-nuru/actions/runs/45946322936 (fetch-depth 0; the ci checkout is fetch-depth 1, and setup uses the same action with the NuGet cache on).
- `green-key-finish` 15s locally (14.72s). CI logs `ci-finish-seconds` on each run. The runfile directory is not restored from the Actions cache, so this local compile is the proxy.
- Master run https://github.com/TimeWarpEngineering/timewarp-nuru/actions/runs/46074391477 spent 15s in `ci` with no checkout (22:11:41Z–22:11:56Z). Task 603 subtracts these seconds when it tallies minutes saved.

### How to validate

Smoke: Open a pull request whose files are code. In the classify log, find `green-key-v1 shadow=log-only` and a `would=` line. The build, samples, and test stages still run. The required check is still `ci`.

Expect: A same-repo pull request logs `outcome=hit` or `outcome=miss`. A hit logs `would=skip` and still builds and tests. A master push logs `would=skip` with `carry_run` only when a green master run for that key still has a live `Packages-*`; otherwise it logs `would=pack-only` or `would=full`, and the build stage still runs. A hit whose full run then fails logs `false_skip=true`. A cancelled stage logs `pipeline=cancelled`, `false_skip=false`, and `rate=out` when finish runs, and a cancelled workflow does not run finish. A failed `green-key` job or a failed ci checkout does not fail the workflow run. Fork, release, schedule, and `workflow_dispatch` runs do not upload a marker. `GreenKeyShadow.Enabled` is true.

Gates (fix pass, 2026-11-28):

- `./bin/dev build` — succeeded, 0 warnings
- `dotnet run --file tests/timewarp-nuru-tests/devcli/green-key-01-rules.cs` — 14 passed. Standalone runfile (`#if !JARIBU_MULTI`); CI runs it from `tests/ci-standalone/run-standalone-tests.cs`
- `dotnet run --file tools/dev-cli/dev.cs -- workflow --stage green-key-finish` — succeeded in 14.72s. Type-check of the dev CLI runfile, which `./bin/dev build` does not compile, and the ci-finish wall-time proxy above
- `ganda repo audit` — passed, 43 passed, 0 failed, 1 warning (`vscode-window-icon`, exit 0)

### Review

- Rounds: 2, roster general. Round 1 at effort 3, round 2 at effort 2 (fix-pass delta).
- Final counts (round 2): bug 0 open / 1 fixed, suggestion 0 open / 1 fixed, nit 0 open / 3 fixed; 0 wontfix.
- Disposition: **clean**. M1–M5 were fixed in ac2b28c6, and round 2 raised no new findings. The deferred phase-2 work is task 603.
- Paths: `review/review-framework.md`, `review/round-1/merged.md`, `review/round-2/general.md`, `review/round-2/merged.md`, `review/disposition.md`.

## Learnings

- Scope narrowing (review round 1). The restatement narrows the task to shadow mode only. That is reasonable, because the task itself says to run the shadow window first and that window takes weeks. But it leaves the title goal (enable the skip) and four requirements with no card. When a walk can only deliver one phase of a multi-week task, file the follow-up card in the same PR so the done move does not drop the rest. why: review round 1 M1, task 540 (2026-11-28)
