# Round 2 — general
**Date:** 2026-11-28
**Scope reviewed:** fix-pass delta `85363b84..ac2b28c6` (commit ac2b28c6, "fix: keep a green-key failure from failing a green master run"): `.github/workflows/workflow.yml`, `tools/dev-cli/services/green-key-{ledger,ledger-io,log,markers,skip}.cs`, `tests/timewarp-nuru-tests/devcli/green-key-01-rules.cs`, and task.md. I also checked follow-up task 603 on `origin/master`.

## Summary

The fix pass addresses all five round-1 findings. The `green-key` job now continues on error. The new `ci` checkout and setup steps continue on error, and the finish step is gated on both outcomes plus `!cancelled()`, so a ledger or checkout flake cannot turn a green master run red or skip the `Packages-*` upload. A cancelled stage maps to `pipeline=cancelled` (`rate=out`, `false_skip=false`) through a new `NormalizePipeline` helper. Carry probes now project `head_branch` and keep master rows only. The dead `pull_request` runner clause is gone. Follow-up card 603 exists on master, depends on 540, and carries the deferred checklist and the recorded ci time cost. The tests cover the new behavior: row parsing with `head_branch`, master-only carry, cancelled end line, normalization, and workflow text checks. Results record the fix-pass gates and the added ci critical-path time (about 26 s, plus a per-run `ci-finish-seconds` line). I found no new defects.

## Prior findings re-verified

| ID | Status | Evidence |
|----|--------|----------|
| M1 | fixed | `origin/master:kanban/to-do/603-read-nurus-green-key-shadow-window-and-enable-the-skip/task.md` exists. `## Depends on: 540`, and it holds the five deferred checklist items. 540's checklist, Notes, and Results name 603. |
| M2 | fixed | `workflow.yml:120` job-level `continue-on-error: true`. `:330` and `:338` guard checkout and setup. `:351-355` gate finish on both outcomes. Results record about 26 s of added ci time with run links, and finish now prints `ci-finish-seconds`. |
| M3 | fixed | Finish uses `!cancelled()` (`:352`). `GREEN_KEY_PIPELINE` maps `cancelled` (`:362`). `GreenKeyLog.Cancelled` and `NormalizePipeline` (`green-key-log.cs:25,141`) are covered by tests. |
| M4 | fixed | `GreenKeyMarkers.ArtifactListJq` projects `head_branch`. `CarryCandidateRuns` drops non-master rows and rows with a null branch (`green-key-skip.cs:92-98`). The test includes a PR row and a null-branch row. |
| M5 | fixed | `workflow.yml:121-124` runner expression is push-only. The test asserts `head.repo` is absent from the job. |

## Issues

None.

## Observations (not findings)

- A stage job that hits its timeout reports `cancelled`, so it would log `pipeline=cancelled` / `false_skip=false` instead of `failed`. The stage matrix sets no `timeout-minutes` (the default is 360), and `cancel-in-progress` applies only to pull requests, where it cancels the whole run and finish does not run. So this case is a six-hour hang. That is too rare to raise. Task 603 can treat a long `cancelled` hit as a suspect line when it reads the window.
- The workflow assertions in the tests match on text (`ShouldContain`), the same pattern round 1 accepted.

No build, test, audit, or hook was run. Read-only git and file reads only.
