# Round 1 — general
**Date:** 2026-09-30
**Scope reviewed:** branch `task/294-repl-scoping-sessionscoped-and-commandscoped-servi` vs `origin/master` — session and command lifetimes, both DI paths, REPL host, and `generator-51-repl-service-scopes.cs`.

## Summary

Option A is implemented for both DI paths, and the lifetime table holds for handler parameters that do not cross scopes. Three holes sit around the REPL host and around injecting a session instance into a shorter scope. The source-generated host still opens a single-run command scope. Runtime DI skips that scope only for exact `--interactive` / `-i` argument lists, so `AutoStartWhenEmpty` leaves the command cache active and the first command's `Begin` throws. A session service is registered as a transient, so a command scope disposes it when a scoped service injects it. Source-generated scoped services that inject session or command services are constructed before those fields exist.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs:1071
- Description: Source-generated DI calls `BeginSingleRunServices` whenever `fromRepl` is false. The `--interactive` / `-i` host invocation is `fromRepl: false`, so it constructs command-scoped services and process `AddScoped` instances for the whole REPL, then `EndSingleRunServicesAsync` runs when the host returns. Runtime DI already skips this with `hostRepl`.
- Suggestion: Treat the REPL host like `NuruCommandLease.Enter` does: do not begin a command scope, and do not initialize non-REPL scoped fields.
- Status: open

### Issue 2 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs:1068
- Description: `hostRepl` is true only when `args` is exactly `--interactive` or `-i`. `AutoStartWhenEmpty` starts the REPL from the same `ExecuteRouteAsync` on an empty route. Runtime DI has already called `NuruCommandCache.Begin`, so the first command's `Begin` throws `InvalidOperationException`. The check also uses raw `args` rather than `routeArgs`, so a config-only argument list does not count as empty.
- Suggestion: After config arguments are stripped, treat `--interactive`, `-i`, and (`AutoStartWhenEmpty` with an empty route) as the REPL host.
- Status: open

### Issue 3 — Severity: bug
- File: source/timewarp-nuru/dependency-injection/nuru-service-collection-extensions.cs:338
- Description: `AddSessionScoped` registers a transient whose factory returns the cached session instance. Microsoft.Extensions.DependencyInjection disposes every disposable transient resolved from a scope. A scoped service that injects a session service therefore disposes the session when the command scope ends. The next command receives that disposed instance. Handler parameters avoid this because they resolve session services from the root; constructor injection does not.
- Suggestion: Before the command scope disposes, drop session instances the cache owns from the scope's disposable list so `ResetAsync` remains the only dispose.
- Status: open

### Issue 4 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs:576
- Description: `EnsureServicesInitialized` constructs `AddScoped` services before `BeginSingleRunServices` assigns session and command fields. A scoped constructor that takes a session-scoped or command-scoped service reads a null field. The REPL path builds those lifetimes in one topological pass, so the same graph works there.
- Suggestion: Build scoped services that depend on session or command scope after those instances exist, and only on the first single-run so a later invocation does not replace the process-scoped instance.
- Status: open
