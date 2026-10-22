# Generated UseTelemetry code fails with CS1061 without a Microsoft.Extensions.Logging using

## Description

An app that calls `UseTelemetry()` fails to build with CS1061 in `NuruGenerated.g.cs` unless the app itself has `using Microsoft.Extensions.Logging;` (and the OpenTelemetry logging namespace) in scope, usually as a global using. Users hit this; our tests do not.

Cause: `source/timewarp-nuru-analyzers/generators/emitters/telemetry-emitter.cs` emits extension-method calls on `ILoggingBuilder`:

```csharp
builder.SetMinimumLevel(global::Microsoft.Extensions.Logging.LogLevel.Information);   // line ~124
builder.AddOpenTelemetry(options => ...);                                             // line ~125
```

Extension methods resolve only through the usings in scope, and the generated file does not emit them. Everything else in the emitters is written fully qualified with `global::`, which is why only these calls break.

The tests hide it: `tests/Directory.Build.props` adds `<Using Include="Microsoft.Extensions.Logging" />` to every test project.

Found by the 517 implementer while building its timing harness (517 task, follow-up (c)).

## Requirements

- Emit these calls in static form with fully qualified types, for example `global::Microsoft.Extensions.Logging.LoggingBuilderExtensions.SetMinimumLevel(builder, ...)` and the OpenTelemetry logging extension class for `AddOpenTelemetry`. Resolve the exact declaring types from the referenced assemblies; do not guess the names. Check the rest of the telemetry emission (tracing, metrics, exporter setup) for other extension-method calls and fix them the same way.
- Do not fix it by emitting `using` directives into the generated file, since that can clash with user code.
- A regression test that compiles a `UseTelemetry()` app **without** the test projects' global `Microsoft.Extensions.Logging` using, and fails on today's master with CS1061. It may need its own project or a `<Using Remove=...>` so `tests/Directory.Build.props` does not mask it.
- Run `ganda runfile cache --clear` before the CI tests, since this changes generator output.

## Goal (implementer restatement)

An app that calls `UseTelemetry()` must build when its own code has no `using Microsoft.Extensions.Logging;`. Today the telemetry emitter calls `ILoggingBuilder` extension methods (`SetMinimumLevel`, `AddOpenTelemetry`) by instance syntax, and only the user's usings can bring those into scope, so such an app fails with CS1061.

Acceptance:

- The telemetry emitter writes every extension-method call in static form, with the declaring type spelled `global::`, using the types that the referenced assemblies actually declare. This covers logging, tracing, metrics, resource, and exporter setup. The fix must not add `using` directives to the generated file.
- A regression test compiles a `UseTelemetry()` app together with its generated code, with no logging using in scope, and asserts that the compile has no errors. The test must fail with CS1061 on unfixed master, and `tests/Directory.Build.props` must not mask it.
- The runfile cache is cleared, CI tests pass, and a changelog entry is added.

## Checklist

- [x] Reproduce CS1061 with an app that has no logging using
- [x] Emit the logging and OpenTelemetry extension calls in static, fully qualified form
- [x] Audit the other telemetry emission for unqualified extension calls
- [x] Regression test that is not masked by tests/Directory.Build.props
- [x] CI tests green; changelog entry

## Review

- Round 1: Done

## Notes

- Related: 517 (FlushTelemetryAsync no-provider delay), 520 (telemetry overhead benchmark, which will hit this when its apps have no global usings).

## Results

- `telemetry-emitter.cs` now emits every extension call in static form, with `global::` declaring types that come from reflection over OpenTelemetry 1.19.1, OpenTelemetry.Exporter.OpenTelemetryProtocol 1.19.1, and Microsoft.Extensions.Logging 10.0.12:
  - resource: `OpenTelemetry.Resources.ResourceBuilderExtensions.AddService`
  - tracing: `OpenTelemetry.Trace.TracerProviderBuilderExtensions.SetResourceBuilder` / `.Build`, `OpenTelemetry.Trace.OtlpTraceExporterHelperExtensions.AddOtlpExporter`. `AddSource` is an instance method.
  - metrics: `OpenTelemetry.Metrics.MeterProviderBuilderExtensions.SetResourceBuilder` / `.Build`, `OpenTelemetry.Metrics.OtlpMetricExporterExtensions.AddOtlpExporter`. `AddMeter` is an instance method.
  - logging: `Microsoft.Extensions.Logging.OpenTelemetryLoggingExtensions.AddOpenTelemetry` and `OpenTelemetry.Logs.OtlpLogExporterHelperExtensions.AddOtlpExporter`. `OpenTelemetryLoggerOptions.SetResourceBuilder` is an instance method.
- `SetMinimumLevel` cannot be called in static form. `Microsoft.Extensions.Logging.LoggingBuilderExtensions` is declared in both Microsoft.Extensions.Logging and Microsoft.Extensions.Logging.Configuration, and OpenTelemetry references both, so the static form fails with CS0433 (observed in the regression test). The emitter now calls `OptionsServiceCollectionExtensions.Configure<LoggerFilterOptions>(builder.Services, o => o.MinLevel = Information)`, which is what `SetMinimumLevel` does internally.
- `interceptor-emitter.cs` no longer emits the six `using global::OpenTelemetry.*;` directives. With every name fully qualified, nothing uses them, and removing them means OpenTelemetry names cannot clash with user usings copied into the generated file. No `using` was added.
- Regression test `tests/timewarp-nuru-tests/generator/generator-63-telemetry-without-logging-using.cs` is a Roslyn-hosted, in-memory compile of a `UseTelemetry()` app plus its generated code, where only `using TimeWarp.Nuru;` is in scope, so `tests/Directory.Build.props` cannot mask the bug. It asserts zero compile errors. Before the fix it failed with exactly the two CS1061 errors (`SetMinimumLevel`, `AddOpenTelemetry`). It is registered as a CI standalone test, the same way as generator-43 (CiTestExcludes plus the run-ci-tests.cs list).
- Changelog: entry under Unreleased / Fixed.
- Gates: ran `ganda runfile cache --clear`, then `dotnet run tests/ci-tests/run-ci-tests.cs`: exit 0 with 0 failures (multi-mode plus standalone, generator-63 included). `./bin/dev build` passed. `ganda repo audit` passed with nothing to fix. The `samples/aspire-otel/nuru-client.cs` build passed.

### Review

- Round 1, effort 2, roster: general. Counts: bug 0/0/0, suggestion 0/0/0, nit 0/0/0 (open/fixed/wontfix).
- Disposition: clean. No wontfix and no escalation.
- Paths: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`.

### How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/generator/generator-63-telemetry-without-logging-using.cs
dotnet run tests/timewarp-nuru-tests/generator/generator-43-telemetry-options.cs
```

Expect: generator-63 reports `Passed: 2`, and generator-43 reports `Passed: 2`. If you revert `telemetry-emitter.cs` to master, generator-63 fails with CS1061 for `SetMinimumLevel` and `AddOpenTelemetry` in `NuruGenerated.g.cs`.

## Session

- Created: 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-10-22), from 517 follow-up (c); Steven asked to file it.
- 2026-10-23 implementer (claude, ganda task work): reproduced CS1061 in a Roslyn-hosted test; static-form emission; SetMinimumLevel replaced by Configure<LoggerFilterOptions> (CS0433); dropped the OpenTelemetry usings; CI, build and audit green.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-22T17:04:49Z
- Review oracle: reviewed by general (cursor, model grok-4.7), session not reported, max-turns uncapped (review budget 120), clean — 2026-10-23
- Review oracle: review by reviewer-cursor (cursor, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 120) — 2026-10-22T17:16:26Z
