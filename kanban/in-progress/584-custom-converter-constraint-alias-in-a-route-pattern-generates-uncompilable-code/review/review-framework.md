# Review framework — task 584

**Date:** 2026-11-14
**Host task:** kanban/in-progress/584-custom-converter-constraint-alias-in-a-route-pattern-generates-uncompilable-code/
**Diff scope:** Product delta of `69ac1455` (plus executable bit `b649ae83`) against stack tip `a52e46d6` (`origin/feature/generator-fixes-2026-11-14`). HEAD is `2d49ff0f`. That product diff is the task change. The three-dot diff against `master` (`5d2da84d`) also contains the task 583 and 586 stack already on that feature branch; those files are not in `69ac1455`. Task 583 disposition accepted that stack.
**Plan / brief:** `task.md` ## Goal (implementer restatement) and the original description. A route constraint that names a custom converter's `ConstraintAlias` binds through that converter. A constraint the generator cannot resolve stops the build with a Nuru diagnostic and does not emit CS0103/CS0029.
**Effort:** 3 (general only)
**Reviewer roster:** general
**Session IDs:** cursor reviewer (Grok 4.7); session not reported; turn cap 200 (no `--max-turns` flag; bound 200)

## Budget (by-diff)

- Lines changed: 825
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
