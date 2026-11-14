# Review framework — task 586

**Date:** 2026-11-14
**Host task:** kanban/in-progress/586-route-parameter-with-the-built-in-string-type-constraint-generates-code-that-does-not-compile/
**Diff scope:** branch `task/586-route-parameter-with-the-built-in-string-type` vs merge-base `2c66bb2c` (master), HEAD `a2759831`. Product diff is the string-constraint emitter branch, the option-parsing skip, the catch-all variable helper, and `routing-38-string-type-constraint.cs`. Kanban `task.md` is in the same range and is not product code.
**Plan / brief:** `{name:string}` and `{name:string?}` are built-in constraints. The generator must declare the handler variable from the capture with no conversion, the same binding an untyped `{name}` / `{name?}` gets, including `--opt {value:string}` / `{value:string?}` and any casing of `string`.
**Effort:** 2
**Reviewer roster:** general
**Session IDs:** reviewer grok (Grok 4.7), body tw-implementation-review, print mode. Cursor conversation `473052f1-c7a9-4aea-97f4-a61d15ed9c8d`. Turn cap 120; cursor session uncapped (no `--max-turns` flag).

## Budget (by-diff)

- Lines changed: 337
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
