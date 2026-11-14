# Review framework — task 588

**Date:** 2026-11-15
**Host task:** kanban/in-progress/588-bump-microsoft-code-analysis-c-sharp-code-style-to-5-12-0-and-fix-the-ide0028-sweep/
**Diff scope:** branch `task/588-bump-microsoft-code-analysis-c-sharp-code-style-to` vs `origin/master` (merge-base `26e4191b`, HEAD `12b05e82`). 298 lines, 38 files. Product pin plus IDE0028 rewrites; kitchen `task.md` landed with the implementer.
**Plan / brief:** Bump `Microsoft.CodeAnalysis.CSharp.CodeStyle` 5.9.0 → 5.12.0 and fix every new IDE0028 in code with collection expressions. No pragma, NoWarn, or severity downgrade. Generator behavior stays the same.
**Effort:** 2 (by-diff). Roster axis is general only; no specialist reviewers.
**Reviewer roster:** general
**Session IDs:** not reported (review budget 120; this Cursor session is uncapped)

## Budget (by-diff)

- Lines changed: 298
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/` and the kitchen `task.md` Results, Session, and Learnings
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
