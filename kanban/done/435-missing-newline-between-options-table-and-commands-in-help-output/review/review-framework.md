# Review framework — task 435

**Date:** 2026-09-29
**Host task:** kanban/to-do/435-missing-newline-between-options-table-and-commands-in-help-output/
**Diff scope:** branch `task/435-missing-newline-between-options-table-and-commands` vs `origin/master` (merge-base `0f390189014122c9a314a583d984fb57f5520fd2`, commit `a1649552`)
**Plan / brief:** Global `--help` leaves a blank line between the Options table and the first command heading. `HelpEmitter.EmitCommands` writes that line only after it decides a Commands or REPL section will be shown. `Should_separate_options_table_from_commands_heading` locks the options-table border, a blank line, then `Commands:`.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review-oracle (ganda task work, tw-implementation-review, Cursor implementer-cursor profile, headless)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
