# Review framework — task 533

**Date:** 2026-11-14
**Host task:** kanban/in-progress/533-usetelemetry-ignores-a-non-literal-otlpendpoint-without-a-diagnostic/
**Diff scope:** task implementation commit `67931f0e` (parent `17bd2571`) on branch `task/533-usetelemetry-ignores-a-non-literal-otlpendpoint` vs base `master`. Review covers that commit only. Earlier commits on the branch are merges of tasks 530 and 575.
**Plan / brief:** `UseTelemetry` must warn when an option value or `ActivitySources.Add` / `Meters.Add` argument is not a compile-time constant (NURU_T001), and when the configure argument is not an inline lambda (NURU_T002). Literals and consts stay supported. See `task.md` Goal and Description.
**Effort:** 3 (by-diff, 834 lines). Roster axis is general only.
**Reviewer roster:** general
**Session IDs:** review session not reported (Cursor, Grok 4.7, turn cap 200)

## Budget (by-diff)

- Lines changed: 834
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
