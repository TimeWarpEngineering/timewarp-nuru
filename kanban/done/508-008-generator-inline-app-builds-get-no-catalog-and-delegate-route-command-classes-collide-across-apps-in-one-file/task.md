# Generator: inline app builds get no catalog and delegate-route command classes collide across apps in one file

## Description

Two generator defects seen while writing tests for 508-003 and 508-005. Both bite only when a compilation holds more than one `NuruApp.CreateBuilder()…Build()` chain, which is exactly the shape of every test file and of the CI multi-mode assembly.

### A. Inline app builds get no capabilities catalog (508-005)

Two `CreateBuilder()…Build()` chains written inline inside test methods, one inside a `foreach` body and one whose app was only used with `ForwardAsync`, produced an app with no generated catalog. `app.Capabilities` threw "No generated capabilities catalog is attached to this NuruApp" (`source/timewarp-nuru/nuru-app.cs:72`). The identical chains returned from a helper method worked. The 508-005 tests use helpers as a workaround (`tests/timewarp-nuru-daemon-tests/`). Likely the `Build()` interceptor that attaches the catalog (508-002) is not emitted, or is emitted for a different interception site, when the chain sits inside certain statement shapes.

### B. Delegate-route command classes collide across apps (508-003)

When an `INuruBehavior` applies to a delegate (fluent `Map().WithHandler(...)`) route, the generator emits a command class named `__Route_{routeIndex}_Command` (`command-class-emitter.cs:90`). The name uses the global route index but the class is used with the per-app index, so two apps in one file collide when one of them has behaviors on delegate routes. The 508-003 behavior test uses a `[NuruRoute]` endpoint to avoid it.

## Requirements

- A: every `Build()` call in the compilation gets its catalog regardless of the enclosing statement (loop body, lambda, expression-bodied member, local function, `using` declaration). Add a generator test that builds three apps inline in one method, one in a `foreach`, and asserts each has a catalog with its own routes.
- B: command class names are unique per app and per route; two apps in one file with behaviors on delegate routes compile and dispatch correctly. Add a test with two such apps in one file.
- Record the root cause of A in Results; if it is an interception-site key problem, check whether `Map<T>()` and `AddMcpRoute()` / `AddServeRoute()` interceptors share it.

## Goal (implementer restatement)

Make the source generator correct when one compilation holds several `CreateBuilder()…Build()` chains.

- A: every app built inline (in a loop body, lambda, local function, expression-bodied member, `using` declaration, or only used via `ForwardAsync`) must get its own generated capabilities catalog, so `app.Capabilities` never throws for a generator-built app.
- B: the command classes emitted for delegate routes that have behaviors must be named per app and per route, so two apps in one file that both have behaviors on delegate routes compile and dispatch to their own handlers.

Acceptance:
- A generator test builds three apps inline in one method (one inside a `foreach`) and asserts that each catalog lists only that app's routes.
- A test with two apps in one file, both with behaviors on delegate routes, compiles and dispatches correctly.
- Results record the root cause of A and whether `Map<T>()` / `AddMcpRoute()` / `AddServeRoute()` share it.
- The workarounds in the daemon tests and the 508-003 behavior test are removed, or Results says why they stay.
- Full CI passes after `ganda runfile cache --clear`.

## Checklist

- [x] Repro A as a failing test; find the emission gap
- [x] Fix A
- [x] Repro B as a failing test (two apps, behaviors on delegate routes, one file)
- [x] Fix B (app-scoped naming)
- [x] Remove the helper-method workaround in the daemon tests and the endpoint workaround in the 508-003 behavior test, or note why they stay
- [x] Full CI after `ganda runfile cache --clear`

## Review

- Round 1: Done

## Notes

- A filed from 508-005 task Results (2026-10-19); B filed from 508-003 task Results (2026-10-18). Both marked "worth its own task" by the implementers.
- Related: 508 (parent), 508-002 (catalog attach at `Build()`), 508-003, 508-005.

## Session

- Created: 3913282 (2026-10-19)
- 2026-10-21 implementer-claude (ganda task work): both repros failed with CS9153 (one RunAsync intercepted twice); fixed A in the DSL interpreter and B in the emitters; full CI green.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-21T03:05:49Z
- Review oracle: grok session 01a15c6c-1504-78a0-b012-51f7497ddb32 (2026-10-21). Round 1, effort 2, roster general. Disposition clean.
- Review oracle: review by reviewer-grok (grok, model grok-4.7), session not reported, max-turns 120 — 2026-10-21T03:15:25Z

## Results

### Root cause of A

This was not an interception-site key problem. It was app selection in the DSL interpreter.

- `AppExtractor.ExtractFromBuildCall` runs once per `Build()` call. It interpreted the whole containing method body and then took `models[0]`, the **first** app built in that method, whatever `Build()` call triggered the extraction. With two apps in one method, the second `Build()` produced a copy of the first app's model (its routes, its `BuildSite`, its `RunAsync` sites) under the second call's location. The second app got no `Build()` interceptor, so it had no catalog. When both apps called `RunAsync`, the two models claimed the same call and the build failed with CS9153.
- `DslInterpreter.ProcessStatement` walked only declarations, expression statements, `return` and `try`. It skipped `foreach`, `for`, `while`, `do`, `if`, `using (...) { }`, `lock`, `switch`, nested blocks and the rest. An app built in one of those bodies was never seen.
- A `Build()` in an expression lambda, a method argument or an expression-bodied member was not reached either. With no method block, the extractor fell back to the file's top-level statements, which never contain that call.

The 508-005 cases match this: a second app in one method used only with `ForwardAsync`, and an app built in a `foreach` body. Helper methods worked because each helper builds exactly one app.

`Map<T>()`, `AddMcpRoute()` and `AddServeRoute()` are not intercepted, so they have no site key. They are interpreted onto the app builder, so the same selection bug credited them to the first app in the method. The same fix covers them. The generator intercepts only `Build()`, `RunAsync()` and `RunReplAsync()`.

### Fix A

- `DslInterpreter.InterpretBuildCall(scope, buildInvocation)` replaces `InterpretWithDiagnostics` and `InterpretTopLevelStatementsWithDiagnostics`. The interpreter records which `Build()` invocation built which app (`BuildCalls`) and returns the app built by **this** call. If the walk never reaches the call, it evaluates the call itself using the state the walk produced (expression lambdas, arguments, expression-bodied members).
- `AppExtractor.ExtractFromBuildCall` passes the method block, the compilation unit (only when the call is in a top-level statement), or null for an expression-bodied member.
- `ProcessStatement` now walks nested blocks, `if`/`else` (both branches), `foreach`, `for`, `while`, `do`, `using` statements (declaration and body), `lock`, `fixed`, `checked`, `unsafe`, labeled statements and `switch` sections. Local functions are still their own scope.

### Fix B

Command classes for delegate routes are emitted per app, indexed like that app's route matcher (`RoutesInIndexOrder`). They are named `__Route{methodSuffix}_{routeIndex}_Command`. With a single app the suffix is empty, so the name `__Route_0_Command` and `BehaviorContext.CommandTypeName` do not change. With several apps the names are `__Route_0_0_Command`, `__Route_1_0_Command` and so on. `BehaviorEmitter.EmitPipelineWrapper` takes the app's `methodSuffix` from the route matcher and JSON-args callers.

### Tests and workarounds

- New `tests/timewarp-nuru-tests/generator/generator-61-inline-app-builds.cs` (5 tests): three apps inline in one method with one in a `foreach`; two inline apps with the same route that dispatch to their own handlers; `if`/`using`/`lock`/`for` bodies; expression and block lambdas, local functions and a method argument; expression-bodied method and property. Each test asserts that the catalog lists only that app's routes.
- New `tests/timewarp-nuru-tests/generator/generator-62-delegate-behavior-command-classes.cs`: two apps in one method, both with `AddBehavior` on delegate routes of different shapes. They compile, dispatch, and give the behavior each app's own command instance.
- The 508-003 behavior test (`routing-34` `Should_run_nuru_behaviors_around_the_handler`) now uses a delegate route. The `JbiTracedQuery` endpoint workaround is deleted.
- Daemon tests (`daemon-01-server.cs`): the second app used only with `ForwardAsync`, and the app built in the `foreach` body, are now inline chains. The `CreateServedApp`/`CreateTerminalApp`/`CreateOtherBuildApp` and `daemon-02` `CreateApp` helpers stay. They are shared fixtures used by about 20 tests, and inlining them would only copy the chains.

### Notes for review

- Walking both `if` branches means the generator sees every app and route the code *can* build, which is the static view the interceptors need. A builder that maps the same route in both branches would now get a duplicate-route diagnostic. No existing test does this.
- When several apps share one method, each `Build()` extraction still interprets the whole method, so a diagnostic in that method is collected once per app. That was already true before this change.

### Verification

- Both repros failed before the fix: CS9153 "The indicated call is intercepted multiple times".
- `ganda runfile cache --clear` then `dotnet run tests/ci-tests/run-ci-tests.cs`: multi-mode 2079 total, 2073 passed, 6 skipped, 0 failed; standalone 34 files, 0 failed.
- `generator-61` 5/5, `generator-62` 1/1, `routing-34` 10/10, `daemon-01-server` 19/19, each run on its own.
- `ganda repo audit`: passes all checks, nothing to fix.

### How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/generator/generator-61-inline-app-builds.cs
dotnet run tests/timewarp-nuru-tests/generator/generator-62-delegate-behavior-command-classes.cs
dotnet run tests/ci-tests/run-ci-tests.cs
```

Expect: generator-61 prints 5 passed and generator-62 prints 1 passed. CI prints 0 failed for both multi-mode and standalone. The generated `artifacts/generated/generator-62-delegate-behavior-command-classes.cs/.../NuruGenerated.g.cs` declares `__Route_0_0_Command`, `__Route_1_0_Command` and `__Route_1_1_Command`, and each app's behavior code constructs its own class.

### Review

Round 1, effort 2, roster general. Disposition: **clean**. Final counts: 0 open, 0 fixed, 0 wontfix (bug, suggestion, and nit).

The implementer restatement matches the original task. No misread.

- Framework: `review/review-framework.md`
- Round: `review/round-1/general.md`, `review/round-1/merged.md`
- Disposition: `review/disposition.md`

Reviewer re-ran generator-61 (5 passed) and generator-62 (1 passed) and checked the generated interceptors and command class names. Full CI was not re-run in the review.
