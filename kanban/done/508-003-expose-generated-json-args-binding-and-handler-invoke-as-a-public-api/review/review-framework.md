# Review framework — task 508-003

**Date:** 2026-10-19
**Host task:** kanban/to-do/508-003-expose-generated-json-args-binding-and-handler-invoke-as-a-public-api/
**Diff scope:** branch `task/508-003-expose-generated-json-args-binding-and-handler-inv` vs `master` (3 commits, `fd4181e8`)
**Plan / brief:** Expose the generated `--json-args` binder and handler invoke as `NuruApp.TryBind`, `InvokeAsync`, and `RenderAsync`, sharing one generated bind method per route.
**Effort:** 3 (general only)
**Reviewer roster:** general
**Session IDs:** grok `01a15050-f688-7a92-90d7-22dde54dec50`

## Budget (by-diff)

- Lines changed: 1477
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
