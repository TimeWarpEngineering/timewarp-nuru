# Disposition — task 591-003

**Date:** 2026-11-19
**Outcome:** accepted-exceptions
**Rounds:** 1
**Final open count:** 0

## Summary

One general reviewer (effort 2) reviewed branch HEAD 2c2dc94a against master. No bugs or
suggestions. The fix matches the goal restatement and the original task: root `--help` prints a
root default route's usage, description, parameters, options, and examples; description-only
default routes and named commands are unchanged; docs and the migrate skill describe the new
behavior. Recorded gates (build, help-11 6/6, help-01..09 regressions, audit) cover the change, and
CI runs the full suite on the PR. Two nits were accepted as wontfix.

## Exception log (if accepted-exceptions)

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M1 | nit | Formatting duplicated from route-help-emitter; same output today, extraction is a refactor beyond this fix | review orchestrator |
| M2 | nit | Several promoted default routes share one Parameters/Options table; rare, usage lines disambiguate | review orchestrator |

## Escalations

- None.
