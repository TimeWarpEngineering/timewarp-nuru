# Disposition — task 482-006

**Date:** 2026-10-05
**Outcome:** clean
**Rounds:** 2
**Final open count:** 0

## Summary

Round 1 (general, effort 2) found two bugs. M1: `generator-53` was excluded from the CI multi assembly and was not on the `run-ci-tests.cs` standalone list, so CI never ran it. M2: `NURU_P010` showed doubled braces because Roslyn 5.6 `GetMessage` does not unescape `{{` when a diagnostic has no format arguments. Both were fixed on this task. Round 2 re-verified them and raised nothing new. Final counts: bug 0 open / 2 fixed / 0 wontfix. No suggestions, nits, or wontfix items.

## Exception log (if accepted-exceptions)

None.

## Escalations

None.
