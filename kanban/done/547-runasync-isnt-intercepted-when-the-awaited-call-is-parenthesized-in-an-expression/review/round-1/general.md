# Round 1 — general
**Date:** 2026-10-26
**Scope reviewed:** branch `task/547-runasync-isnt-intercepted-when-the-awaited-call-is` vs `master`. Generator intercept pass (`dsl-interpreter.cs`, `run-async-locator.cs`, `nuru-generator.cs`, `ir-app-builder.cs`), NURU_I001, generator-71, generator-72.

## Summary

The change does what the listed contexts ask. After the statement walk, `RegisterEntryPointCalls` finds `NuruApp.RunAsync` and `RunReplAsync` by method symbol inside parentheses, arguments, ternary branches, `ConfigureAwait`, lambdas, and expression-bodied members, and generator-71 exercises those shapes. NURU_I001 warns for a parameter and a method return, and it stays quiet for a method group and for an app that already has an extraction error.

Two holes remain. The new pass reads `VariableState` only after the whole scope has been walked, so a local that is assigned more than one built app binds every call to the last app. And both the registrar and NURU_I001 only recognize `receiver.RunAsync(...)` written as a member access, so `app?.RunAsync(...)` still throws at runtime with no warning. The restatement matches the task checklist; this is not a misread.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs:175
- Description: `RegisterEntryPointCalls` runs after `ProcessBlock` / the top-level walk, and `ResolveBuiltApp` maps an identifier or member access through the final `VariableState` entry for that symbol (`dsl-interpreter.cs:254`). The statement walk still registers a call against the builder stored at that statement (`DispatchRunAsyncCall`), then a later assignment to the same local overwrites `VariableState`. The end pass adds that same call site to the later app. `AddInterceptSite` dedupes only inside one builder, so one call is emitted on two apps. Roslyn rejects a call intercepted by more than one method, and a program that used to compile stops compiling. A call the walk never enters (an argument, or a parenthesized receiver of an unknown method) is registered only by the end pass, so it is intercepted as the last app and NURU_I001 does not fire. Sibling locals are fine (`generator-61` `first` / `second`); generator-71 uses one assignment per app, so the suite does not catch this.

```csharp
NuruApp app = NuruApp.CreateBuilder().Map("a").WithHandler(() => "a").AsCommand().Done().Build();
int first = await app.RunAsync(["a"]);
app = NuruApp.CreateBuilder().Map("b").WithHandler(() => "b").AsCommand().Done().Build();
int second = await app.RunAsync(["b"]);
```

The same overwrite happens when both arms of an `if` assign one local and each arm calls `RunAsync` on it. The walk visits both arms on purpose (`dsl-interpreter.cs:392`).
- Suggestion: Bind each call to the app the receiver names at that point in source order. Keep a call the statement walk already registered on its original app. Expression contexts the walk skips need that same ordered state, not a second scan of the final map. Add a regression with one local assigned two `Build()` results, in statement form and in `(await app.RunAsync(...))` / argument form, and assert each call runs the app assigned before that call.
- Status: open

### Issue 2 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/locators/run-async-locator.cs:24
- Description: `IsPotentialMatch`, `LocateCall`, and `RegisterEntryPointCalls` (`dsl-interpreter.cs:218`) all require `InvocationExpressionSyntax.Expression` to be a `MemberAccessExpressionSyntax`. `app?.RunAsync(args)` is a conditional access whose invocation expression is a member binding, so the call is not registered and NURU_I001 is not reported. The goal requires an ordinary `RunAsync` invocation to be intercepted, and a call that is not intercepted to be a compile-time warning rather than the runtime "RunAsync was not intercepted" throw. `app!.RunAsync` is covered; `app?.RunAsync` is the other nullable form and is still a runtime failure.
- Suggestion: Treat a member binding under a conditional access as a `RunAsync` / `RunReplAsync` call. The receiver is the conditional access expression; run it through `ResolveBuiltApp`. When it traces to a built app, intercept it. When it does not, report NURU_I001. Cover `app?.RunAsync(args)` on a traced local and on a parameter.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/timewarp-nuru-analyzers/generators/nuru-generator.cs:314
- Description: `ReportRunAsyncNotIntercepted` returns immediately when any extraction diagnostic in the compilation is an error. That matches generator-72 for a single app whose route is invalid (no second warning on that app's own `RunAsync`). It also drops NURU_I001 for every other call in the compilation, including `target.RunAsync(...)` on a `NuruApp` parameter next to an unrelated app that failed extraction. The acceptance criterion is that an untraceable `RunAsync` is a warning. A route error on a different app hides it, and the parameter call still throws at runtime.
- Suggestion: Suppress NURU_I001 only for calls that belong to an app whose extraction already reported an error. Still report it for a call that no app intercepts. Extend generator-72 with a second, valid app in the same compilation plus a parameter `RunAsync`, and expect NURU_I001 on the parameter only.
- Status: open
