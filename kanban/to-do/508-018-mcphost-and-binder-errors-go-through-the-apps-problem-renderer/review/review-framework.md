# Review framework — task 508-018

**Date:** 2026-10-21
**Host task:** kanban/to-do/508-018-mcphost-and-binder-errors-go-through-the-apps-problem-renderer/
**Diff scope:** branch `task/508-018-mcphost-and-binder-errors-go-through-the-apps-prob` vs `master` (commits `68241c0a`, `ed6a3460`). Product diff is the feature commit plus executable bits on the two new runfiles. Kitchen edits are out of the product scope.
**Plan / brief:** When an app has `IResponseRenderer<SharedProblemDetails>`, McpHost and the forwarded daemon path render bind failures and escaped exceptions through it. CLI `--json-args` and argv binding errors do the same only after `UseProblemRendererForErrors()`. See `task.md` Requirements and `## Goal (implementer restatement)`.
**Effort:** 3 (by-diff, 1091 lines). Roster axis is general only.
**Reviewer roster:** general
**Session IDs:** grok `01a15ae4-96a0-7680-862d-bcce4c9319e0` (2026-10-21). Implementer session was not reported.

## Budget (by-diff)

- Lines changed: 1091
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
