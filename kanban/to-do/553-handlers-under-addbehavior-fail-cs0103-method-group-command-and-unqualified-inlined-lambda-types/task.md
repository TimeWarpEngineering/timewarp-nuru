# Handlers under AddBehavior fail CS0103: method-group __command and unqualified inlined lambda types

## Description

Two generator bugs when a route handler runs under `.AddBehavior(...)`; both produce code that doesn't compile (CS0103). Found by task 551 (PR #367) while writing `telemetry-04`.

1. **Method-group handler:** `.WithHandler(SomeClass.Method)` together with `AddBehavior(...)` generates `Command = __command` where `__command` is never declared.
2. **Lambda body with an out-of-namespace type:** under `AddBehavior(...)`, the lambda body is inlined without qualifying type names, so `() => Tel04Capture.Probe()` fails CS0103 when `Tel04Capture` is in another namespace than the generated code expects.

## Checklist

- [ ] Reproduce both in a generator test
- [ ] Method-group handler + behavior: declare/pass the command object the behavior pipeline needs (match the lambda path)
- [ ] Inlined lambda bodies: emit fully qualified names via the SemanticModel (or don't inline; invoke the delegate), never syntax text
- [ ] Check the same inlining without behaviors, and with delegate handlers that take services
- [ ] Regression tests for both, runnable in multi-mode

## Session

- Created: 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-10-23)
