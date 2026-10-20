# Round 1 — general
**Date:** 2026-10-20
**Scope reviewed:** branch `task/508-007-generated-di-constructs-singletons-lazily-so-forwa` vs `master` (emitter lazy resolvers, daemon-05, sample, served-apps doc, changelog)

## Summary

Source-generated DI now builds each cached service in a `__Resolve…()` accessor on first read. `EnsureServicesInitialized` only sets framework fields and clears process fields when the app instance changes. Built-ins, a parse error, and a forwarded call do not construct user singletons. `dotnet run tests/timewarp-nuru-daemon-tests/daemon-05-lazy-singletons.cs` passed 5 of 5 on this review.

The hole is a singleton (or any other process-lifetime constructor) that reads a scoped service whose own graph reaches a session or command service. That per-run instance is stored in the static field, tracked on the call, and disposed when the call ends, while the singleton keeps it. The second call uses a disposed dependency.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs:787
- Description: A scoped service that depends on a session or command service is late-scoped. Its process resolver is the same double-checked `__Resolve__svc_…()` used for singletons, and the generated body is `field ??= __TrackCommand(new …)`. `EndSingleRunServicesAsync` disposes `__commandDisposables` and then assigns that static field to null (line 819). `ServiceScopeExpressions.Read` in `ProcessInit` mode reads a scoped dependency through that same process resolver (`service-scope-expressions.cs` Scoped arm). A singleton constructor therefore runs during the call, builds the late-scoped service, and stores it. The call then disposes that graph. The next `ExecuteAsync` returns the same singleton, whose dependency is the previous call's object. A throwaway runfile with `AddCommandScoped<IToken>`, `AddScoped<IHolder>` (holder takes the token), and `AddSingleton<IRoot>` (root takes the holder) printed `rootToken=1 holderToken=1 sameHolder=True rootTokenDisposed=False` on the first `go`, and `rootToken=1 holderToken=2 sameHolder=False rootTokenDisposed=True` on the second. `DependsOnInvocationScope` still says these instances are built after process singletons. `daemon-05` and `generator-51` do not register a singleton that takes a late-scoped service, so they stay green.
- Suggestion: Keep the per-run instance on the call (`__cmd_` / session field) and clear that slot at the end of the run. Do not let a process-lifetime constructor (`ProcessInit`, singleton and non-late scoped resolvers) read that slot. A singleton whose dependency closure reaches a session or command service, including through scoped services, should report NURU056 the way a direct singleton → session/command dependency already does, and the generated singleton must not retain the per-run instance. Add a two-call test: the handler's late-scoped service is a new live instance, and the singleton does not observe the previous call's disposed token.
- Status: open
