# Apps in one compilation share the first app's logger factory

## Description

When one compilation builds several Nuru apps, every app's `ILogger<T>` comes from the first app's logger factory. `InterceptorEmitter.GetFirstLoggerFactoryFieldName` picks one factory field for all apps, so app B logs through app A's `AddLogging` configuration (providers and minimum level), or gets a logger even though B configured none.

Single-app consumers are unaffected. The CI multi-mode test assembly is affected, which is why task 527's `generator-66-add-logging-injected-logger.cs` had to run standalone. Found by task 527 (PR #356).

## Goal (implementer restatement)

Every generated logger in a multi-app compilation must come from the app that is running, not from
whichever app the generator saw first. Handlers, endpoint constructors, service constructors,
behaviors, and the source-gen mediator's `ILoggerFactory` all follow the running app's `AddLogging`
factory (or its telemetry `app.LoggerFactory`), and an app with neither gets a NullLogger.

Acceptance:
- Per-app emit paths pass that app's own factory source; shared static paths (service framework
  fields, behavior Lazy fields, mediator container) read a factory set per app in
  `EnsureServicesInitialized`.
- An app with no logging gets NullLogger even when a sibling app configures logging; NURU_H007 unchanged.
- Generic framework service fields (`__fw_ILogger_*`) are named from the fully qualified type argument,
  so `A.Foo` and `B.Foo` do not clash.
- Regression tests: two apps with different minimum levels in one compilation; two same-named
  classes in different namespaces injecting `ILogger<T>`.
- generator-66 runs in the CI multi-mode assembly again; full CI suite green after a runfile cache clear.

## Checklist

- [x] Read the Purpose/Design regions of `InterceptorEmitter` and the per-app emit path
- [x] Each app resolves `ILogger<T>` from its own logger factory field (keyed by app, like other per-app state)
- [x] An app with no `AddLogging` gets no logger from another app (NullLogger behavior, NURU_H007 unchanged)
- [x] Regression test: two apps in one compilation with different minimum levels or providers each log through their own configuration
- [x] Logger fields are named by fully qualified type, not short class name, so `A.Foo` and `B.Foo` injecting `ILogger<T>` don't clash (noted by 545); regression test with two same-named classes in different namespaces
- [x] Move generator-66 back into the multi-mode assembly if it now passes there, removing its exclude
- [x] `ganda runfile cache --clear`, full CI suite green

## Notes

- 541, 546 and 545 (merged 2026-10-26) reworked this path: `LoggerExpressions`, parameterless `AddLogging()`, and service-constructor loggers in `EmitEnsureServicesInitialized`. Build on those; generator-67/68/69 run standalone and may also move back into multi-mode.
- Related: the multi-app codegen cost work (522, 524) and the multi-mode Jaribu/codegen design note follow-up.
- No trailing whitespace. Use `Map<T>()` in tests, not `DiscoverEndpoints()`.

## Session

- Created: 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-10-23)
- Scope added 2026-10-26 by cockpit 7dce871f (logger field name clash from 545)
- Implement 2026-10-26 (implementer-claude, ganda task work): per-app logger factory source, current-app
  factory for shared code, FQ framework field names, generator-70 added, generator-66 back in multi-mode.

## Results

**Per-app source.** `GetFirstLoggerFactoryFieldName` is gone. `InterceptorEmitter.GetLoggerFactorySource(model, appIndex)`
returns the app's own `__loggerFactory_N` (AddLogging), `app.LoggerFactory` (telemetry logging), or null. Each app's
`ExecuteRouteAsync` path (delegate handlers, endpoint handler constructors, JSON bind/invoke) uses it.

**Shared code.** Service framework fields, behavior `Lazy` fields, and the source-gen mediator container are emitted once
for all apps. They now read `__nuruLoggerFactory` (`LoggerExpressions.CurrentAppFactory`, nullable).
`EnsureServicesInitialized(app, configuration, loggerFactory)` sets it when the running app changes, and each app
passes its own source (or `null`). That extra parameter and field are emitted only when some app in the compilation
has a logging source, so output for compilations without logging stays the same. A behavior whose constructor takes a
logger is now a non-readonly `Lazy` and gets a new instance in `EnsureServicesInitializedLocked` when the app changes,
the way singleton services already do. Behaviors without a logger still keep one instance for the whole process.

**Field names.** `FrameworkServices.GetFieldName` names generic framework fields by the fully qualified type argument
(`__fw_ILogger_Gen70_Alpha_Worker`), with non-identifier characters replaced by `_`. Global-namespace types keep their
old names (`__fw_ILogger_Gen69Service`). Field declaration and init now dedupe by field name.

**Tests.**
- New `generator-70-per-app-logger-factory.cs` builds three apps in one file: Information, Warning, and no logging.
  Each one logs from a delegate handler, an endpoint constructor, two same-named singleton services
  (`Gen70.Alpha.Worker`, `Gen70.Beta.Worker`), and a behavior. It asserts that each entry reaches only its own app's
  sink, at that app's level, and that the app with no logging writes nothing. It runs in multi-mode.
- generator-69's expected strings now match the current-app field and the factory each app passes in.
- generator-66 is removed from `CiTestExcludes` and from the standalone list, and its Design region is updated.
  generator-67/68/69 stay standalone: they are Roslyn-hosted and hit CS0433 in multi-mode, which has nothing to do
  with this task.

**Gates.** `ganda runfile cache --clear` then `dotnet run tests/ci-tests/run-ci-tests.cs`: multi-mode 2120 total,
2114 passed, 6 skipped, 0 failed; standalone 40 files, 0 failed. `ganda repo audit` passes, with no fixes needed.

**Observation (not fixed here).** `(await app.RunAsync([...])).ShouldBe(0)` is not intercepted. A parenthesized await
receiver fails at runtime with "RunAsync was not intercepted". generator-70 uses `int x = await app.RunAsync(...)`
instead. This is worth its own task if consumers write that shape.

### How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/generator/generator-70-per-app-logger-factory.cs
dotnet run tests/timewarp-nuru-tests/generator/generator-69-endpoint-constructor-logger-warning.cs
dotnet run tests/ci-tests/run-ci-tests.cs
```

Expect: generator-70 shows 1/1 passed and generator-69 shows 5/5 passed. The CI run reports 0 failed in multi-mode,
and its summary table includes `PerAppLoggerFactory` and `AddLoggingInjectedLogger`. The standalone phase reports
0 failed. In `artifacts/generated/generator-70-per-app-logger-factory.cs/.../NuruGenerated.g.cs` each app calls
`EnsureServicesInitialized(app, configuration, __loggerFactory_0)`, `__loggerFactory_1`, or `null`, and
`__fw_ILogger_Gen70_Alpha_Worker` and `__fw_ILogger_Gen70_Beta_Worker` are declared separately.

