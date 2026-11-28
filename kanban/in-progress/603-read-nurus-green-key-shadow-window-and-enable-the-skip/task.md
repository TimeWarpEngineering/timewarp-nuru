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
- Record skipped runs and minutes saved over several weeks in Results. Subtract the ci time 540 added (about 26 seconds per run, and the `ci-finish-seconds` line).
- Open a PR with run links as proof.

## Goal (implementer restatement)

Once 540's shadow logger is on master, read the same-repo `green-key-v1 shadow=log-only` lines and compare them with the intel estimate (most of the old 72% was kanban-only work 537 already skips; the leftover master saving is only a tree-equal push that still has a live `Packages-*`). A `false_skip=true` whose full run failed because the key missed an input blocks the enable. A flake on a tree that had just passed is recorded and does not change the key.

Steven said yes on 2026-11-28 without waiting for a larger nuru window (see Requirements). Turn `GreenKeyShadow.Enabled` off and make the workflow act on the would-line: a pull request hit skips, a master hit skips only when a green master run for the key still has a live `Packages-*` and those bytes are republished, and every other master hit builds. The required `ci` check stays green on a skip. Release must still find a `Packages-*` for every master commit it can promote. Skipped runs and minutes saved, minus the `ci-finish-seconds` 540 added, are recorded after the skip has been on for several weeks. The proof PR carries the run links.

## Checklist

- [ ] Read the shadow window and compare it with the estimate (2026-11-28: two runs only; not a window yet)
- [ ] Enable the skip (`GreenKeyShadow.Enabled` off, and the workflow acts on the would-line). Prove a tree-equal master push is skipped and `ci` is green
- [ ] Prove release still finds a `Packages-*` artifact after a skipped commit
- [ ] Record skips and minutes saved
- [ ] Open a PR with run links as proof

## Notes

- Steven's yes to enable the skip was given on 2026-11-28 ("enable the skip now and relaunch"), on ganda's and architecture's evidence. The wait for a nuru shadow window is waived.
- 540 logs and does not skip. `GreenKeyShadow.Enabled` stays true until this card turns it off.
- Cross-repo gates are already met (recorded on 540): nuru 537, timewarp-ganda 469 and 470, timewarp-architecture 504.
- ganda 709 tallies skips and minutes saved over two weeks. Use its numbers when sizing this window.
- A `false_skip=true` line is a key defect. `pipeline=cancelled` is `rate=out` and `false_skip=false`. A path-skipped run is also `rate=out`.

## Session

- Created: 3078006 (2026-11-28)
- Filed from 540 review round 1 M1 so the deferred enable work stays on a card after 540 closes.
- 2026-11-28: first shadow read, about 80 minutes after PR #440 merged. Two `green-key-v1` runs, both `would=full` and `false_skip=false`. Skip left off. `GreenKeyShadow.Enabled` stays true. No product change.
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a2202c-19c8-7271-8cc3-ec07756abc82, max-turns 500, Blocked — 2026-11-28T03:27:42Z
- 2026-11-28: the Blocked and Parked entries below are resolved. Steven: "enable the skip now and relaunch". The shadow-window wait is waived and the yes is given; implement the enable in this walk. Claude session 06092692.

## Results

No product change: the shadow window is two runs, and enabling waits on a window the size of ganda's pilot plus Steven's yes.

540 merged at 2026-11-28T02:00:55Z ([PR #440](https://github.com/TimeWarpEngineering/timewarp-nuru/pull/440), commit `0c848a13`). Read at 2026-11-28T03:23Z. Same-repo runs since that merge:

| Run | Event | Wall | Earlier | Would | End |
| --- | --- | --- | --- | --- | --- |
| [46095191604](https://github.com/TimeWarpEngineering/timewarp-nuru/actions/runs/46095191604) | pull_request | 01:51:25Z–02:00:45Z, about 9.3 min | miss, key `558f255d…` | `would=full reason=miss` | `pipeline=succeeded false_skip=false rate=in`, `ci-finish-seconds=69` |
| [46095948159](https://github.com/TimeWarpEngineering/timewarp-nuru/actions/runs/46095948159) | push master (the merge) | 02:00:58Z–02:10:20Z, about 9.4 min | hit, marker run 46095191604 (the PR) | `would=full reason=carry-unknown` | `pipeline=succeeded false_skip=false rate=in`, `ci-finish-seconds=50` |

The master hit is the usual merge of a green PR. Carry candidates are master rows only, and there was no earlier master marker, so `PredictMasterCarryAsync` returns `full` / `carry-unknown` rather than `pack-only`. The build, samples, and test stages still ran, and `ci` succeeded. That is not a false skip and not a would-skip. Zero minutes would have been saved. Nothing to subtract yet beyond recording the finish seconds: 50s and 69s, both above the 26s estimate (checkout about 4s, setup about 7s, local `green-key-finish` 15s).

Ganda's comparison used about 77 runs (hit rate 37.8%) before Steven's yes on 2026-11-23. Ganda 709, the two-week minutes tally, is still backlog and starts no earlier than 2026-12-07, so it has no number to reuse. The intel estimate (70 of 96 runs, 263 min/week of tree-equal master pushes) is not measurable from these two lines.

### How to validate

Smoke: `gh run list --repo TimeWarpEngineering/timewarp-nuru --workflow workflow.yml` since 2026-11-28T02:00:55Z, then `gh run view <id> --log` and grep `green-key-v1`.

Expect: a later read sees more than these two runs. A `would=skip` or `would=pack-only` is still `shadow=log-only` and the stage matrix still runs. `false_skip=true` is listed with its run id. `GreenKeyShadow.Enabled` is still true until the window is compared and Steven says yes.

Gates: none. No product file changed, so `./bin/dev build`, the green-key runfile, and `ganda repo audit` were not run.

## Blocked

2026-11-28: A shadow window after 540 merged (2026-11-28T02:00:55Z, https://github.com/TimeWarpEngineering/timewarp-nuru/pull/440). Ganda's pilot was about 40 runs or 1–2 weeks (77 measured) before Steven's yes. This window is two runs, both would=full and false_skip=false: PR https://github.com/TimeWarpEngineering/timewarp-nuru/actions/runs/46095191604 (miss) and master https://github.com/TimeWarpEngineering/timewarp-nuru/actions/runs/46095948159 (hit, carry-unknown, full pipeline still ran). Unblock: that window read and compared, no key-defect false_skip, and Steven's yes to turn GreenKeyShadow.Enabled off. The several-week minutes tally starts only after the skip is on.

- https://github.com/TimeWarpEngineering/timewarp-nuru/pull/440
- https://github.com/TimeWarpEngineering/timewarp-nuru/actions/runs/46095191604
- https://github.com/TimeWarpEngineering/timewarp-nuru/actions/runs/46095948159

## Parked

2026-11-28 at implement: A shadow window after 540 merged (2026-11-28T02:00:55Z, https://github.com/TimeWarpEngineering/timewarp-nuru/pull/440). Ganda's pilot was about 40 runs or 1–2 weeks (77 measured) before Steven's yes. This window is two runs, both would=full and false_skip=false: PR https://github.com/TimeWarpEngineering/timewarp-nuru/actions/runs/46095191604 (miss) and master https://github.com/TimeWarpEngineering/timewarp-nuru/actions/runs/46095948159 (hit, carry-unknown, full pipeline still ran). Unblock: that window read and compared, no key-defect false_skip, and Steven's yes to turn GreenKeyShadow.Enabled off. The several-week minutes tally starts only after the skip is on.

No failing command output.
