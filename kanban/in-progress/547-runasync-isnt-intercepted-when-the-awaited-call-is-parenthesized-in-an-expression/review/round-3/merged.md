# Round 3 — merged findings
**Date:** 2026-10-26
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 3 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs:235
- Description: A reassigned local no longer puts one call site on two apps. Unchanged on this delta. Straight-line calls still bind in source order (`AppAt` uses the whole tree when the call is not in a closure).
- Suggestion: Bind each call to the app the receiver names at that point in source order. Do not copy a call the statement walk already registered onto a later app.
- Source: general
- Disposition notes: Fixed in `31353fd4`. Re-checked on `d627bb2e..20bd7c36`; the closure rule does not apply to straight-line calls.

### M2 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/locators/run-async-locator.cs:39
- Description: `TryGetMemberCall` treats `x?.M(...)` as a member call. Not touched on this delta.
- Suggestion: Treat a member binding under a conditional access as a `RunAsync` / `RunReplAsync` call.
- Source: general
- Disposition notes: Fixed in `31353fd4`.

### M3 — Severity: suggestion — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/nuru-generator.cs:315
- Description: NURU_I001 is suppressed only in the scope of a generator error. Not touched on this delta.
- Suggestion: Suppress NURU_I001 only for calls that belong to an app whose extraction already reported an error.
- Source: general
- Disposition notes: Fixed in `31353fd4` by scope.

### M4 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs:309
- Description: A lambda or local function between two `Build()` assignments is no longer intercepted as the earlier app. Source order counts only assignments inside the call's innermost closure. With none, different apps fall through to NURU_I001. generator-72 expects one NURU_I001 on each of those calls. A closure that assigns the captured local and then calls `RunAsync` in a shape the statement walk enters is bound by that walk, and the outer walk does not add a second site.
- Suggestion: Do not intercept a closure call when the captured variable was assigned more than one app; report NURU_I001. Keep source-order binding for straight-line calls.
- Source: general
- Disposition notes: Fixed in `20bd7c36` for the round-2 repro. A call the walk does not enter is M5.

### M5 — Severity: bug — Status: open
- File: source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs:329
- Description: The every-assignment fallback binds a closure call when no assignment inside the innermost closure is at or before the call, as long as this interpretation recorded one app. A `Build()` inside a lambda is a separate interpretation, so it does not see outer assignments, and `IsAssignedOutsideWalk` ignores assignments outside closures and assignments it already recorded. A nested lambda, or `RunAsync` passed as an argument before the closure's assignment, is intercepted as the closure's app. No CS9153 and no NURU_I001.
- Suggestion: Bind a closure call with no in-closure assignment before it only when every assignment of that symbol, including those outside this interpretation, is the same app. Otherwise leave it for NURU_I001. Cover the nested lambda and the argument-before-assignment shape.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- No overlap. One general reviewer. M1–M4 carried forward from round 2.
