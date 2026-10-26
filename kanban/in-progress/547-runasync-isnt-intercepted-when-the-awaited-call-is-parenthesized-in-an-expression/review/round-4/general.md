# Round 4 — general
**Date:** 2026-10-26
**Scope reviewed:** fix delta `2ea4f9ac..53b3cc91` (`fix(generator): require every write of a captured app local to be known`). Prior open finding M5, plus new defects on that delta. Not the whole branch.

## Summary

M5's two repros are fixed. A closure call with no assignment before it inside its innermost closure now binds only when every write of that symbol in the file was recorded by this interpretation and all of those writes are the same app. A `Build()` inside `make` no longer intercepts the nested lambda, and a `RunAsync` argument that runs before the closure's own assignment no longer intercepts as that later app. generator-72 expects one NURU_I001 on each of those calls. A bare `null` initializer is ignored, so one later `Build()` still traces the closure call.

The new ignore list only matches a bare `null` or `default` node. `EvaluateExpression` already unwraps parentheses and `!` and then treats the value as null, and `RecordAssignment` drops that first write. `HasUnrecordedWrite` still sees it, so the fallback refuses a closure call whose only real app is the following `Build()`. The implementer restatement matches the task checklist; this is not a misread.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs:414
- Description: `IsNullLiteral` returns true only for a `null` literal, a `default` literal, or `default(T)`. `HasUnrecordedWrite` uses it for a declarator initializer (line 351) and an assignment's right-hand side (line 364). `EvaluateExpression` unwraps parentheses and the null-forgiving operator before the literal, and `RecordAssignment` does not record a null when the symbol has no earlier app, so these writes are absent from `AppAssignments`. On the fallback path `AppAt` then returns null. A closure `RunAsync` whose receiver is that one built app is reported as NURU_I001 and is not intercepted. A straight-line call after the `Build()` still binds by source order, because that path returns before `HasUnrecordedWrite`. generator-72 covers `NuruApp? app = null` and generator-71's lambda uses `app!.RunAsync` on a local initialized with `Build()`, so both stay green.

```csharp
NuruApp app = null!;
app = NuruApp.CreateBuilder().Map("work").WithHandler(() => "work").AsCommand().Done().Build();
Func<Task<int>> run = () => app.RunAsync(["work"]);
await run();
```

`NuruApp? app = (null);` and `app = default!;` do the same. Before `53b3cc91`, `IsAssignedOutsideWalk` did not look at these initializers, and the closure was intercepted as that `Build()`.
- Suggestion: Unwrap parentheses and `!` in `IsNullLiteral`, then accept a null or default literal, so `(null)`, `null!`, `(default)!`, and `default(NuruApp)!` are ignored. Ignore a cast only when its operand is one of those (`(NuruApp?)null`); a cast of any other expression still counts as an unrecorded write. Cover `NuruApp app = null!` plus one `Build()` and a closure `RunAsync` in generator-72: no compile error and no NURU_I001. Keep the bare `null` case.
- Status: open
