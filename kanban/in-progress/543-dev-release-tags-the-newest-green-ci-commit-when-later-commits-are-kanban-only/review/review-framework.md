# Review framework — task 543

**Date:** 2026-10-30
**Host task:** kanban/in-progress/543-dev-release-tags-the-newest-green-ci-commit-when-later-commits-are-kanban-only/
**Diff scope:** branch `task/543-dev-release-tags-the-newest-green-ci-commit-when-l` vs `master` (`a17d0a0b..03ff8dbb`). Product diff is `release-command.cs`, `release-guard.cs`, `props-version-reader.cs`, the guard-matrix and props-reader tests, `documentation/developer/guides/releasing.md`, and `changelog.md`.
**Plan / brief:** `dev release` step 8 tags the newest first-parent commit with a successful `workflow.yml` run when every commit after it changes only `kanban/`, bounded at 50 commits.
**Effort:** 2
**Reviewer roster:** general
**Session IDs:** general reviewer cursor session `7dce871f-a50c-41b9-b2b5-aefa4c4ad890` (2026-10-30). Implementer session `1d5be352-95f1-4e60-96b2-54c8f8eb134e`.

## Budget (by-diff)

- Lines changed: 537
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
