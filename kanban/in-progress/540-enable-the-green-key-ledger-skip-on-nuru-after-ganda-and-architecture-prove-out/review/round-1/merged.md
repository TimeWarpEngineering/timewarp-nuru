# Round 1 — merged findings
**Date:** 2026-11-28
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 3 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: kanban/in-progress/540-…/task.md:36
- Description: Checklist items 3–7 (shadow window read, enable the skip, the master proof, the release proof, minutes saved) are deferred, and no card tracks them. The host done node then closes 540 with that work untracked.
- Suggestion: File a follow-up card with `ganda kanban create` (Depends on 540) that carries the deferred requirements and checklist. Name it in 540's Results and Notes.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: .github/workflows/workflow.yml:109 (also :320, :327)
- Description: The shadow ledger can fail CI. The `green-key` job has no `continue-on-error`, so a failed job makes a green master run conclude `failure`. `dev release` locate-run lists only `--status success` runs (`release-command.guards.cs:336`), so that commit cannot be promoted. The new `ci` checkout and setup steps are unguarded on the required check. The added `ci` critical-path time is not recorded.
- Suggestion: Job-level `continue-on-error: true` on `green-key`. Guard the two new `ci` steps and gate the finish step on their outcome. Record the added `ci` wall time.
- Source: general
- Disposition notes:

### M3 — Severity: nit — Status: open
- File: .github/workflows/workflow.yml:342
- Description: A `cancelled` stage result maps to `failed`. A cancelled run gets no end line (checkout is skipped and the finish step errors). If those steps ran, a superseded hit would log `false_skip=true`.
- Suggestion: Use `!cancelled()` on the finish step, or add a `cancelled` pipeline value with `rate=out false_skip=false`, plus a test.
- Source: general
- Disposition notes:

### M4 — Severity: nit — Status: open
- File: tools/dev-cli/services/green-key-ledger.cs:371
- Description: Carry candidates include pull request marker runs. Ganda's rule is green master runs. Pull request rows waste probes and can use up `CarryRunLimit`.
- Suggestion: Project `head_branch` in the jq and keep only master rows.
- Source: general
- Disposition notes:

### M5 — Severity: nit — Status: open
- File: .github/workflows/workflow.yml:119
- Description: The `green-key` job's `runs-on` has a `pull_request` clause that never matches, because of the job's `if`.
- Suggestion: Drop the clause.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None. Single reviewer.

## Not findings (recorded)

- The task Notes say "Steven's yes before any workflow edit lands". This walk will merge the workflow edit. Under the global launch rule, a human "work N" request authorizes the whole walk, including the merge. I treated the launch of this walk as that yes and did not raise a finding. The operator should confirm this at the merge gate if they meant a separate yes.

## Evidence

- No screenshots were listed for this review. None viewed.
- One targeted read outside the diff: `source/timewarp-nuru-devcli/content/any/endpoints/release-command.guards.cs:310-360`, to confirm M2 (release locate-run filters on `--status success`). No build, test, audit, or hook was run.
