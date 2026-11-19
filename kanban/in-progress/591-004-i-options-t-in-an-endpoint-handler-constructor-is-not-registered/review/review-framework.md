# Review framework

## Budget (by-diff)

- Lines changed: 663
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 591-004

**Date:** 2026-11-19
**Host task:** kanban/in-progress/591-004-i-options-t-in-an-endpoint-handler-constructor-is-not-registered/
**Diff scope:** branch `task/591-004-i-options-t-in-an-endpoint-handler-constructor-is` vs `cdbd12eb` (merge of 591-003, the task's base): commits `c9bc948e..eccf6d71`, 16 files, +609/−54
**Plan / brief:** endpoint `Handler` constructor `IOptions<T>` binds from configuration like a Fluent delegate parameter; `Configure<T>` stays NURU052; snapshot/monitor report NURU050
**Effort:** 2 (by-diff budget), general only
**Reviewer roster:** general
**Session IDs:** review oracle (claude, opus-5.5, ganda task work), 2026-11-19

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading
  the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to
  the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the
  command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
