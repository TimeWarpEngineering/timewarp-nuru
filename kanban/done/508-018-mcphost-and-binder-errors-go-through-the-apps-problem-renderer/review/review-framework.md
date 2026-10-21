# Review framework — task 508-018

**Date:** 2026-10-21
**Host task:** kanban/to-do/508-018-mcphost-and-binder-errors-go-through-the-apps-problem-renderer/
**Diff scope:** branch `task/508-018-mcphost-and-binder-errors-go-through-the-apps-prob` vs `master` (commits `68241c0a`, `ed6a3460`). Product diff is the feature commit plus executable bits on the two new runfiles. Kitchen edits are out of the product scope.
**Plan / brief:** When an app has `IResponseRenderer<SharedProblemDetails>`, McpHost and the forwarded daemon path render bind failures and escaped exceptions through it. CLI `--json-args` and argv binding errors do the same only after `UseProblemRendererForErrors()`. See `task.md` Requirements and `## Goal (implementer restatement)`.
**Effort:** 3 (by-diff, 1091 lines). Roster axis is general only.
**Reviewer roster:** general
**Session IDs:** grok `01a15ae4-96a0-7680-862d-bcce4c9319e0` (2026-10-21). Implementer session was not reported.

## Budget (by-diff)

- Lines changed: 10
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Round 2

**Date:** 2026-10-21
**Diff scope:** `63545b11..c9dbc5c3` (fix pass for M1 and M2). Product paths only. Kitchen edits are out of the product scope.
**Session:** grok `01a15af5-910a-7262-b4f1-5b30952fb72c`

## Round 3

**Date:** 2026-10-21
**Diff scope:** `46138c91..b71d5d68` (fix pass for M3). Product path is `documentation/user/features/neutral-contracts.md` only. Kitchen edits are out of the product scope.
**Effort:** 1 (by-diff, 10 lines). Roster axis is general only.
**Session:** grok `01a15afc-48f9-7ad2-b5eb-d05f6ccf7746`

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
