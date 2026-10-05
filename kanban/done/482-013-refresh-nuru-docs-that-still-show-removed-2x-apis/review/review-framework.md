# Review framework — task 482-013

**Date:** 2026-10-05
**Host task:** kanban/to-do/482-013-refresh-nuru-docs-that-still-show-removed-2x-apis/
**Diff scope:** commits `b64d055a` and `46b1f6d0` on `task/482-013-refresh-nuru-docs-that-still-show-removed-2x-apis` (`bea516e3..HEAD`). 42 files, 3496 lines. Living docs, `[NuruRoute]` remarks, DevCli readme, and this kitchen `task.md`. Not the sibling task commits that this branch also contains, and not `documentation/posts/`.
**Plan / brief:** Refresh the D-1 pages so they teach the 3.0 surface named in `documentation/user/guides/migrating-to-3.0.md` and in `source/`. Parent findings: D-1, P-1, S-7, S-8, S-9, S-10, R-2 (guide only), R-7 (DevCli readme). Do not change stdout/stderr behavior. Do not rewrite `documentation/posts/`.
**Effort:** 3
**Reviewer roster:** general
**Session IDs:** Grok `01a10c26-ade6-7920-b50f-35c6419409bf` (2026-10-05, review oracle)

## Budget (by-diff)

- Lines changed: 3496
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
- Scope is this task's docs refresh. Do not reopen the parent 482 code review.
