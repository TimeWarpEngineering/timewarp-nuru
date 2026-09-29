# Review framework — task 456

**Date:** 2026-09-29
**Host task:** kanban/to-do/456-check-version-warn-when-source-is-multiple-versions-ahead-of-last-release/
**Diff scope:** branch `task/456-check-version-warn-when-source-is-multiple-version` vs `origin/master` (merge-base `a56ab8509f97502e2f4b9d0e7de79f4963b1941e`, commit `6be8955c`)
**Plan / brief:** `check-version` reports an honest prerelease-increment distance after the source and latest NuGet lines, warns when that distance is greater than 1 (exit 0), and `--strict` exits non-zero for that warning only. Distance is defined only when major.minor.patch and the prerelease label match and only the numeric identifier differs.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review-oracle (ganda task work, tw-implementation-review, Cursor implementer-cursor profile, headless)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
