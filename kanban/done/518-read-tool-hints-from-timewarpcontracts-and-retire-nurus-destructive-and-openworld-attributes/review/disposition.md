# Disposition — task 518

**Date:** 2026-10-23
**Outcome:** accepted-exceptions
**Rounds:** 2
**Final open count:** 0

## Summary

Round 1 (general, effort 2) found the change meets every Goal and acceptance item, with one
doc-comment indentation nit. Round 2 (general, effort 1, 0 lines) re-read that same tree. The pin is
still 2.0.0-beta.24. Contracts `Destructive` / `OpenWorld` are still read by metadata name with the
same tri-state rule. An operation's title is still `Operation.Title` only, and a route class with no
operation still keeps `[DisplayName]`. The Nuru attribute pair is still removed. A file that imports
both namespaces still compiles. Host overrides still win. The changelog still records the break. The
518 product files are unchanged since `a58b48fc`, so round 2 did not re-run the round 1 smoke checks.
No new findings. M1 stays wontfix.

## Exception log

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M1 | nit | Whitespace inside a doc comment only. The build and format accept it, and there is no behavior change. A fix pass plus another review round costs more than the nit is worth. Fix it in passing. Round 2 confirmed the lines and did not reopen the wontfix. | review oracle (orchestrator), round 1; affirmed round 2 |

## Escalations

- None.
