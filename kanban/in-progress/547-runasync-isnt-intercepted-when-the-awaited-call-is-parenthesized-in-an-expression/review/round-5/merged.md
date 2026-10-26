# Round 5 — merged findings
**Date:** 2026-10-26
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 5 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs:235
- Description: A reassigned local no longer puts one call site on two apps. Unchanged on this delta. Straight-line calls still bind in source order.
- Suggestion: Bind each call to the app the receiver names at that point in source order. Do not copy a call the statement walk already registered onto a later app.
- Source: general
- Disposition notes: Fixed in `31353fd4`. Re-checked on `80af7f31..930090e9`; this delta does not change that path.

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
- Description: A lambda or local function between two `Build()` assignments is no longer intercepted as the earlier app. The source-order return inside the innermost closure is unchanged, so a closure that assigns the local and then calls `RunAsync` still binds on that walk alone.
- Suggestion: Do not intercept a closure call when the captured variable was assigned more than one app; report NURU_I001. Keep source-order binding for straight-line calls.
- Source: general
- Disposition notes: Fixed in `20bd7c36`. Re-checked on `80af7f31..930090e9`. `HasUnrecordedWrite` runs only after that source-order return.

### M5 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs:334
- Description: A closure call with no in-closure assignment before it now binds only when every recorded write is the same app and `HasUnrecordedWrite` finds no other write of that symbol in the file. The nested lambda is not intercepted as `make`'s app. The `RunAsync` argument before the closure's assignment is not intercepted as that later app. A bare `null` initializer stays unbound to an app, so one later `Build()` still traces the closure.
- Suggestion: Bind a closure call with no in-closure assignment before it only when every assignment of that symbol, including those outside this interpretation, is the same app. Otherwise leave it for NURU_I001. Cover the nested lambda and the argument-before-assignment shape.
- Source: general
- Disposition notes: Fixed in `53b3cc91`. Re-checked on `80af7f31..930090e9`. The null-literal ignore now includes the wrapped forms from M6.

### M6 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs:418
- Description: `IsNullLiteral` unwraps parentheses, `!`, and casts, then accepts `null`, `default`, and `default(T)`. A first write spelled `null!`, `(null)`, `default!`, or `(NuruApp?)null` is no longer an unrecorded app write, so a closure `RunAsync` after that write and one `Build()` is intercepted and generator-72 expects no NURU_I001. A cast of any other expression still counts as an unrecorded write. A wrapped null that follows a recorded app is stored as a null app, and the every-assignment rule still withholds the closure.
- Suggestion: Unwrap parentheses and `!` before the null or default test, and ignore a cast only when its operand is one of those. Cover `NuruApp app = null!` plus one `Build()` and a closure `RunAsync`: no NURU_I001.
- Source: general
- Disposition notes: Fixed in `930090e9`. Verified this round against `IsNullLiteral`, `RecordAssignment`, `HasUnrecordedWrite`, and `ReportRunAsyncNotIntercepted`.

## Duplicates / conflicts

- No overlap. One general reviewer. M1–M5 carried forward from round 4. M6 is fixed on this delta. No new findings.
