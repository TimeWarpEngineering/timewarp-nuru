# Handlers under AddBehavior fail CS0103: unqualified inlined lambda types

## Description

One remaining generator bug when a route handler runs under `.AddBehavior(...)`; it produces code that doesn't compile (CS0103). Found by task 551 (PR #367) while writing `telemetry-04`.

The method-group `__command` bug originally filed here was fixed by 552 (`CommandClassEmitter.HasCommandClass` now includes `HandlerKind.Method`). What remains:

**Lambda body with an out-of-namespace type:** under `AddBehavior(...)`, the lambda body is inlined without qualifying type names, so `() => Tel04Capture.Probe()` fails CS0103 when `Tel04Capture` is in another namespace than the generated code expects.

## Checklist

- [ ] Reproduce in a generator test
- [ ] Inlined lambda bodies: emit fully qualified names via the SemanticModel (or don't inline; invoke the delegate), never syntax text
- [ ] Check the same inlining without behaviors, and with delegate handlers that take services
- [ ] Regression test, runnable in multi-mode

## Notes

- 2026-11-14 groom: dropped the method-group item; 552 shipped that fix.

## Session

- Created: 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-10-23)
