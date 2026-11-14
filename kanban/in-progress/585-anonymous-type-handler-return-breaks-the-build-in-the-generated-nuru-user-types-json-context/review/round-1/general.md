# Round 1 — general
**Date:** 2026-11-14
**Scope reviewed:** `713a9cc3..6c4636ed` on `task/585-anonymous-type-handler-return-breaks-the-build-in`. Product commit `2a71a81c`.

## Summary

`ResultValueKinds.Of` marks a result Anonymous when the symbol is an anonymous type or contains one (type argument, array element, or containing type). `ValueReturnType` then names that result `object` (`Task<object>` when the handler is async), so the local function, the result local, and the `__invocation.Result` cast no longer write `<anonymous type: ...>`. `GetOutputStrategy` prints Anonymous as raw `ToString()`, and `CanBeNull` keeps the null guard. `GetReturnTypeName` returns null for that kind, so the JSON context task writes no `[JsonSerializable]` for it. `routing-40` covers the repro plus async, `List<anon>`, command, and null, and `## Results` records `./bin/dev build`, `./bin/dev test` (4705 passed, 0 failed, routing-40 5/5), and `ganda repo audit` after the runfile executable bit was set. The restatement matches the task: the extra invoker defect is the same unnameable display string the task already pointed at in the output emitter. No product defect.

## Issues

No issues.
