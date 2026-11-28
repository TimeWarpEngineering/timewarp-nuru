# Prove nuru's first live green-key skip and release after it, then tally minutes saved

## Description

Task 603 turned the green-key skip on in nuru's CI ([PR #441](https://github.com/TimeWarpEngineering/timewarp-nuru/pull/441), merged 2026-11-28T08:50Z). Three checklist items on 603 could not be proven in that walk, because #441 changed key inputs and so ran as a miss. This card collects that evidence from real runs. It is a reading task: no product change is expected unless a run shows a defect.

Blocked until the events below have happened. The first two need a same-repo run that logs `green-key-v1 … skip=yes` on master. The tally needs the skip to have been on master for at least two weeks, so no earlier than 2026-12-12.

## Requirements

- Find the first pull request run and the first master push run that log `skip=yes`. Record each run link, the key, the trusted run it matched, and that the required `ci` check is green.
- For the master skip, confirm the run downloaded the carried `Packages-*`, checked the set against the tree's packable set, and uploaded it as that run's `Packages-<run_number>`.
- Prove release finds a `Packages-*` for a skipped master commit. Use the release lookup read-only (a dry lookup, as ganda 470 did) against that commit. Do not cut a release for this card.
- Tally skips and minutes saved from the 603 merge to the read date, using the rule in 603's Results: sum over `skip=yes` hits of full-run wall minus skipped-run wall, minus `green-key-seconds` of every push in the window. Count `skip=yes` only; `pipeline=key-skipped` and `pipeline=pack-only` are `rate=out`. Also count `pack-only` and `carry-unknown` master hits, so the share of master hits that actually skip is known.
- List every `false_skip=true` line with its run link. A key that missed an input is a defect: file a fix task and say so here. A flaky test on a tree that had just passed is recorded and does not change the key (ganda 470; Steven's waiver of 2026-11-23).
- Compare the measured saving with the intel estimate in 540 (about 263 min per week on master, flagged there as likely too high) and with ganda 709's tally if it has numbers by then.
- If no master push has logged `skip=yes` by the read date, say so with the counts of `pack-only` and `carry-unknown` hits, and say whether the master skip can happen at all with today's artifact rules (ganda 470 names the option of PR runs uploading `Packages-*` with 1-day retention; that is a quota decision for Steven, not this card's to make).

## Checklist

- [ ] First pull request `skip=yes` run recorded, `ci` green
- [ ] First master `skip=yes` run recorded, carried `Packages-*` uploaded, `ci` green
- [ ] Release lookup finds `Packages-*` for a skipped master commit (dry lookup)
- [ ] Skips, minutes saved, pack-only and carry-unknown counts recorded
- [ ] `false_skip=true` lines listed and classified
- [ ] Compared with the 540 estimate and ganda 709

## Notes

- Source of the rules and the subtraction: `kanban/done/603-read-nurus-green-key-shadow-window-and-enable-the-skip/task.md`, `## Results`.
- Code: `tools/dev-cli/services/` (green-key ledger), `.github/workflows/workflow.yml`.
- Before-state: skips 0, minutes saved 0 at 2026-11-28.
- Related: 540 (shadow logger), 603 (enable), timewarp-ganda 469, 470, 709, timewarp-architecture 504.

## Session

- Created 2026-11-28 at Steven's request ("file the follow-up"). Claude session 06092692.

## Results
