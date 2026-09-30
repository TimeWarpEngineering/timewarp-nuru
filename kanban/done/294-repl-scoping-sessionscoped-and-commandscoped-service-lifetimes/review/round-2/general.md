# Round 2 — general
**Date:** 2026-09-30
**Scope reviewed:** post-fix diff for M1–M4, plus `generator-51-repl-service-scopes.cs` (single-run, REPL, and `AutoStartWhenEmpty` on both DI paths).

## Summary

The REPL host is decided from `routeArgs` after config arguments are removed. `--interactive`, `-i`, and an empty route when `AutoStartWhenEmpty` is set skip the command scope on both DI paths. Source-generated DI uses `NuruAsyncLease.Idle` for that host and does not build process `AddScoped` fields. `NuruCommandLease` removes cache-owned session instances from the command scope's disposable list before the scope disposes. Scoped services that inject a session-scoped or command-scoped service are built in `BeginSingleRunServices` after the session exists, and again on each single-run so they see that invocation's instances. `generator-51` covers a scoped service that takes the session, and an auto-start REPL that must not construct an extra command instance.

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs:1129
- Description: Source-generated DI opened a single-run command scope for the REPL host.
- Suggestion: Skip that scope on the host.
- Source: general
- Disposition notes: `__enteringRepl` skips `BeginSingleRunServices` and uses `NuruAsyncLease.Idle`. `EnsureServicesInitialized` is called with `fromRepl || __enteringRepl`, so process scoped fields stay unbuilt. Re-checked: auto-start source-gen constructs one command instance (`NextId` 1).

### M2 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs:1117
- Description: `hostRepl` ignored `AutoStartWhenEmpty` and raw args that still contained config tokens.
- Suggestion: Use `routeArgs` and the auto-start flag.
- Source: general
- Disposition notes: `EnteringReplExpression` covers both. Re-checked: runtime auto-start runs the command instead of throwing, and constructs one command instance.

### M3 — Severity: bug — Status: fixed
- File: source/timewarp-nuru/dependency-injection/nuru-command-lease.cs:137
- Description: A command scope disposed session instances injected into scoped services.
- Suggestion: Drop those instances before the scope disposes.
- Source: general
- Disposition notes: `ReleaseSessionInstances` removes cache-owned instances from `_disposables`. Re-checked: runtime REPL `DisposedAtEntry` is `[0, 0]`, `Same` is `[true, true]`, and the session `DisposeAsync` count is 1.

### M4 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs:700
- Description: Single-run source generation built scoped services before session and command fields existed.
- Suggestion: Build them with the invocation services.
- Source: general
- Disposition notes: `DependsOnInvocationScope` splits those services out of process init. `BeginSingleRunServices` builds them after `EnsureSessionServices`, in topological order with command services, on every single-run. Disposable ones join `__commandDisposables`. Re-checked: source-gen `Same` is `[true, true]` across two `RunAsync(["check"])` calls.

## New findings

None.
