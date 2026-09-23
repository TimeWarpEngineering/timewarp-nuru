# Review framework — task 470-008

**Date:** 2026-09-23
**Host task:** kanban/to-do/470-008-fix-search-version-flag-and-fts-nul-crash/
**Diff scope:** branch `task/470-008-fix-search-version-flag-and-fts-nul-crash` vs `origin/master` — commits `d4124083` (folderize/results) + `7548dc6e` (product fix). Product paths: `source/timewarp-nuru-search/services/{database-path,search-index,search-index-json-context}.cs`, `source/timewarp-nuru-search/endpoints/search-query.cs`, `source/timewarp-nuru-search/global-usings.cs`, `tests/timewarp-nuru-search-tests/search-0{1,2,3,4}-*.cs`.
**Plan / brief:** Parent 470 M11 (`--version` prints `Endpoint.Kind` instead of CLI version), M12 (FTS NUL / C0 controls crash via unterminated string), M30 (world-readable `index.db`). Requirements: join `clis.version` onto SearchResult; strip/reject NUL/C0 in SanitizeFtsQuery and/or catch SqliteException; owner-only `~/.nuru` (0700) and `index.db` (0600) at creation; tests.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle (Composer / ganda task-work); general reviewer spawned via Task tool

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
