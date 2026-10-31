# Review framework — task 481

**Date:** 2026-10-31
**Host task:** kanban/in-progress/481-upgrade-timewarp-nuru-to-net-11/
**Diff scope:** branch `task/481-upgrade-timewarp-nuru-to-net-11` vs base `master` (merge-base `72f70740a924420a2e5dd5d141184622d9394be9`, head `12da89d2c4cceeefc3983fbf67d552413b5cd7bd`). Product commit `c97f4fb5cab45a72a0253c76e6e1d0dfa7c52ed5` (814 lines, 76 files). Kanban `task.md` moves are out of the product review.
**Plan / brief:** `task.md` requirements and `## Goal (implementer restatement)`. Move the repo from .NET 10 to .NET 11 and update dependencies, with documented holdbacks.
**Effort:** round 1 was 3 (by-diff, 814 lines). Round 2 is 1 (by-diff, 106 lines). Roster axis is general only.
**Reviewer roster:** general
**Session IDs:** implementer 09166cef-c421-4fd1-810d-9ef28addd8d8 (claude). Reviewer session not reported (cursor, Grok 4.7). Round 1 turn cap 200; round 2 turn cap 80; cursor uncapped.

## Budget (by-diff) — round 2

- Lines changed: 106
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

Round 1 budget: 814 lines, effort 3, roster general, turn cap 200.

## Round 2 scope

**Date:** 2026-10-31
**Diff scope:** fix commit `b021b79a065f2cd1e515aa7221155adfca04e0ce` since round-1 base `12da89d2c4cceeefc3983fbf67d552413b5cd7bd`. Product files: `benchmarks/timewarp-nuru-benchmarks/program.cs`, `changelog.md`.
**Re-review:** M1 and M2. New defects only on that delta.
**Session:** reviewer session not reported (cursor, Grok 4.7). Turn cap 80; cursor uncapped.

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/` and the kitchen `task.md` sections this oracle owns
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
