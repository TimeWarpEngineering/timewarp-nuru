# Round 2 — general
**Date:** 2026-10-26
**Scope reviewed:** fix delta `993f24bd..31353fd4` (`fix(generator): bind RunAsync in source order; intercept app?.RunAsync`). Prior open findings M1, M2, M3, plus new defects on that delta. Not the whole branch.

## Summary

M1, M2, and M3 are fixed. A call the statement walk already bound is skipped, so a reassigned local no longer emits two interceptors for one site. `AppAt` binds a straight-line call to the last assignment before that call, which covers `codes.Add(await app.RunAsync(...))`. `app?.RunAsync(...)` is a member binding: a traced local is intercepted and a parameter reports NURU_I001. An extraction error suppresses NURU_I001 only in the same method or top-level statements, and generator-72 expects the parameter warning when the broken app is elsewhere.

One hole is new. `AppAt` uses the invocation's source position. A lambda or local function is not walked (`EvaluateExpression` returns null for a lambda; local functions are ignored), so a `RunAsync` inside one is bound only by that position. When the closure sits between two `Build()` assignments and runs after the later one, the call is intercepted as the earlier app. The build stays clean. Each app's interceptor runs that app's route table, so the later app's routes do not run. The restatement matches the task checklist; this is not a misread.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs:299
- Description: `AppAt` returns the last assignment whose span ends at or before `invocation.SpanStart`. That matches straight-line calls, and `BoundEntryPointCalls` keeps a call the walk already registered on its original app. It is wrong for a closure. `EvaluateExpression` does not enter a lambda (`dsl-interpreter.cs:615`), and `ProcessStatement` does not enter a local function (`dsl-interpreter.cs:528`), so those calls are bound only here. A lambda or local function written between two assignments captures the variable and usually runs later, after the second assignment. The intercept is still the first app. `[InterceptsLocation]` selects that app's `RunAsync_Intercepted` method, which executes that app's route table. No CS9153, no NURU_I001. generator-71's lambda test assigns the local once. The new reassignment tests use statement, parenthesized, and argument forms, which are not closures.

```csharp
NuruApp app = NuruApp.CreateBuilder().Map("a").WithHandler(() => "a").AsCommand().Done().Build();
Func<Task<int>> run = () => app.RunAsync(["b"]);
app = NuruApp.CreateBuilder().Map("b").WithHandler(() => "b").AsCommand().Done().Build();
await run();
```

The same bind happens for a local function declared between the two assignments.
- Suggestion: When the invocation is inside a lambda, anonymous method, or local function, and the captured variable was assigned more than one app, do not intercept it. Leave it for NURU_I001, the same rule `AppAt` already uses when every assignment is not the same app. Keep source-order binding for straight-line calls. Cover a lambda and a local function placed between two `Build()` assignments and invoked after the second: expect NURU_I001, and expect the single-app lambda in generator-71 to stay intercepted.
- Status: open
