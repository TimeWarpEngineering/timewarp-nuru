# Review framework — task 595

## Budget (by-diff)

- Lines changed: 1483
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

**Date:** 2026-11-16
**Host task:** kanban/in-progress/595-ship-the-pull-request-build-classifier-in-time-warp-nuru-dev-cli/
**Diff scope:** branch `task/595-ship-the-pull-request-build-classifier-in-time` vs `master` (merge-base `2aed8439`). Product commits `880af245` and `b2ee8275`, plus the kanban move commits. 15 files, +1334 / −149. Untracked at review start: this `review/` tree only.
**Plan / brief:** Ship `PullRequestBuildClassifier` in `TimeWarp.Nuru.DevCli` content (ganda `WorkflowChangeFilter.RequiresBuild`), one `DecideAsync` call, repo additions in `.timewarp/dev.jsonc`, tests, package readme, version `4.0.0-beta.15`, and timewarp-nuru's `dev workflow --stage classify` in place of the PowerShell path gate.
**Effort:** 3 (general only; budget roster is a single axis)
**Reviewer roster:** general
**Session IDs:** Grok review `01a1e403-0252-7720-adf2-120385f3ebb0`; implementer Claude `c38d03ab-dd0c-4a05-afaf-48941e0e01f2`

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
