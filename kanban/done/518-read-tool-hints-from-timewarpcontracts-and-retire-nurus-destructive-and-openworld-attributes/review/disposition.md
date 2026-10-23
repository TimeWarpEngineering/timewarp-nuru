# Disposition — task 518

**Date:** 2026-10-23
**Outcome:** accepted-exceptions
**Rounds:** 1
**Final open count:** 0

## Summary

One general reviewer ran at effort 2. The change meets every Goal and acceptance item. The pin is
2.0.0-beta.24. Contracts `Destructive` / `OpenWorld` are read by metadata name with the same tri-state
rule. An operation's title is `Operation.Title` only, and a route class with no operation keeps
`[DisplayName]`. The Nuru attribute pair is removed. A file that imports both namespaces compiles. Host
overrides still win. The changelog records the break. The reviewer re-ran `mcphost-04` (6/6),
`mcphost-01` (16/16), and the calculator `--capabilities` after clearing the runfile cache. The only
finding is a doc-comment indentation nit, accepted as wontfix.

## Exception log

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M1 | nit | Whitespace inside a doc comment only. The build and format accept it, and there is no behavior change. A fix pass plus another review round costs more than the nit is worth. Fix it in passing. | review oracle (orchestrator) |

## Escalations

- None.
