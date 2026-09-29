# Disposition — task 457

**Date:** 2026-09-29
**Outcome:** accepted-exceptions
**Rounds:** 2
**Final open count:** 0

## Summary

Effort-1 general review of the `--json-args` recommendation. Four findings were fixed on this id: the `json-args` diagnostic is new, every `IsConfigArg` attached form is an error, built-ins that remain after the pair is removed do not read stdin, and `EmitNoMatch` is cited on `interceptor-emitter.cs`. The literal-count tie stays exit 1. That is the exception below.

## Exception log (if accepted-exceptions)

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M1 | bug | Do not replace the tie with silent `ComputedSpecificity` first-match. NURU_R003 (Error) already rejects a same-required-signature sibling such as `greet {name}` beside `greet {name} {title?}`. Two compiling routes that can both bind one object (`deploy {env}` and `deploy --env {env}` for `{"env":"prod"}`) must list both patterns and exit 1. Calls without `--json-args` keep today's matcher. | review oracle, round 2 |

## Escalations

- None
