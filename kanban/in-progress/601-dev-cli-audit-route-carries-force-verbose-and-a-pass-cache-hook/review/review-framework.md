# Review framework — task 601

**Date:** 2026-11-25
**Host task:** kanban/in-progress/601-dev-cli-audit-route-carries-force-verbose-and-a-pass-cache-hook/
**Diff scope:** branch `task/601-dev-cli-audit-route-carries-force-verbose-and-a` vs merge-base `2d2dde35` (master). Product commit `aecc579f`, executable-bit fix `902b3c4a`. Ahead 4, behind 1 (`9ce1bfff` lands the kanban move on master).
**Plan / brief:** Shared `dev audit` accepts `--force` and `--verbose,-v`, passes `DevAuditContext` to each check, offers a pass-cache hook plus `DevAuditTreeStamp`, and can run a named subset in process. Version 4.0.0-beta.18.
**Effort:** 3 (by-diff, 2603 lines). Roster axes: general.
**Reviewer roster:** general
**Session IDs:** review oracle Grok `01a213a5-5dae-7362-8a96-a98107ebf434`. Implementer session `23de9849-2434-422e-9c73-bfe581606347` (Claude).

## Budget (by-diff)

- Lines changed: 2603
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
