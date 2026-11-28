# Round 2 — merged findings
**Date:** 2026-11-28
**Sources:** general
**Scope:** fix-pass delta `85363b84..ac2b28c6`

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 3 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: kanban/in-progress/540-…/task.md
- Description: Deferred checklist items 3–7 had no tracking card.
- Disposition notes: Task 603 is on master (`kanban/to-do/603-read-nurus-green-key-shadow-window-and-enable-the-skip/`), depends on 540, and holds the five items. 540's checklist, Notes, and Results name it.

### M2 — Severity: suggestion — Status: fixed
- File: .github/workflows/workflow.yml:120, :330, :338, :351
- Description: A shadow ledger failure could fail CI and block release locate-run. The added ci time was not recorded.
- Disposition notes: The `green-key` job continues on error. The ci checkout and setup continue on error, and finish is gated on both. Results record about 26 s of added ci time with run links. Finish prints `ci-finish-seconds`.

### M3 — Severity: nit — Status: fixed
- File: .github/workflows/workflow.yml:352, :362; tools/dev-cli/services/green-key-log.cs:141
- Description: A cancelled stage mapped to `failed`.
- Disposition notes: The finish step has `!cancelled()`. `pipeline=cancelled` is `rate=out` and `false_skip=false`. Tests cover it.

### M4 — Severity: nit — Status: fixed
- File: tools/dev-cli/services/green-key-skip.cs:92; green-key-markers.cs:29
- Description: Carry candidates included pull request markers.
- Disposition notes: The jq projects `head_branch`. Only master rows are kept, and the test covers it.

### M5 — Severity: nit — Status: fixed
- File: .github/workflows/workflow.yml:121
- Description: The green-key runner expression had a dead `pull_request` clause.
- Disposition notes: Dropped, and the test asserts it is gone.

## New findings

None.

## Duplicates / conflicts

- None. Single reviewer.

## Evidence

- No screenshots were listed for this review. None viewed.
- Read-only inspection only: `git diff 85363b84..HEAD`, `.github/workflows/workflow.yml` (ci job), and `git show origin/master:kanban/to-do/603-…/task.md`. No build, test, audit, or hook was run.
