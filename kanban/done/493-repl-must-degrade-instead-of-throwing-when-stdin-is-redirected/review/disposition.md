# Disposition — task 493

**Date:** 2026-10-12
**Outcome:** accepted-exceptions
**Rounds:** 1
**Final open count:** 0

## Summary

One general reviewer at effort 2. No bugs. One suggestion and one nit, both accepted as wontfix:
the fix meets every requirement (line mode on redirect, no exception escapes, exit code propagated,
test with redirected input).

## Exception log (if accepted-exceptions)

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M1 | suggestion | Failure is surfaced (stderr + exit 1); narrowing needs a `ReplConsoleReader` refactor, out of scope | orchestrator |
| M2 | nit | Prompt in line mode matches the documented expected output; behaviour change belongs in its own task | orchestrator |

## Escalations

- none
