# Round 1 — general
**Date:** 2026-10-26
**Scope reviewed:** branch `task/544-apps-in-one-compilation-share-the-first-apps-logge` vs merge-base `0c94984a`. Product commits `e8913db5` and `17b4a0ba`: per-app logger factory source, current-app field for shared code, fully qualified framework field names, generator-70, generator-66 back in multi-mode.

## Summary

`GetFirstLoggerFactoryFieldName` is gone. `GetLoggerFactorySource(model, appIndex)` returns that app's `__loggerFactory` / `__loggerFactory_N` when it calls `AddLogging`, `app.LoggerFactory` when it only enables telemetry logging, or null. `EmitLoggingFactoryFields` still emits the static factory with the same `GetLoggerFactoryFieldName(appIndex, length)`, so the name the app passes matches the field that was generated. `EmitAppInterceptorMethod` threads that source into `ExecuteRouteAsync`, route matching, and JSON bind/invoke. Delegate parameters and direct endpoint constructors resolve through `LoggerExpressions.Resolve`. A single-app `AddLogging` handler still emits `__loggerFactory.CreateLogger`, which is what generator-67, generator-68, and generator-69 assert.

Shared code uses `__nuruLoggerFactory`. It is emitted only when some app has a logging source, and `EnsureServicesInitialized` gains the factory argument only then. After the same-app early return, the locked init stores the argument, reassigns framework logger fields from that field, drops the previous app's service instances, nulls the mediator container when source-gen DI needs one, and replaces the `Lazy` for behaviors whose constructors take a logger. `ILogger<>` inside the mediator container is a singleton, and the container is rebuilt on app change, so it captures the factory just stored. `RunAsync_Intercepted` assigns `app.LoggerFactory` before `ExecuteRouteAsync`, so the telemetry snapshot on that path sees the factory. `NURU_H007` is unchanged: it still stays quiet when any app in the compilation configures logging.

`FrameworkServices.GetFieldName` names a generic framework field from the fully qualified type argument, with other characters replaced by `_`. `Gen69Service` stays `__fw_ILogger_Gen69Service`. Declaration and init skip a field name already emitted. generator-66 is off `CiTestExcludes` and off the standalone list. generator-70 runs three apps in one file (Information, Warning, none), logs from a delegate, a behavior, a mediator handler, and `Gen70.Alpha.Worker` / `Gen70.Beta.Worker`, and expects 12, 6, and 18 entries. Those counts match the sites, and running the later apps first fails if every app still used the first app's factory. generator-67/68/69 stay standalone; the task only requires generator-66, and their exclude comments are the existing Roslyn-hosted CS0433 reason.

The implementer restatement matches the task. No misread. `git diff --check` on the product diff is clean. generator-70 did not re-run here: the file-based host could not write its runfile cache in the sandbox, and an unsandboxed retry was not permitted. The test's assertions were checked against the emitters and the log-site counts.

## Issues

<!-- none -->
