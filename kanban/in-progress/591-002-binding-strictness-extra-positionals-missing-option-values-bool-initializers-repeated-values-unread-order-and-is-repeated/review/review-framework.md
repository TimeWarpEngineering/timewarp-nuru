# Review framework

## Budget (by-diff)

- Lines changed: 350
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-11-19
**Host task:** kanban/in-progress/591-002-binding-strictness-extra-positionals-missing-option-values-bool-initializers-repeated-values-unread-order-and-is-repeated/
**Diff scope:** branch `task/591-002-binding-strictness-extra-positionals-missing`, `cdbd12eb..HEAD`
(commits `dcde7dbb`, `9bd84bed`, `14104e13`, plus the kanban move `0e4de63e`). `cdbd12eb` is the
merge of PR #427 (task 591-003) that the branch was cut from; the 591-003 commits before it are out of scope.
**Plan / brief:** task.md `## Goal (implementer restatement)` items 1–5 and the original `## Description`.
**Effort:** 3
**Reviewer roster:** general (review oracle, Claude Opus 5.5)
**Session IDs:** review oracle session in this walk (headless `ganda task work`)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading
  the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to
  the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the
  command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
