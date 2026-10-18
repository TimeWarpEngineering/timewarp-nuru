# Disposition — task 509

**Date:** 2026-10-19
**Outcome:** accepted-exceptions
**Rounds:** 1
**Final open count:** 0

## Summary

The zsh template now calls `_app` when `compinit` autoloads it (`loadautofunc`), and calls `compdef` when the script is sourced or eval'd. I checked the fix against the task and against the implementer restatement. The new completion-31 tests pass 4 of 4 locally. The pre-fix template fails the same autoload driver (`HITS:0`), so the tests guard against a regression. Round 1 raised one CI nit (M1), and it is accepted as wontfix. No bugs or suggestions were raised.

## Exception log (if accepted-exceptions)

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M1 | nit | The `Install zsh` step has no probe-mode `if:`. It is harmless because of the `command -v` guard. An apt failure on a release run fails before any push, and a rerun fixes it. Tidy it the next time `workflow.yml` changes. | review oracle |

## Escalations

- None.
