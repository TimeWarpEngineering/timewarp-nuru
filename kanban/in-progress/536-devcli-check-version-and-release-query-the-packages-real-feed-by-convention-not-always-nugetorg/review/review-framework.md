# Review framework — task 536

**Date:** 2026-10-29
**Host task:** kanban/in-progress/536-devcli-check-version-and-release-query-the-packages-real-feed-by-convention-not-always-nugetorg/
**Diff scope:** branch `task/536-devcli-check-version-and-release-query-the-package` vs base `master` (merge-base `4b3df3020aa80553b586bd1508f8749407c667b1`, head `d29417f853629f32b5e1cb7f28454b5363330529`). Product diff is the DevCli two-feed publish gate, its tests, `workflow.yml` `packages: read`, and the releasing docs. Kanban `task.md` moves are out of the product review.
**Plan / brief:** `task.md` requirements and `## Goal (implementer restatement)`. Publish gates read nuget.org and the org GitHub Packages feed, with no new setting, and refuse when GitHub Packages cannot be checked.
**Effort:** 3 (by-diff, 1731 lines). Roster axis is general only.
**Reviewer roster:** general
**Session IDs:** implementer bb03761c-eba3-48f9-ba58-9e8f7995b48f (claude). Reviewer session not reported (cursor, Grok 4.7). Turn cap 200; cursor uncapped.

## Budget (by-diff)

- Lines changed: 1731
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/` and the kitchen `task.md` sections this oracle owns
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
