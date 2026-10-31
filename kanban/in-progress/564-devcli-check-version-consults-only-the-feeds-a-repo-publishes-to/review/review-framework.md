# Review framework — task 564

**Date:** 2026-10-31
**Host task:** kanban/in-progress/564-devcli-check-version-consults-only-the-feeds-a-repo-publishes-to/
**Diff scope:** branch `task/564-devcli-check-version-consults-only-the-feeds-a-rep` vs base `master` (merge-base `229f2697d9aee3d0bc2b2d254df94fa2e8eb00f9`, head `c1e1c816e821dd84ceb3d5773b3f50591b13e0f4`). Product diff is the declared-feed publish gate (`checkVersionConfig.feeds`, `PackageFeedsResolver`, `LookupAsync`, both commands), its tests, the DevCli readme, `documentation/developer/guides/releasing.md`, `.timewarp/dev.jsonc`, and removal of `packages: read` from the `release` job. Kanban `task.md` moves are out of the product review.
**Plan / brief:** `task.md` requirements and `## Goal (implementer restatement)`. Publish gates read only the feeds a repo declares. Absent `feeds` means nuget.org. An unknown name or a value that names no feed fails before any network call.
**Effort:** 3 (by-diff, 1004 lines). Roster axis is general only.
**Reviewer roster:** general
**Session IDs:** implementer c4618c42-96d0-4fc0-9256-9fff24ecff93 (claude). Reviewer session not reported (cursor, Grok 4.7). Turn cap 200; cursor uncapped.

## Budget (by-diff)

- Lines changed: 1004
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
