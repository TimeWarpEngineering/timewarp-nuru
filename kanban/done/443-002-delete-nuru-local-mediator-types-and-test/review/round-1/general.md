# Round 1 — general
**Date:** 2026-09-25
**Scope reviewed:** same as framework

## Summary

The change removes Nuru's duplicate mediator abstractions and points the generator at
`global::TimeWarp.Mediator.*`. Message-kind inference now matches only `TimeWarp.Mediator` interfaces and
applies Query > IdempotentCommand > Command independent of `AllInterfaces` order (also honouring the
`ICommand<T>, IIdempotent` marker form). Source-gen DI gains a lazily built `__GetMediator()` bridge
container so `ISender`/`IPublisher`/`IMediator` resolve for handlers, services and behaviors. Risk is low:
the Release build is clean, the full CI gate passes, and the precedence and DI paths have direct tests.
Verified independently: `dotnet build -c Release` 0/0, capabilities-07 7/7, generator-46 7/7,
`run-ci-tests.cs` exit 0, `ganda repo audit` passes, no `TimeWarp.Nuru.Unit` / Nuru message-interface
references remain outside kanban/changelog.

## Issues

### Issue 1 — Severity: suggestion
- File: source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs:395
- Description: `EmitSourceGenMediator` builds the bridge container from framework services, configuration,
  logging and `app.Services` only. Services that `ServiceResolverEmitter` synthesizes for Nuru handlers
  (`IOptions<T>` binding, `AddHttpClient` typed clients) are not registered, so a TimeWarp.Mediator handler
  that depends on them fails at runtime with a DI resolution exception under source-gen DI.
- Suggestion: register the options/HttpClient services in the bridge, or emit a diagnostic when a mediator
  handler depends on a type the bridge cannot supply.
- Status: open

### Issue 2 — Severity: nit
- File: source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs:503
- Description: `EnsureServicesInitialized` resets `__mediatorServiceProvider = null` per app instance without
  disposing the previous `ServiceProvider`.
- Suggestion: dispose the previous provider, or document why it is intentionally dropped.
- Status: open

### Issue 3 — Severity: nit
- File: source/timewarp-nuru/repl/repl-session.cs:328
- Description: the continuation lines of the `#pragma warning disable CA1031` justification comment were
  re-indented to column 4, visually detaching them from the pragma line.
- Suggestion: restore alignment or fold the justification into one comment line above the pragma.
- Status: open
