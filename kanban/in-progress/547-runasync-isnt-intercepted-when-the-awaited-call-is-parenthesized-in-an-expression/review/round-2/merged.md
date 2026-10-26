# Round 2 — merged findings
**Date:** 2026-10-26
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 2 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs:235
- Description: A reassigned local no longer puts one call site on two apps. `BoundEntryPointCalls` skips a call the statement walk already registered. `AppAt` binds a call the walk did not enter to the last assignment before that call in source order. generator-71 runs statement and parenthesized forms on each app and checks the handler output. generator-72 compiles `codes.Add(await app.RunAsync(...))` on each app with no CS9153 and no NURU_I001.
- Suggestion: Bind each call to the app the receiver names at that point in source order. Do not copy a call the statement walk already registered onto a later app.
- Source: general
- Disposition notes: Fixed in `31353fd4`. The if/else limit (a call after both arms bind to the later arm) is unchanged and was already how the statement walk behaved.

### M2 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/locators/run-async-locator.cs:39
- Description: `TryGetMemberCall` treats `x?.M(...)` as a member call and uses the conditional access's receiver. The entry-point pass, NURU_I001, and the field scan use it. `app?.RunAsync(...)` on a traced local is intercepted (generator-71). On a parameter it reports NURU_I001 (generator-72).
- Suggestion: Treat a member binding under a conditional access as a `RunAsync` / `RunReplAsync` call. Intercept it when the receiver traces to a built app; otherwise report NURU_I001.
- Source: general
- Disposition notes: Fixed in `31353fd4`.

### M3 — Severity: suggestion — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/nuru-generator.cs:315
- Description: NURU_I001 is no longer dropped for the whole compilation. `ReportRunAsyncNotIntercepted` suppresses it only when the call's `EntryPointScope` (the enclosing method, or the compilation unit for top-level statements) contains a generator error. generator-72 keeps a parameter call in another method and expects one NURU_I001 naming `target` while the broken app's error remains.
- Suggestion: Suppress NURU_I001 only for calls that belong to an app whose extraction already reported an error.
- Source: general
- Disposition notes: Fixed in `31353fd4` by scope, which is coarser than per app. A second untraceable call in the same method or the same top-level statements stays quiet while that error fails the build. Accepted: the named case (another member in the same compilation) warns, and a failed build does not run.

### M4 — Severity: bug — Status: open
- File: source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs:299
- Description: `AppAt` binds a `RunAsync` inside a lambda or local function by the closure's source position. Those bodies are not walked. Placed between two `Build()` assignments and invoked after the second, the call is intercepted as the earlier app, with no CS9153 and no NURU_I001. That app's interceptor runs its own route table.
- Suggestion: Do not intercept a closure call when the captured variable was assigned more than one app; report NURU_I001. Keep source-order binding for straight-line calls. Add a lambda and a local function between two assignments, invoked after the second.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- No overlap. One general reviewer. M1–M3 carried forward from round 1.
