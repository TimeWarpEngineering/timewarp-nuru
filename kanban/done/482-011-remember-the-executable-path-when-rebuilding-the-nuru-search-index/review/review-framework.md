# Review framework — task 482-011

**Date:** 2026-10-05
**Host task:** kanban/to-do/482-011-remember-the-executable-path-when-rebuilding-the-nuru-search-index/
**Diff scope:** commits `314951fc` and `4605c20a` on `task/482-011-remember-the-executable-path-when-rebuilding-the-n` (these commits only; the branch is stacked, so not `origin/master...HEAD`). Product files: `source/timewarp-nuru-search/endpoints/index-rebuild-command.cs`, `source/timewarp-nuru-search/endpoints/search-query.cs`, `source/timewarp-nuru-search/global-usings.cs`, `source/timewarp-nuru-search/services/search-index.cs`, `tests/timewarp-nuru-search-tests/search-06-index-rebuild-path.cs`. `4605c20a` only marks the runfile executable.
**Plan / brief:** Finding S-3 from task 482. Store the executable path that was actually run. `index rebuild --all` must invoke that path. `search --cli` still filters on the capabilities name. Parent record: `kanban/done/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/review/supporting.md` S-3.
**Effort:** 2 (general only; no specialist axes)
**Reviewer roster:** general
**Session IDs:** grok 01a10c04-80b0-7ab3-a658-313db8815faa (2026-10-05)

## Budget (by-diff)

- Lines changed: 274
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
