# Round 1 — merged findings
**Date:** 2026-10-28
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/timewarp-nuru-analyzers/generators/emitters/behavior-emitter.cs:213
- Description: `UseTelemetry()` registers `TelemetryBehavior` for every app that traces or records metrics, so every fluent route now goes through `EmitCommandCreation`. A short-only flag's generated command property and matcher local use the short form (`V` / `v`, `I` / `i`, `T` / `t`). The initializer does not: it emits `Option = flag` for each flag. `build -v` and `docker run -i -t` under `UseTelemetry()` fail to compile. Those routes compiled before this change when the app did not add a behavior. A short option that has a `{parameter}` (`commit -m {message}`) still generates `Message = message`.
- Suggestion: Name the initializer property like `CommandClassEmitter` (`LongForm ?? ParameterName ?? ShortForm`) and the local like `RouteMatcherEmitter` for flags (`LongForm ?? ShortForm`). Cover `UseTelemetry()` plus `build -v` and `docker run -i -t` with a test that compiles, runs, and sees one span.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- One finding. No overlap.
