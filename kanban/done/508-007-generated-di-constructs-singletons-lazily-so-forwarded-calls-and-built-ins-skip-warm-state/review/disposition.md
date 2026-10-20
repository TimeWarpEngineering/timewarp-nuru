# Disposition — task 508-007

**Date:** 2026-10-20
**Outcome:** clean
**Rounds:** 2
**Final open count:** 0

## Summary

Round 1 (general, effort 2) found M1: a singleton kept a per-run Scoped instance that the call disposed. The fix on this task moved that Scoped service to the call's command slot, gave the singleton its own untracked copy, and reports NURU056. Round 2 re-reviewed that delta and found no new defects. M1 is fixed.

## Exception log (if accepted-exceptions)

None.

## Escalations

- None.
