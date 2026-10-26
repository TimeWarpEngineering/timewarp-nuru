# Round 1 — merged findings
**Date:** 2026-10-26
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 2 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs:175
- Description: `RegisterEntryPointCalls` runs after the scope walk and resolves an identifier or member access through the final `VariableState` entry (`dsl-interpreter.cs:254`). A later assignment to that local overwrites the entry. A call the statement walk already registered on the earlier app is also added to the later app (`AddInterceptSite` dedupes only within one builder), so one call site is emitted on two apps and the compiler rejects the duplicate intercept. A call the walk never enters is registered only on the last app, with no NURU_I001. Repro: one local, two `Build()` assignments, `RunAsync` after each assignment. The same overwrite happens when both arms of an `if` assign one local.
- Suggestion: Bind each call to the app the receiver names at that point in source order. Do not copy a call the statement walk already registered onto a later app. Expression contexts the walk skips need that ordered state. Add a regression for statement form and for `(await app.RunAsync(...))` / argument form.
- Source: general
- Disposition notes:

### M2 — Severity: bug — Status: open
- File: source/timewarp-nuru-analyzers/generators/locators/run-async-locator.cs:24
- Description: `IsPotentialMatch`, `LocateCall`, and `RegisterEntryPointCalls` (`dsl-interpreter.cs:218`) require a `MemberAccessExpressionSyntax`. `app?.RunAsync(args)` is a conditional access whose invocation is a member binding, so it is neither intercepted nor reported as NURU_I001, and it still throws "RunAsync was not intercepted" at runtime.
- Suggestion: Treat a member binding under a conditional access as a `RunAsync` / `RunReplAsync` call. Intercept it when the receiver traces to a built app; otherwise report NURU_I001. Cover a traced local and a parameter.
- Source: general
- Disposition notes:

### M3 — Severity: suggestion — Status: open
- File: source/timewarp-nuru-analyzers/generators/nuru-generator.cs:314
- Description: Any extraction error in the compilation suppresses every NURU_I001. That avoids a second warning on the broken app's own `RunAsync` (generator-72). It also hides an untraceable `RunAsync` on a parameter when a different app in the same compilation failed extraction.
- Suggestion: Suppress NURU_I001 only for calls that belong to an app whose extraction already reported an error. Still report it for a call no app intercepts. Extend generator-72 with a valid second app plus a parameter `RunAsync`.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- No overlap. One general reviewer.
