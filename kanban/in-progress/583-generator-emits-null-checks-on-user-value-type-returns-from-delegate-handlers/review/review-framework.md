# Review framework — task 583

**Date:** 2026-11-14
**Host task:** kanban/in-progress/583-generator-emits-null-checks-on-user-value-type-returns-from-delegate-handlers/
**Diff scope:** branch `task/583-generator-emits-null-checks-on-user-value-type` (`7a3c4f6a`) vs `master` (`560f013f`). Product delta is `master`...`HEAD` (two-dot matches three-dot). The value-kind commit is `b6c50b72`.
**Plan / brief:** `task.md` ## Goal (implementer restatement) and the original description. A non-nullable user value-type handler result must compile: semantic `ResultValueKind` on the return type and the problem-pair success type, no `is not null` / `?.` for that kind, enums print raw (`Primitive` in capabilities), user structs stay JSON with a plain `ToString()` fallback, `Nullable<T>` keeps the null guard.
**Effort:** round 1: 2 (general only). Round 2: 1 (general only)
**Reviewer roster:** general
**Session IDs:** round 1 cursor reviewer (Grok 4.7); turn cap 120 (`--max-turns`; cursor uncapped). Round 2 cursor reviewer (Grok 4.7); session not reported; turn cap 80 (no `--max-turns` flag; bound 80)

## Budget (by-diff)

Round 2 budget. The header diff scope above is round 1.

- Lines changed: 105
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Round 2 scope

**Date:** 2026-11-14
**Diff scope:** delta since round 1 base `7a3c4f6a` (`bec98676`, kitchen only). M1 re-checked on `origin/feature/generator-fixes-2026-11-14` (`75b4475f`) ... `HEAD` (`bec98676`), which is the task delta the wontfix names. Product paths did not change in this delta.

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
