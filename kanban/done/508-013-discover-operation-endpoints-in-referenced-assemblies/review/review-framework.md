# Review framework — task 508-013

**Date:** 2026-10-21
**Host task:** kanban/to-do/508-013-discover-operation-endpoints-in-referenced-assemblies/
**Diff scope:** branch `task/508-013-discover-operation-endpoints-in-referenced-assembl` vs merge-base `master` (`718873714ef6d6fee25bfd7afd2a162ae6525665`). Three commits: implementation, kanban results, audit fix. 1696 lines changed.
**Plan / brief:** `task.md` original description plus `## Goal (implementer restatement)`. Opt-in discovery of `[Operation]` / `[NuruRoute]` endpoints from a referenced assembly, metadata defaults, handler visibility, and JSON context coverage.
**Effort:** round 1 was 3 (1696 lines); this round is 2 (217 lines, by-diff). Roster axes: general.
**Reviewer roster:** general
**Session IDs:** round 1 grok `01a15a6e-77a8-7502-85fc-a9aae6c58c66` (GANDA_SESSION 1134605); round 2 grok `01a15a8a-52e5-72d3-9dce-e2f0e1ae5d10`

## Budget (by-diff)

- Lines changed: 217
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Round 2 scope

Re-review of commit `11cc4e85` since round-1 base `ac001e9f`. The product delta is the M1 and M2 fix in the extractors plus generator-59. The budget above is this round. Round 1 reviewed the branch against `71887371` at effort 3.

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Restatement check

The implementer restatement matches the original task. The original asks for referenced `[Operation]` discovery and names four concerns (initializer defaults, nested handler visibility, the JSON context task, opt-in versus automatic discovery). The restatement picks the opt-in shapes and the defaults rule. That is the design for those concerns, not a misread.
