# Review framework — task 508-015

**Date:** 2026-10-20
**Host task:** kanban/to-do/508-015-mcphost-schema-fidelity-real-json-schema-for-non-scalar-members-contract-carried-annotations-one-defaults-convention/
**Diff scope:** branch `task/508-015-mcphost-schema-fidelity-real-json-schema-for-non-s` vs `master` (2 commits, 1199 lines). Product diff reviewed; kitchen `task.md` excluded from the product verdict.
**Plan / brief:** McpHost `tools/list` publishes the JSON schema of non-scalar members, contract-carried `title` / `destructiveHint` / `openWorldHint` with a host override, and one defaults convention (JSON `default` plus a description sentence). Nullable members follow the binder.
**Effort:** 3 (general only)
**Reviewer roster:** general
**Session IDs:** grok `01a15948-5cd1-7ed2-82e2-28012ac95836` (review, 2026-10-20). Implementer session was not reported.

## Budget (by-diff)

- Lines changed: 1199
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
