# Review framework

## Budget (by-diff)

- Lines changed: 1110
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Scope — task 506

**Date:** 2026-10-18
**Host task:** kanban/to-do/506-roslynk-cli-spike-on-nuru-pitch-for-pete/
**Diff scope:** branch `task/506-roslynk-cli-spike-on-nuru-pitch-for-pete` vs `master` (3 commits, `989ab292`..`ebe86357`): `spikes/roslynk-cli/**` plus the task's `## Results`.
**Plan / brief:** `## Goal (implementer restatement)` and `## Requirements` in task.md. A hand-written Nuru CLI with `diagnostics` and `refs` calling Roslynk in-process, the `--json-args` mapping record, the `--capabilities` token size, and cold versus warm timing.
**Effort:** 3 (by-diff budget). Roster axis: general only.
**Reviewer roster:** general (review oracle, claude-opus-5-5)
**Session IDs:** review oracle, headless `ganda task work`; session id not exposed to the oracle.

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
