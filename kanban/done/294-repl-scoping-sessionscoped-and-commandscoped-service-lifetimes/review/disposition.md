# Disposition — task 294

**Date:** 2026-09-30
**Outcome:** clean
**Rounds:** 2
**Final open count:** 0

## Summary

Round 1 (general, effort 1) found four bugs: the source-generated REPL host opened a command scope, `AutoStartWhenEmpty` was not treated as that host, command scopes disposed injected session instances, and single-run scoped services were built before session and command instances existed. Round 2 re-checked the fixes. No open findings.

## Exception log (if accepted-exceptions)

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|

## Escalations

- None.
