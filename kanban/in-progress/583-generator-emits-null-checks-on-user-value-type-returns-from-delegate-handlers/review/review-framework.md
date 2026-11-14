# Review framework — task 583

**Date:** 2026-11-14
**Host task:** kanban/in-progress/583-generator-emits-null-checks-on-user-value-type-returns-from-delegate-handlers/
**Diff scope:** branch `task/583-generator-emits-null-checks-on-user-value-type` (`7a3c4f6a`) vs `master` (`560f013f`). Product delta is `master`...`HEAD` (two-dot matches three-dot). The value-kind commit is `b6c50b72`.
**Plan / brief:** `task.md` ## Goal (implementer restatement) and the original description. A non-nullable user value-type handler result must compile: semantic `ResultValueKind` on the return type and the problem-pair success type, no `is not null` / `?.` for that kind, enums print raw (`Primitive` in capabilities), user structs stay JSON with a plain `ToString()` fallback, `Nullable<T>` keeps the null guard.
**Effort:** 2 (general only)
**Reviewer roster:** general
**Session IDs:** cursor reviewer (Grok 4.7); turn cap 120 (`--max-turns`; cursor uncapped)

## Budget (by-diff)

- Lines changed: 493
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
