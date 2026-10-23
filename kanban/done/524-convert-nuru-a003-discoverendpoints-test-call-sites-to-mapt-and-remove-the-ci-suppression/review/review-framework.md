# Review framework — task 524

**Date:** 2026-10-23
**Host task:** kanban/to-do/524-convert-nuru-a003-discoverendpoints-test-call-sites-to-mapt-and-remove-the-ci-suppression/
**Diff scope:** branch `task/524-convert-nuru-a003-discoverendpoints-test-call-site` vs `master` (commit `dbf5f780`). Product paths: seven test files under `tests/timewarp-nuru-tests/`, `tests/ci-tests/Directory.Build.props`, `.agent/local/nuru-specific.md`.
**Plan / brief:** Convert the 49 unfiltered `DiscoverEndpoints()` call sites in the CI multi-mode assembly to `Map<T>()`, remove `NURU_A003` from `NoWarn`, and stop the nuru-specific agent note from saying the suppression remains.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review-oracle (ganda task work, tw-implementation-review, Cursor headless, session not reported, max-turns uncapped, review budget 80)

## Budget (by-diff)

- Lines changed: 185
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
