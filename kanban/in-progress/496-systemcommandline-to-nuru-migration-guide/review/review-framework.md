# Review framework — task 496

**Date:** 2026-11-15
**Host task:** kanban/in-progress/496-systemcommandline-to-nuru-migration-guide/
**Diff scope:** task commit `d5d7f8c8` (docs page, skill, overview and migrating-to-3.0 links, readme pointer, kitchen `task.md`). Merges of origin/master on this branch also bring tasks 497 and 520; those files are out of scope.
**Plan / brief:** `## Goal (implementer restatement)` in `task.md`, checked against the original Description, Requirements, and Validation list. Restatement matches the task. No misread.
**Effort:** 3 (general only)
**Reviewer roster:** general
**Session IDs:** print-mode review oracle; the launch prompt did not supply a vendor session id

## Budget (by-diff)

- Lines changed: 1432
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
