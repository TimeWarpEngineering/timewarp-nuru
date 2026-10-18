# Expose generated --json-args binding and handler invoke as a public API

## Description

`json-args-emitter.cs` already generates reflection-free binding from a JSON object to a route's request (exact `name` keys, ordinal comparison, unknown keys rejected, argv-overrides-JSON merge) and the invoker that runs the handler and renders the result. All of it is inline in the interceptor and generated app class. A host that receives JSON from somewhere other than argv (an MCP `tools/call`, a batch, a daemon request) has to write its own binder and cannot guarantee the same semantics.

Expose the binding and invocation as a generated public entry point, roughly:

```csharp
bool TryBind(string route, JsonElement args, out object request, out BindError error);
Task<object?> InvokeAsync(object request, CancellationToken ct);
```

Exact shape is open: a generated static on the app class, or an `INuruInvoker` registered in DI.

## Requirements

- Same semantics as `--json-args`: exact names, unknown keys error, type errors reported with the key, JSON `null` rules, arrays for repeated options and catch-alls.
- Binding does not go through argv parsing, the route matcher's literal handling aside (the route is named explicitly).
- Invoke runs the Nuru behavior chain and the handler, and returns the typed result rather than printing it, so a host can render it in its own protocol. A separate helper renders with the existing `OutputStrategy` when a host wants the CLI's text.
- Works in the REPL and under AOT. No reflection.
- Tests: parity between `--json-args` and `TryBind` for every binding case the emitter already covers.

## Goal (implementer restatement)

A host that gets a JSON object from somewhere other than argv (MCP `tools/call`, a batch, a daemon) can bind it to a named route and run that route in-process, with exactly the `--json-args` rules, without writing its own binder.

Acceptance criteria:

- `NuruApp.TryBind(route, JsonElement, out request, out error)` names the route by its catalog pattern (`EndpointCapability.Pattern`) and binds with the same generated code `--json-args` uses: exact ordinal keys, unknown keys rejected, wrong JSON kinds reported with the key, JSON `null` only for nullable members, arrays for repeated options and catch-alls, defaults kept for absent keys. No argv parsing, no reflection.
- The error is structured (`NuruBindError`: kind, key, message); its message is the text `--json-args` prints for the same input.
- `NuruApp.InvokeAsync(request, ct)` runs the same service setup, command scope, Nuru behaviors and handler as the CLI, and returns the handler's typed result (boxed) instead of printing it.
- `NuruApp.RenderAsync(request, result, terminal)` writes the CLI's text for that result (registered `IResponseRenderer<T>`, problem details to stderr, `OutputStrategy` otherwise) and returns the CLI exit code.
- The emitter generates one bind method per route that both `--json-args` and `TryBind` call.
- Works for apps that only call `Build()` (attached by the Build interceptor, as `Capabilities` is) and under AOT; `--json-args` in the REPL keeps working.
- Parity tests: every binding case the `--json-args` tests cover gives the same result (value or message) through `TryBind`/`InvokeAsync`.
- `agent-invocation.md` documents the API.

## Checklist

- [x] Public surface decided and documented
- [x] Emitter refactor so `--json-args` and the public path share one binder
- [x] Invoke returns typed result; render helper
- [x] Parity tests
- [x] Docs in `agent-invocation.md`

## Notes

- Parent: 508. 508-004 depends on this.
- Roslynk's `multi_query` batch binder (string/bool/int coercions, unknown keys fail) is the kind of per-app binder this replaces.
- Review: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`.

## Session

- Created: 1479989 (2026-10-17)
- Implemented (implementer-claude, headless task work): 2026-10-19
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-18T18:40:45Z
- Reviewer: grok session 01a15050-f688-7a92-90d7-22dde54dec50 (2026-10-19), effort 3, roster general

## Results

### Public surface

On `NuruApp` (core, `source/timewarp-nuru/nuru-app.cs`; types in `source/timewarp-nuru/invocation/`):

- `bool TryBind(string route, JsonElement arguments, out NuruBoundRequest? request, out NuruBindError? error)`. The route is the catalog pattern (`EndpointCapability.Pattern`, ordinal). `default(JsonElement)` binds no arguments; any other non-object is `NotAnObject`.
- `Task<object?> InvokeAsync(NuruBoundRequest request, CancellationToken ct = default)`: returns the boxed handler result (the `OneOf` for problem-pair endpoints, null for void/`Unit`), writes nothing.
- `Task<int> RenderAsync(NuruBoundRequest request, object? result, ITerminal terminal)`: the CLI's text (registered/declared `IResponseRenderer<T>`, problem details to stderr, output strategy) and the CLI exit code.
- `NuruBindError` (`Kind`, `Key`, `Message`) with `NuruBindErrorKind` (`UnknownRoute`, `NotAnObject`, `UnknownKey`, `MissingValue`, `WrongType`, `InvalidValue`, `UnexpectedArgument`). `Message` is the line `--json-args` prints.
- Plumbing marked `EditorBrowsable(Never)`: `NuruApp.JsonBinder` (`NuruJsonBinder` delegate), `NuruApp.InvocationExecutor`, `NuruInvocation`. Attached by the generated `Build()` interceptor and as a `??=` fallback at the start of route execution, like `CapabilitiesProvider`.

### Generator

- `JsonArgsEmitter.EmitBindMethods` emits, per route, a sealed `NuruBoundRequest` subclass holding the bound locals (and `__supplied_*` flags) and one static bind method. `--json-args` (`EmitBindCase`) and `TryBind` both call that method. `TryBind` passes the route's literal words as argv (group words, literals, `--`), so every member comes from JSON through the same attempt body: one binder.
- Failure sites now construct `NuruBindError` (kind + key + the unchanged message) instead of a string.
- `ExecuteRouteAsync` takes `NuruInvocation? __invocation`. When set, it runs after configuration, services and the command scope: render mode writes `Result` via `HandlerInvokerEmitter.EmitRender`; run mode unpacks the bound request, runs the missing-handler check, behaviors and handler with `captureResultInto` so the result is assigned instead of printed. Empty argv never counts as REPL auto-start on this path.
- `HandlerInvokerEmitter`: `captureResultInto` parameter, terminal expression parameter on the output/renderer emitters, `EmitRender`.
- An app with only `Map<T>()` endpoints that is built but never run now gets `ExecuteRouteAsync` too (otherwise `TryBind` had nothing attached).

### Verification

- `tests/timewarp-nuru-tests/routing/routing-34-json-bind-invoke.cs`: 10 tests. The parity test runs 35 cases (exact names, duplicate key, unknown key, wrong case, wrong kind, `null` for non-nullable and nullable, int range/fraction, enum, repeated option, required repeated option, catch-all, literals after a parameter, parameter between literals, `--` end-of-options, flags, typed repeated ints, endpoint defaults) through `--json-args` and through `TryBind`/`InvokeAsync`/`RenderAsync` and asserts identical text and outcome. Others: structured error kind/key, `default(JsonElement)`, typed result not written, Build-only app, `INuruBehavior` runs, problem pair returned and rendered with status 17, complex result renders as JSON, request from another app rejected, `AutoStartWhenEmpty` REPL app does not enter the REPL.
- `routing-33-json-args.cs` (19) unchanged and passing.
- Full CI (`dotnet run tests/ci-tests/run-ci-tests.cs` after `ganda runfile cache --clear`): multi-mode 1949 total, 1943 passed, 6 skipped, 0 failed; standalone 30 files, 0 failed.
- AOT: a throwaway runfile (deleted) published native from the tests tree with no IL/AOT warnings; `TryBind`/`InvokeAsync` returned `5:Int32`, `RenderAsync` printed `["x","y"]`, and wrong-type / unknown-key inputs returned the `WrongType`/`UnknownKey` errors.
- `ganda repo audit`: passes. Core builds with 0 warnings in Debug and Release.

### Boy-scout

- `source/timewarp-nuru/repl/key-bindings/key-binding-catalog.cs`: CA1830 (`Append(new string('-', n))` → `Append('-', n)`), which failed the core build when it was built from a runfile (the AOT smoke).
- `ganda repo audit` `runfile-executable`: the new `routing-34-json-bind-invoke.cs` runfile was committed without the executable bit; set it (commit 3cd44746). Audit passes and the tree is attested.

### Not done / notes

- Pre-existing, not fixed: delegate-route command classes (`__Route_{i}_Command`, used when an `INuruBehavior` applies) are named by the global route index but used with the per-app index, so two apps in one file collide when one of them has behaviors on delegate routes. The behavior test uses a `[NuruRoute]` endpoint to avoid it. Worth its own task.
- Calls into `InvokeAsync`/`RenderAsync` run one at a time (process-wide service state and `Environment.ExitCode`), documented. An MCP host (508-004) must serialize `tools/call`.
- The REPL runs commands through the same `ExecuteRouteAsync`; `InvokeAsync` uses `fromRepl: false` (a standalone command scope), like `RunAsync`.

### Review

Round 1, reviewer roster general, effort 3. Counts: bug 0 open / 0 fixed / 0 wontfix, suggestion 0, nit 0. Disposition: **clean**. No wontfix and no escalation. The implementer restatement matches the original task (the public shape was left open; the restatement names `TryBind` / `InvokeAsync` / `RenderAsync` and the shared binder).

Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.

Re-checked this round: `dotnet run tests/timewarp-nuru-tests/routing/routing-34-json-bind-invoke.cs` (Total 10, Passed 10) and `dotnet run tests/timewarp-nuru-tests/routing/routing-33-json-args.cs` (Total 19, Passed 19).

### How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/routing/routing-34-json-bind-invoke.cs
dotnet run tests/timewarp-nuru-tests/routing/routing-33-json-args.cs
```

Expect: `Total: 10 / Passed: 10` for routing-34 and `Total: 19 / Passed: 19` for routing-33. Full suite: `dotnet run tests/ci-tests/run-ci-tests.cs` reports 0 failed in multi-mode and standalone.
