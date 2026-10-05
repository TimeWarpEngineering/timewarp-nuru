# Review framework — task 482-010

**Date:** 2026-10-05
**Host task:** kanban/to-do/482-010-match-nuru-search-group-filters-against-dotted-group-paths/
**Diff scope:** commit `f1816861` on `task/482-010-match-nuru-search-group-filters-against-dotted-gro` (this commit only; the branch is stacked, so not `origin/master...HEAD`). Product files: `source/timewarp-nuru-search/services/search-index.cs`, `source/timewarp-nuru-search/endpoints/search-query.cs`, `tests/timewarp-nuru-search-tests/search-03-search-index.cs`.
**Plan / brief:** Finding S-2 from task 482. `--group` must accept the dotted filter the capabilities filter documents (`docker.remote`) and match the stored space-joined `group_path` (`docker remote`) and its children on whole segments. A space-separated filter may keep working. Parent record: `kanban/done/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/review/supporting.md` S-2.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** grok 01a10bf2-0f25-71c1-a584-404e36a61679 (2026-10-05)

## Budget (by-diff)

- Lines changed: 172
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
