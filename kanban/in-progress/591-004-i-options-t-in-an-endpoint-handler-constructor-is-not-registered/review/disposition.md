# Disposition — task 591-004

**Date:** 2026-11-19
**Outcome:** accepted-exceptions
**Rounds:** 1
**Final open count:** 0

## Summary

One general reviewer at effort 2 read `cdbd12eb..eccf6d71` against the restatement and the task. The fix matches both: endpoint `Handler` constructor `IOptions<T>` is registered in the source-gen mediator container (and bound inline under `UseDirectInvoke()`) with the same section key and validator rule as delegate parameters; runtime-DI apps are untouched so `Configure<T>` still runs there; snapshot/monitor report NURU050 on source-gen apps only. No bugs or suggestions. Two nits were raised and accepted as wontfix.

## Exception log (if accepted-exceptions)

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M1 | nit | Unused default parameter `configurationExpression` on an internal emitter helper; generated code is correct. Clean up on next touch. | review oracle |
| M2 | nit | Results call generator-77 standalone-only; CI also compiles it into the multi assembly. Wording only; CI covers the run. | review oracle |

## Escalations

- None.
