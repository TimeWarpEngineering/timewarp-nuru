# Disposition — task 470-007

**Date:** 2026-09-23
**Outcome:** accepted-exceptions
**Rounds:** 2
**Final open count:** 0

## Summary

Round 1 (general, effort 1) raised one bug (a 200 with an empty JSON object or empty `versions` array still read as "never published"), two suggestions, and one nit. The bug, the exception-wrapping suggestion, and the nit were fixed on this task id in commit 8cd05197 with five new test cases. Round 2 re-verified all three fixes as correct and complete, accepted the single wontfix, and found no new defects.

## Exception log

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M3 | suggestion | No release-side endpoint test for the id validation and fail-closed catch. `ReleaseCommand.Handler` step 7 sits behind several git-state preconditions that need a git fixture, and extracting the shared lookup loop is out of scope for this gate fix. The service-level failure contract both endpoints depend on is tested directly (check-version-05). Recorded in task Results for a later release-command refactor. | review oracle (Claude Fable 5.1); accepted by round-2 general reviewer |

## Escalations

- None.
