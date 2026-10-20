# Round 1 — merged findings
**Date:** 2026-10-20
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs:787
- Description: A scoped service that depends on a session or command service is late-scoped. Its process resolver is the same double-checked `__Resolve__svc_…()` used for singletons, and the generated body is `field ??= __TrackCommand(new …)`. `EndSingleRunServicesAsync` disposes `__commandDisposables` and then assigns that static field to null (line 819). `ServiceScopeExpressions.Read` in `ProcessInit` mode reads a scoped dependency through that same process resolver (`service-scope-expressions.cs` Scoped arm). A singleton constructor therefore runs during the call, builds the late-scoped service, and stores it. The call then disposes that graph. The next `ExecuteAsync` returns the same singleton, whose dependency is the previous call's object. A throwaway runfile with `AddCommandScoped<IToken>`, `AddScoped<IHolder>` (holder takes the token), and `AddSingleton<IRoot>` (root takes the holder) printed `rootToken=1 holderToken=1 sameHolder=True rootTokenDisposed=False` on the first `go`, and `rootToken=1 holderToken=2 sameHolder=False rootTokenDisposed=True` on the second. `DependsOnInvocationScope` still says these instances are built after process singletons. `daemon-05` and `generator-51` do not register a singleton that takes a late-scoped service, so they stay green.
- Suggestion: Keep the per-run instance on the call (`__cmd_` / session field) and clear that slot at the end of the run. Do not let a process-lifetime constructor (`ProcessInit`, singleton and non-late scoped resolvers) read that slot. A singleton whose dependency closure reaches a session or command service, including through scoped services, should report NURU056 the way a direct singleton → session/command dependency already does, and the generated singleton must not retain the per-run instance. Add a two-call test: the handler's late-scoped service is a new live instance, and the singleton does not observe the previous call's disposed token.
- Source: general
- Disposition notes: Fixed in implement-fix-2 (2026-10-20). A late Scoped service has no process field. It always reads its `__cmd_` slot on the call, which is tracked and cleared at run end. In `ProcessInit` mode, a per-run dependency is constructed inline and untracked, so a singleton keeps its own copy. NURU056 now reports Singleton → late Scoped. Covered by generator-57 (two-call runtime test) and generator-58 (NURU056).

## Duplicates / conflicts

- One reviewer. Nothing to collapse.
