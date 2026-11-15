# Review framework — task 496

**Date:** 2026-11-15
**Host task:** kanban/in-progress/496-systemcommandline-to-nuru-migration-guide/
**Diff scope:** task commit `d5d7f8c8` (docs page, skill, overview and migrating-to-3.0 links, readme pointer, kitchen `task.md`). Merges of origin/master on this branch also bring tasks 497 and 520; those files are out of scope.
**Plan / brief:** `## Goal (implementer restatement)` in `task.md`, checked against the original Description, Requirements, and Validation list. Restatement matches the task. No misread.
**Effort:** 3 (general only)
**Reviewer roster:** general
**Session IDs:** print-mode review oracle; the launch prompt did not supply a vendor session id

## Budget (by-diff)

- Lines changed: 270
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

## Round 2

**Date:** 2026-11-15
**Diff scope:** fix commit `95dc80b8` since round-1 base `be66b07a` — the guide, `skills/tw-nuru-migrate-system-commandline/SKILL.md`, `glossary.md`, task 591 items 11–13, and this kitchen. Not the whole task diff. Tasks 497 and 520 stay out of scope.
**Plan / brief:** re-verify round-1 M1–M6 against `## Goal (implementer restatement)` and the post-fix diff. Restatement still matches the original task. No misread.
**Effort:** 2 (general only), from the budget above
**Reviewer roster:** general
**Session IDs:** print-mode review-2 oracle; the launch prompt did not supply a vendor session id. Cursor launch has no `--max-turns` flag; bound cap 120.
