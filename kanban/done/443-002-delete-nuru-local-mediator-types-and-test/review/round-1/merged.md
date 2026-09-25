# Round 1 — merged findings
**Date:** 2026-09-25
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 1 |
| nit | 0 | 0 | 2 |

## Issues

### M1 — Severity: suggestion — Status: wontfix
- File: source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs:395
- Description: the source-gen `__GetMediator()` bridge container does not register `IOptions<T>` or typed
  `HttpClient` services, so mediator handlers depending on them fail at runtime under source-gen DI.
- Suggestion: register those services in the bridge or emit a diagnostic.
- Source: general
- Disposition notes: wontfix on this task. The requirement here is that `ISender`/`IPublisher` resolve under
  static DI, which is met and tested. Replacing the source-gen service graph (and this bridge) is task 444
  ServiceGen / ServiceResolverEmitter, listed as out of scope. Apps needing options/typed clients inside
  mediator handlers can use `UseMicrosoftDependencyInjection()` today. Decided by: review oracle.

### M2 — Severity: nit — Status: wontfix
- File: source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs:503
- Description: prior bridge `ServiceProvider` is dropped without disposal on re-initialization.
- Suggestion: dispose it.
- Source: general
- Disposition notes: wontfix. The bridge's transient factories return externally owned instances
  (`__fw_ITerminal`, `__fw_NuruApp`, singleton service fields); disposing the provider would dispose those
  (e.g. a caller's `TestTerminal`). Dropping the provider is the safe behaviour. Decided by: review oracle.

### M3 — Severity: nit — Status: wontfix
- File: source/timewarp-nuru/repl/repl-session.cs:328
- Description: `#pragma` justification comment continuation lines re-indented.
- Suggestion: restore alignment.
- Source: general
- Disposition notes: wontfix. The layout is produced by `./bin/dev format` (the repo format gate); a manual
  re-alignment would be reverted by the formatter. Cosmetic only. Decided by: review oracle.

## Duplicates / conflicts

- None (single reviewer).
