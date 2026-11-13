# Enable the green-key ledger skip on nuru after ganda and architecture prove out

## Description

Phase 2 of the green-key ledger for nuru: skip a CI run whose content key already passed in an earlier green run. This is the last repo in the rollout (ganda, then architecture, then nuru) and starts only after the ganda shadow-mode pilot found no false skips and the skip has run on ganda and architecture.

Source: [timewarp-intel PR #75](https://github.com/TimeWarpEngineering/timewarp-intel/pull/75), action A as reworked by Busara (shadow mode first; then ganda → architecture → nuru; after nuru 537; Steven's yes first).

**Savings are estimates** from intel PR #74/#75: over nuru's last 96 push/PR runs, 70 (923 run-wall-min, 72%) had a code-input key that had already passed, but most of those are kanban-only pushes that 537 already removes. Beyond 537, at least the 17 tree-equal master pushes, about 263 min per week. Not measured.

Filed 2026-10-25 at Steven's request ("File tasks to do this", 23:44 Asia/Bangkok).

## Depends on

- 537

## Requirements

- Port the ledger from ganda's dev CLI (timewarp-ganda tasks 469 and 470) into nuru's `tools/dev-cli/dev.cs`. Each repo has its own dev CLI.
- Run nuru in shadow mode first (log hit or miss, skip nothing) for a short window, then compare with the estimate. Any false skip (a hit whose full run failed) blocks enabling.
- Same key and rules as ganda: code inputs including lint inputs jobs read, SDK version, `global.json`, workflow file and scripts, runner image version, container image digests; same-repo master/PR runs write markers, never fork PRs; scheduled, `release` and `workflow_dispatch` runs never skip.
- On master, carry the green run's `Packages-*` artifact forward, or make the release "promote latest `Packages-*`" lookup tolerate skipped commits (as in 537).
- A skipped run still reports success on the stable required `ci` check.
- Record skipped runs and minutes saved over several weeks in Results.

## Checklist

- [ ] Confirm the gates in Notes are met
- [ ] Port the ledger and run a short shadow window
- [ ] Enable the skip; prove a tree-equal master push is skipped and `ci` is green
- [ ] Prove release still finds a `Packages-*` artifact after a skipped commit
- [ ] Record skips and minutes saved
- [ ] Open a PR with run links as proof

## Notes

- Cross-repo gates (not expressible in `## Depends on`): timewarp-ganda 469 (shadow pilot) Results show no false skip, timewarp-ganda 470 (skip on ganda) has merged, and timewarp-architecture 416 (skip on architecture) has merged.
- If 539 (wslc runner) lands first, the runner image version in the key is the wslc image; otherwise the hosted image version.
- Steven's yes before any workflow edit lands. The nuru repo bot owns the edit.

## Session

- Created: 988895 (2026-10-25)

## Results
