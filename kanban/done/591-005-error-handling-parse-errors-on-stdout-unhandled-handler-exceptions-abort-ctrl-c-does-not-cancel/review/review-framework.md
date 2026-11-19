# Review framework

## Budget (by-diff)

- Lines changed: 850
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Scope — task 591-005

**Date:** 2026-11-19
**Host task:** kanban/in-progress/591-005-error-handling-parse-errors-on-stdout-unhandled-handler-exceptions-abort-ctrl-c-does-not-cancel/
**Diff scope:** branch `task/591-005-error-handling-parse-errors-on-stdout-unhandled`, task commits `de5c941b..0d0e1dfe` (fix `2d572925` plus kitchen land). The 850-line budget count is against the older merge-base and includes 591-003/004 commits already on master; those are out of scope.
**Plan / brief:** `## Goal (implementer restatement)` in task.md: argument errors to stderr, a CLI `RunAsync` handler exception becomes stderr message + exit 1 (`OptionsValidationException` still propagates), Ctrl+C/SIGTERM cancel the handler token with `ProcessTerminationTimeout` (130/143), REPL keeps its own Ctrl+C.
**Effort:** 3 (roster axis general only)
**Reviewer roster:** general (review oracle, claude-opus-5-5)
**Session IDs:** review oracle under `ganda task work` (headless Claude Code, print mode)

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
