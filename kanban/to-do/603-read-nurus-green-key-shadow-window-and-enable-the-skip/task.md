# Read nuru's green-key shadow window and enable the skip

## Description

Phase 2 of nuru's green-key ledger, after the shadow logger in task 540. Read the shadow window, then turn the skip on. 540 logs hit or miss and skips nothing. This card is the enable, the master-push proof, the release proof, and the minutes tally.

Source: [timewarp-intel PR #75](https://github.com/TimeWarpEngineering/timewarp-intel/pull/75), action A. 540 carries the logger and the key rules.

**Savings are estimates** from intel PR #74/#75: over nuru's last 96 push/PR runs, 70 (923 run-wall-min, 72%) had a code-input key that had already passed, but most of those are kanban-only pushes that 537 already removes. Beyond 537, at least the 17 tree-equal master pushes, about 263 min per week. Not measured.

**That master figure is likely too high.** On ganda (470) a master push skips only when another green master run with the same key still has a live `Packages-*` to carry. The usual hit, the merge of a green PR, has none, so it builds. The saving that showed up on ganda is on PR runs: a re-push that changes only kanban or results keeps the key. On nuru, 537 already skips a PR whose files are all non-code, so measure the shadow window before quoting a number. Subtract the ci critical-path time 540 recorded: about 26 seconds per run (checkout about 4s and setup about 7s on ubuntu-latest release run 45946322936, plus a 15s local `green-key-finish` compile). Each ci run also logs `ci-finish-seconds`.

## Depends on

- 540

## Requirements

- Read the shadow window 540 starts once its workflow is on master, and compare it with the estimate above. A false skip (a hit whose full run failed) blocks enabling when the key missed an input. A flaky test on a tree that had just passed is not a key defect: record the run and do not change the key for it (ganda 470; Steven waived two such runs on 2026-11-23).
- On master, skip only when a green master run with the same key still has a live `Packages-*` to carry forward, and republish those bytes on the skipped run (ganda 470's rule). Otherwise build. Release must still find a `Packages-*` for every master commit it can promote.
- A skipped run still reports success on the stable required `ci` check.
- Record skipped runs and minutes saved over several weeks in Results. Subtract the ci time 540 added (about 26 seconds per run, and the `ci-finish-seconds` line).
- Open a PR with run links as proof.

## Checklist

- [ ] Read the shadow window and compare it with the estimate
- [ ] Enable the skip (`GreenKeyShadow.Enabled` off, and the workflow acts on the would-line). Prove a tree-equal master push is skipped and `ci` is green
- [ ] Prove release still finds a `Packages-*` artifact after a skipped commit
- [ ] Record skips and minutes saved
- [ ] Open a PR with run links as proof

## Notes

- Enabling the skip waits on Steven's yes. The shadow window is the evidence for that yes. 540's walk is the yes for the shadow logger only.
- 540 logs and does not skip. `GreenKeyShadow.Enabled` stays true until this card turns it off.
- Cross-repo gates are already met (recorded on 540): nuru 537, timewarp-ganda 469 and 470, timewarp-architecture 504.
- ganda 709 tallies skips and minutes saved over two weeks. Use its numbers when sizing this window.
- A `false_skip=true` line is a key defect. `pipeline=cancelled` is `rate=out` and `false_skip=false`. A path-skipped run is also `rate=out`.

## Session

- Created: 3078006 (2026-11-28)
- Filed from 540 review round 1 M1 so the deferred enable work stays on a card after 540 closes.
