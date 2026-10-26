# Round 2 — general
**Date:** 2026-10-26
**Scope reviewed:** branch `task/544-apps-in-one-compilation-share-the-first-apps-logge` vs merge-base `0c94984a`. Same product commits as round 1 (`e8913db5`, `17b4a0ba`). No product delta since round 1.

## Summary

`GetLoggerFactorySource(model, appIndex)` returns that app's `__loggerFactory` / `__loggerFactory_N` when it calls `AddLogging`, `app.LoggerFactory` when it only enables telemetry logging, or null. `EmitLoggingFactoryFields` still emits the static factory with `GetLoggerFactoryFieldName(appIndex, length)`, so the name passed into `EnsureServicesInitialized` is the field that was generated. `EmitMethodBody` is the only call site, and it passes `app, configuration, <source>` when any app in the compilation has a logging source, including `null` for an app that has none. Delegate parameters, direct endpoint constructors, and JSON bind/invoke receive that per-app source through `LoggerExpressions.Resolve`.

Shared code uses `__nuruLoggerFactory`. It is emitted only when some app has a logging source. After the same-app early return, the locked init stores the argument, reassigns framework logger fields from that field, drops the previous app's service instances, nulls the mediator container, and replaces the `Lazy` for behaviors whose constructors take a logger. `ILogger<>` in the mediator container is a singleton, and the container is rebuilt on app change, so it captures the factory just stored. `RunAsync_Intercepted` assigns `app.LoggerFactory` before `ExecuteRouteAsync`, so the telemetry snapshot on that path sees the factory. `NURU_H007` is unchanged: it stays quiet when any app in the compilation configures logging.

`FrameworkServices.GetFieldName` names a generic framework field from the fully qualified type argument, with other characters replaced by `_`. `Gen69Service` stays `__fw_ILogger_Gen69Service`. Declaration and init skip a field name already emitted. generator-66 is off `CiTestExcludes` and off the standalone list. generator-70's three apps (Information, Warning, none), five log sites, and counts of 12, 6, and 18 match the emitters, and running the later apps first fails if every app still used the first app's factory. generator-67/68/69 stay standalone; the task only requires generator-66.

The implementer restatement matches the task. No misread. Round 1 had no findings to re-open. `git diff --check` on the branch diff is clean. generator-70 was not re-run in this session.

## Issues

<!-- none -->
