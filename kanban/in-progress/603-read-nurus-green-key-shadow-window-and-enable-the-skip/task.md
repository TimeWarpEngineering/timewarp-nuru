# Read nuru's green-key shadow window and enable the skip

## Description

Phase 2 of nuru's green-key ledger, after the shadow logger in task 540. Read the shadow window, then turn the skip on. 540 logs hit or miss and skips nothing. This card is the enable, the master-push proof, the release proof, and the minutes tally.

Source: [timewarp-intel PR #75](https://github.com/TimeWarpEngineering/timewarp-intel/pull/75), action A. 540 carries the logger and the key rules.

**Savings are estimates** from intel PR #74/#75: over nuru's last 96 push/PR runs, 70 (923 run-wall-min, 72%) had a code-input key that had already passed, but most of those are kanban-only pushes that 537 already removes. Beyond 537, at least the 17 tree-equal master pushes, about 263 min per week. Not measured.

**That master figure is likely too high.** On ganda (470) a master push skips only when another green master run with the same key still has a live `Packages-*` to carry. The usual hit, the merge of a green PR, has none, so it builds. The saving that showed up on ganda is on PR runs: a re-push that changes only kanban or results keeps the key. On nuru, 537 already skips a PR whose files are all non-code, so measure the shadow window before quoting a number. Subtract the ci critical-path time 540 recorded: about 26 seconds per run (checkout about 4s and setup about 7s on ubuntu-latest release run 45946322936, plus a 15s local `green-key-finish` compile). Each ci run also logs `ci-finish-seconds`.

## Depends on

- 540

## Requirements

- **Decision, Steven 2026-11-28: "enable the skip now and relaunch".** Turn the skip on in this walk. Do not wait for a nuru shadow window; the evidence is ganda's pilot (about 77 runs, 469 and 470) and architecture 504, which run the same key rules. This is Steven's yes for the workflow edit.
- Record the two shadow runs read on 2026-11-28 as the before-state; no larger window is required. A false skip (a hit whose full run failed) blocks enabling when the key missed an input. A flaky test on a tree that had just passed is not a key defect: record the run and do not change the key for it (ganda 470; Steven waived two such runs on 2026-11-23).
- On master, skip only when a green master run with the same key still has a live `Packages-*` to carry forward, and republish those bytes on the skipped run (ganda 470's rule). Otherwise build. Release must still find a `Packages-*` for every master commit it can promote.
- A skipped run still reports success on the stable required `ci` check.
- Record skipped runs and minutes saved over several weeks in Results. Subtract the ci time 540 added (about 26 seconds per run, and the `ci-finish-seconds` line). Subtract the `green-key` job wall (`green-key-seconds`) on every push: that job sits in front of the stage matrix on a hit and on a miss.
- Open a PR with run links as proof.

## Goal (implementer restatement)

540's logger is on master and does not skip. The two same-repo lines read on 2026-11-28 are the before-state: both `would=full`, both `false_skip=false`. That is not a key defect. Steven said yes the same day to turn the skip on without a longer nuru window, using ganda's pilot and architecture 504, which share these key rules.

Turn `GreenKeyShadow.Enabled` off and make the workflow follow the decision the logger already computes. A pull request hit skips build, samples, and tests, still runs attestation, and leaves the required `ci` check green. A master hit skips only when a green master run for that key still has a live `Packages-*`; the skipped run downloads those bytes, checks they match this tree's packable set, and uploads them as this run's `Packages-*`, so release can still promote the commit. A master hit whose decision is `pack-only` builds and packs and does not retest. A master hit whose decision is `full` (no master marker, or the carry could not be read) runs the whole pipeline. A skip does not write a new green-key marker. Log lines say `mode=skip`, and a run that actually skips says `skip=yes`, so a tally does not read the old `shadow=log-only` lines as skips.

Skipped runs and minutes saved start at zero on this enable. The several-week count begins once the skip is on master. A saved-minute tally subtracts each skipped run's `ci-finish-seconds`, and it also subtracts the `green-key` job wall (`green-key-seconds`) on every push, because that job now sits in front of the stage matrix on a hit and on a miss. The proof pull request is the host's open-pr node; this walk records the wiring and the before-state.

Review round 1 is part of the same goal. The stage job's `if` must not read `matrix`: GitHub rejects that file, no job runs, and the required `ci` check never appears. Pack-only selects only the build leg inside `strategy.matrix`, where `needs` is allowed. Every other decision, including an empty `green_key_run`, keeps build, samples, test-multi, and test-standalone. The `!= 'skip'` gate stays in the job `if`, so a skip still runs no stage. A pack-only run is one leg, so `needs.stage.result` is success or failure.

## Checklist

- [x] Read the two 2026-11-28 shadow runs and compare them with the estimate. Both `would=full`, both `false_skip=false`. Steven waived a larger window.
- [x] Turn `GreenKeyShadow.Enabled` off and make the workflow act on the would-line (pull-request skip, master skip only with a live `Packages-*` republish, pack-only builds, carry-unknown builds).
- [ ] Prove a tree-equal master push is skipped and `ci` is green. This diff changes `workflow.yml` and `tools/dev-cli`, both key inputs, so this pull request is a miss. The live proof is a later same-key master push after merge.
- [ ] Prove release still finds a `Packages-*` artifact after a skipped commit. The master skip uploads the carried bytes; a live proof needs that later push.
- [x] Record the before-state and the subtraction rule. Skips so far: 0. Minutes saved: 0. The tally subtracts `green-key-seconds` on every push and `ci-finish-seconds` when the full-run wall predates that finish step. The several-week tally starts once the skip is on master.
- [ ] Open a PR with run links as proof (host open-pr node).

## Review

- Round 1: M1 and M2 were open. The fix pass implements both. Round 2 has not run.

## Notes

- Steven's yes to enable the skip was given on 2026-11-28 ("enable the skip now and relaunch"), on ganda's and architecture's evidence. The wait for a nuru shadow window is waived.
- 540 logged and did not skip. This card sets `GreenKeyShadow.Enabled` false, and the workflow acts on the decision.
- Cross-repo gates are already met (recorded on 540): nuru 537, timewarp-ganda 469 and 470, timewarp-architecture 504.
- ganda 709 tallies skips and minutes saved over two weeks. Use its numbers when sizing this window. It starts no earlier than 2026-12-07 and has no number yet.
- A `false_skip=true` line is a key defect. `pipeline=cancelled` is `rate=out` and `false_skip=false`. A path-skipped run is also `rate=out`. `pipeline=key-skipped` and `pipeline=pack-only` are rate-out as well. Do not tally `shadow=log-only` lines as skips.

## Session

- Created: 3078006 (2026-11-28)
- Filed from 540 review round 1 M1 so the deferred enable work stays on a card after 540 closes.
- 2026-11-28: first shadow read, about 80 minutes after PR #440 merged. Two `green-key-v1` runs, both `would=full` and `false_skip=false`. Skip left off. `GreenKeyShadow.Enabled` stays true. No product change.
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a2202c-19c8-7271-8cc3-ec07756abc82, max-turns 500, Blocked — 2026-11-28T03:27:42Z
- 2026-11-28: the earlier Blocked and Parked entries are resolved. Steven: "enable the skip now and relaunch". The shadow-window wait is waived and the yes is given. Claude session 06092692.
- 2026-11-28: implementer-grok turned the skip on. Session 01a22127-0b47-71b3-a689-25f5e184eafe.
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a22127-0b47-71b3-a689-25f5e184eafe, max-turns 500, Done — 2026-11-28T08:25:54Z
- Review oracle round 1: reviewer general (Claude Opus 5.5, effort 2, by-diff budget 673 lines, turn cap 120), 2026-11-28. Open findings M1 (bug) and M2 (suggestion). A fix pass is needed.
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 19855cc3-6d3a-4630-811a-b12818a772bc, max-turns 120, artifacts general.md, merged.md — 2026-11-28T08:28:08Z
- Fix pass (implement-fix-2, 2026-11-28): M1, the stage job `if` no longer reads `matrix`. Pack-only selects the build leg in `strategy.matrix`. M2, every push logs `green-key-seconds`, and the tally subtracts that wall. Session 01a22144-d57c-7280-811e-ccb7c5169c94.

## Results

The skip is on in this branch. `GreenKeyShadow.Enabled` is false, and the workflow acts on the decision the logger already computes. A pull request hit skips build, samples, and tests, still runs attestation, and leaves the required `ci` check green. A master hit skips only when a green master run for that key still has a live `Packages-*`; the skipped run downloads those bytes, checks they match this tree's packable set, and uploads them as this run's `Packages-*`. A mismatch or a missing artifact fails `ci` and uploads nothing. A master hit with markers but no live packages is `pack-only` (build and pack, no retest). A master hit with no master marker, or a carry probe that cannot be read, runs the whole pipeline (`carry-unknown`). A skip does not write a new green-key marker. Acted lines say `skip=yes` or `skip=pack-only`. Misses still say `mode=skip would=full`. Old `shadow=log-only` lines are not skips.

Before-state, read 2026-11-28T03:23Z, after 540 merged at 2026-11-28T02:00:55Z ([PR #440](https://github.com/TimeWarpEngineering/timewarp-nuru/pull/440), commit `0c848a13`). Both lines are `shadow=log-only`. Neither is a key defect (`false_skip=false`).

| Run | Event | Wall | Earlier | Would | End |
| --- | --- | --- | --- | --- | --- |
| [46095191604](https://github.com/TimeWarpEngineering/timewarp-nuru/actions/runs/46095191604) | pull_request | 01:51:25Z–02:00:45Z, about 9.3 min | miss, key `558f255d…` | `would=full reason=miss` | `pipeline=succeeded false_skip=false rate=in`, `ci-finish-seconds=69` |
| [46095948159](https://github.com/TimeWarpEngineering/timewarp-nuru/actions/runs/46095948159) | push master (the merge) | 02:00:58Z–02:10:20Z, about 9.4 min | hit, marker run 46095191604 (the PR) | `would=full reason=carry-unknown` | `pipeline=succeeded false_skip=false rate=in`, `ci-finish-seconds=50` |

The master hit is the usual merge of a green PR. Carry candidates are master rows only, and there was no earlier master marker, so the decision is `full` / `carry-unknown`. The build, samples, and test stages still ran. Zero minutes would have been saved. The intel estimate (70 of 96 runs, about 263 min/week of tree-equal master pushes) is not measurable from these two lines. Ganda's pilot was about 77 runs before Steven's yes on 2026-11-23. Ganda 709, the two-week minutes tally, starts no earlier than 2026-12-07 and has no number to reuse. Steven waived a larger nuru window.

Skipped runs since this enable: 0. Minutes saved: 0. Once the skip is on master, minutes saved are the sum over `skip=yes` hits of (full-run wall − skipped-run wall), minus the `green-key-seconds` of every push in the window. The full-run wall is a full run of that tree with the `green-key` wait removed: a pre-enable full run, or a post-enable full run minus its own `green-key-seconds`. The skipped run's wall already includes that run's `green-key-seconds` and `ci-finish-seconds`. Subtract `ci-finish-seconds` again only when the full-run wall is a pre-540 measurement that does not include the finish step. 540's estimate of that finish cost is about 26 seconds (checkout about 4s, setup about 7s, local `green-key-finish` compile about 15s). The two before-state finish lines were 50s and 69s. `green-key-seconds` is the `green-key` job wall, from its first step to the log step, and the job prints `green-key-v1 green-key-seconds=` on every push, hit or miss. A pull request observes inside `changes` and has no such line. A release run may log the line and is not part of this sum. Count `skip=yes` lines only. `pipeline=key-skipped` and `pipeline=pack-only` are `rate=out`. The several-week count starts when this lands on master.

This diff changes `.github/workflows/workflow.yml` and `tools/dev-cli/`, and both are key inputs, so this pull request is a miss and runs the full pipeline. A live tree-equal master skip cannot happen until after merge, on a later push whose tree matches a green master run that still has `Packages-*`. The proof pull request, with that run's link, is the host open-pr node. There is no live release of a skipped commit yet. The upload step is what makes release able to find `Packages-<run_number>` on the skipped run.

A pack-only run has a single build leg, chosen in `strategy.matrix`. `needs.stage.result` is success or failure. A skip runs no stage, and `ci` does not require the matrix on a skip.

### How to validate

Smoke: on a same-repo pull request whose tree matches a green marker, the changes job logs `green-key-v1 skip=yes` (not `shadow=log-only`), the stage matrix does not run, and the required `ci` check succeeds. On a master push whose key matches a green master run that still has a live `Packages-*`, the stage matrix does not run, the log contains `carry=downloaded`, and the run uploads `Packages-<run_number>`. On a master push with `reason=carry-unknown` or `reason=miss`, the stage matrix runs all four legs. On a master push with `reason=no-packages-to-carry`, the matrix runs only `build`. Every push logs `green-key-v1 green-key-seconds=`.

Expect: `GreenKeyShadow.Enabled` is false. A miss logs `mode=skip would=full` and does not contain `skip=yes`. A skip logs `pipeline=key-skipped`, `false_skip=false`, `rate=out`. A failed carry logs `pipeline=key-skip-failed` and the `ci` check fails. The stage job's `if` does not read `matrix`. This branch's own pull request is a miss, because the workflow file and the dev CLI are key inputs.

Gates (fix pass, 2026-11-28):

- `./bin/dev build` — succeeded, 0 warnings, 0 errors.
- `dotnet run --file tests/timewarp-nuru-tests/devcli/green-key-01-rules.cs` — 14 passed. Standalone runfile (`#if !JARIBU_MULTI`).
- `/tmp/actionlint` 1.7.12 `.github/workflows/workflow.yml` — exit 0, no findings. The same binary on a copy that puts `matrix.stage` back into the stage job `if` exits 1: `context "matrix" is not allowed here`.
- `ganda repo audit` — pass, 43 passed, 0 failed, 1 warning (`vscode-window-icon`, pre-existing, exit 0).

The enable pass also ran `dotnet run --file tools/dev-cli/dev.cs -- workflow --help` (exit 0). This fix pass does not change that runfile.

### Review

- Round 1 (2026-11-28), reviewer general, effort 2. Counts: bug 1 open, suggestion 1 open, nit 0. The fix pass implements both. Round 2 has not run.
- M1 (bug): the stage job `if` no longer reads `matrix`. Pack-only selects `["build"]` in `strategy.matrix`; every other decision keeps the four legs. The `!= 'skip'` gate stays in the job `if`. actionlint 1.7.12 parses the file.
- M2 (suggestion): the tally subtracts `green-key-seconds` on every push. The `green-key` job logs that line the way `ci` logs `ci-finish-seconds`.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.
