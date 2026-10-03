# Disposition — task 375

**Date:** 2026-10-03
**Outcome:** accepted-exceptions
**Rounds:** 1
**Final open count:** 0

## Summary

One general reviewer (effort 3, roster axes: general) raised 2 bugs, 2 suggestions, and 2 nits. Both bugs and both suggestions are fixed on this task.
- The REPL listing now uses the profile the reader resolved, including JSON config.
- The tool accepts a positional profile name.
- The changelog documents that the built-in shadows a same-named app route.
- The catalog table is checked once.

Two nits are wontfix with the rationale below. After the fixes, repl-47 passed 18/18, CI tests passed 3738 with 0 failed, and `ganda repo audit` passed. The fixes were small and each was checked directly with a smoke run or a test, so no round 2 was opened.

## Exception log

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M5 | nit | The tool's error stays on stdout by design, and exit code 1 is the signal. | review oracle |
| M6 | nit | Accepting dash-prefixed values would let a value swallow the next flag. | review oracle |

## Escalations

- None.
