# Round 1 — general
**Date:** 2026-10-26
**Scope reviewed:** branch `task/545-nuru-search-drops-all-its-log-output-because-it-ne` vs merge-base `4ee00816`. Product commit `8a5a4444`: nuru-search logging, `LoggerExpressions` for service fields, NURU_H007 sites, both `logging.md` copies, changelog, generator-69.

## Summary

`program.cs` calls `AddLogging` at `LogLevel.Warning` with `StandardErrorLoggerProvider`. That provider writes one `level: message` line per entry, plus the exception when there is one, through `TimeWarpTerminal.Default.WriteErrorLine`. The minimum level stays on the builder. Endpoint handler constructors already resolved loggers through `LoggerExpressions.Resolve` when `AddLogging` was present; `SearchQuery.Handler` picks that path up. `CapabilitiesClient` and `SearchIndex` did not: `EnsureServicesInitialized` initialized every `ILogger` / `ILoggerFactory` framework field with `FrameworkServices.GetInitExpression`, which always emitted `NullLoggerFactory`. Those fields now use `LoggerExpressions.Resolve` (the static `__loggerFactory` field, or `app.LoggerFactory` when only telemetry logging is configured). `RunAsync` assigns `app.LoggerFactory` before `ExecuteRouteAsync` calls `EnsureServicesInitialized`, so the telemetry snapshot sees the factory on the normal entry path.

`ReportLoggerWithoutConfigurationWarnings` now includes `Handler.ConstructorDependencies` and registered-service constructor types, still skips `ILoggerFactory`, and still warns once per category. Both `logging.md` copies drop the "does not warn yet" sentence and list registered service constructors. The changelog has a Changed entry for the breaking warning and Fixed entries for service loggers and search stderr. generator-69 expects the endpoint warning and the `NullLogger` expression without `AddLogging`, silence plus `__loggerFactory.CreateLogger` with it, the same for a service constructor, and the `app.LoggerFactory?.CreateLogger` assignment under `UseTelemetry()`. Those strings match `LoggerExpressions.Resolve` and the field name from `FrameworkServices.GetFieldName`. The file is on the CI exclude list and the standalone list, same as generator-68.

The restatement's "console provider" line is a misread of the checklist, recorded under `## Learnings`. The synchronous stderr provider is what the checklist asks for: warnings and errors on stderr, stdout left to the existing result and terminal lines. A console provider would queue those lines on a factory the generator never disposes.

The standalone generator-69 run did not start: the file-based host could not write its runfile cache, and an unsandboxed retry was not permitted. No `TimeWarp.Nuru.Search` binary is in the tree, so the `false` stderr hand check was not repeated. The assertions were checked against the emitter instead.

## Issues

<!-- none -->
