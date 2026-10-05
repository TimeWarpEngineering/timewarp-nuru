# Review framework — task 482-009

**Date:** 2026-10-05
**Host task:** kanban/to-do/482-009-stop-nuru-search-from-printing-a-json-array-after-the-listing/
**Diff scope:** commit `cb2561e3` on `task/482-009-stop-nuru-search-from-printing-a-json-array-after` (this task's commit only; not the stacked branch vs `origin/master`). Product files: `source/timewarp-nuru-search/endpoints/search-query.cs`, `source/timewarp-nuru-search/global-usings.cs`, `tests/timewarp-nuru-search-tests/search-05-search-query-output.cs`.
**Plan / brief:** Finding S-1 from task 482. `nuru search` printed the human listing and then the generated invoker serialized `SearchResult[]`. Default stdout must be only the listing. Keep a machine-readable mode if one already exists. Parent record: `kanban/done/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/review/supporting.md` S-1.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** grok 01a10be1-9df1-71f0-b11e-035c41517cc9 (2026-10-05)

## Budget (by-diff)

- Lines changed: 138
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
