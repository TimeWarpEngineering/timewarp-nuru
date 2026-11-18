# Review framework — task 591-001

**Date:** 2026-11-19
**Host task:** kanban/in-progress/591-001-option-value-and-value-tokens-are-swallowed-as-configuration-overrides-also-after/
**Diff scope:** branch `task/591-001-option-value-and-value-tokens-are-swallowed-as` vs `master` (three-dot). Product commits `2ba4b240`, `de263a3f`, `7701e88c`, plus the kanban land commit. 489 lines in the by-diff budget.
**Plan / brief:** Stop `IsConfigArg` from dropping tokens after `--`, and bind a declared long option's `--name=value` / `--name:value` form. See `task.md` Goal and the original description.
**Effort:** 2
**Reviewer roster:** general
**Session IDs:** reviewer grok `01a1f055-b7fd-7891-94b6-6728fcfd0c3a` (2026-11-19). Implementer session was not reported (claude-opus-5-5, 2026-11-18).

## Budget (by-diff)

- Lines changed: 489
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
