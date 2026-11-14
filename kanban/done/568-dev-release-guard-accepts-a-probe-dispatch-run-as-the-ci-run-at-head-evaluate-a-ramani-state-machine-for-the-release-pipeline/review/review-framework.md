# Review framework — task 568

**Date:** 2026-11-14
**Host task:** kanban/in-progress/568-dev-release-guard-accepts-a-probe-dispatch-run-as-the-ci-run-at-head-evaluate-a-ramani-state-machine-for-the-release-pipeline/
**Diff scope:** product commit `73a533c1` (parent `5e20dc31`) plus kitchen land `c9f58d52`. Files: `release-command.cs`, `release-command.guards.cs`, `ci-run-promotion.cs`, `release-guard.cs`, `release-01-guard-matrix.cs`, `documentation/developer/guides/releasing.md`, `report/analysis.md`. Branch history also contains the master merge; those commits are outside this review.
**Plan / brief:** `dev release` step 8 accepts a candidate run only when `SelectPackagesArtifact` finds a live `Packages-*` artifact, prints a skip line otherwise, and keeps walking a commit whose runs are all skipped. Ramani evaluation under `report/` recommends not adopting a state machine now.
**Effort:** 2
**Reviewer roster:** general
**Session IDs:** reviewer session not reported (cursor harness, no `--max-turns` flag; bound cap 120)

## Budget (by-diff)

- Lines changed: 565
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/` and the kitchen `task.md` sections this oracle owns
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
