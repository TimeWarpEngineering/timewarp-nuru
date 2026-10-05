# Review framework — task 482-006

**Date:** 2026-10-05
**Host task:** kanban/to-do/482-006-diagnose-invalid-route-patterns-instead-of-emitting-a-fallback-literal/
**Diff scope:** commit `50e2787f` (task product + kitchen) vs its parent. 222 insertions, 15 deletions (237 lines). Do not review the 482-005 commits that are also on this branch relative to a stale `origin/master`.
**Plan / brief:** Finding A-2 from task 482. `InvalidIdentifierError`, `InvalidModifierCombinationError`, and `AdjacentParametersError` must become NURU diagnostics, and a failed pattern must not be emitted as a fallback literal route. Patterns that already diagnose (unbalanced braces, bad option) must still diagnose and must not emit a route. Asserting tests for the three new errors.
**Effort:** 2 (general only)
**Reviewer roster:** general
**Session IDs:** grok 01a10ae6-22a2-73a2-8d03-84b40635c7d8 (2026-10-05)

## Budget (by-diff)

- Lines changed: 237
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

## Round 2

**Date:** 2026-10-05
**Scope:** Re-verify M1 and M2 on the fix delta only. Carry the same IDs. New defects only if the fix introduced them.
**Fix:** `generator-53` added to `tests/ci-tests/run-ci-tests.cs` `standaloneTests`. `NURU_P010` message uses `{0}` and `{1}`. `dotnet run tests/timewarp-nuru-tests/generator/generator-53-invalid-route-patterns.cs`: 5 passed.
