# Round 3 — general
**Date:** 2026-10-26
**Scope reviewed:** fix delta `d627bb2e..20bd7c36` (`fix(generator): don't bind a closure's RunAsync by source position`). Prior open finding M4, plus new defects on that delta. Not the whole branch.

## Summary

M4's repro is fixed. A `RunAsync` in a lambda or local function no longer takes the last assignment before that closure in source order. When every assignment of the local is outside the closure and they are not all the same app, the call is left for NURU_I001. generator-72 covers the lambda and the local function placed between two `Build()` assignments. A closure that assigns the local and then calls `RunAsync` in a shape the statement walk enters is bound by that walk, and the outer walk no longer adds a second site.

The new fallback still binds the wrong app for a closure call the statement walk does not enter. `AppAt` only counts an assignment inside the innermost closure. With none before the call, one recorded assignment is enough to intercept, and `IsAssignedOutsideWalk` ignores assignments this interpretation already recorded and assignments that are not themselves inside a closure. A `Build()` inside a lambda is its own interpretation, so it never sees the outer assignments. The restatement matches the task checklist; this is not a misread.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs:329
- Description: After M4, a call inside a closure uses the every-assignment fallback whenever no assignment inside the innermost closure is at or before the call. That fallback returns the only app this interpretation recorded. `FindContainingBlock` makes a `Build()` inside a lambda or local function its own interpretation, so outer assignments are absent from `AppAssignments`. `IsAssignedOutsideWalk` then skips an assignment it already recorded and skips an assignment that is not inside a closure (`dsl-interpreter.cs:347`), including a later assignment in the enclosing method. The statement walk does not enter a nested lambda, a local function, or the arguments of an ordinary call (`EvaluateInvocation` evaluates the receiver only), so `BoundEntryPointCalls` does not hide the fallback. The call is intercepted as the closure's app. No CS9153 and no NURU_I001. The handler that runs is that app's route table.

```csharp
NuruApp app = NuruApp.CreateBuilder().Map("a").WithHandler(() => "a").AsCommand().Done().Build();
Func<Func<Task<int>>> make = () =>
{
  app = NuruApp.CreateBuilder().Map("b").WithHandler(() => "b").AsCommand().Done().Build();
  return () => app.RunAsync(["c"]);
};
Func<Task<int>> run = make();
app = NuruApp.CreateBuilder().Map("c").WithHandler(() => "c").AsCommand().Done().Build();
await run();
```

`make`'s interpretation records only `"b"`. The call's innermost closure is the inner lambda, so that assignment does not count. The fallback still returns `"b"`. At the call, `app` is `"c"`.

The same fallback binds a call the walk does not enter when it sits before the closure's assignment. The argument is evaluated while `app` is still `"a"`, and the interceptor is `"b"`:

```csharp
NuruApp app = NuruApp.CreateBuilder().Map("a").WithHandler(() => "a").AsCommand().Done().Build();
Func<int> run = () =>
{
  int code = Check(app.RunAsync(["a"]));
  app = NuruApp.CreateBuilder().Map("b").WithHandler(() => "b").AsCommand().Done().Build();
  return code;
};
```

A direct `pending = app.RunAsync(...)` before that assignment still warns: the walk binds it on a builder this interpretation does not return. generator-72 does not cover either shape. The single-app lambda in generator-71 and the lambda that assigns and then returns `app.RunAsync` stay on the paths that already work.
- Suggestion: If the call is inside a closure and no assignment inside the innermost closure is at or before the call, bind it only when every assignment of that symbol is the same app, including declarations and assignments outside this interpretation and assignments the walk did not record. Otherwise leave it for NURU_I001. Keep source-order binding for an assignment inside the innermost closure before the call, and keep the single-app lambda intercepted. Cover the nested lambda above and the argument-before-assignment shape: expect NURU_I001, and expect no interceptor for `"b"`.
- Status: open
