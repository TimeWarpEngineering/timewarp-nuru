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

## Checklist

- [ ] Reproduce CS1061 with an app that has no logging using
- [ ] Emit the logging and OpenTelemetry extension calls in static, fully qualified form
- [ ] Audit the other telemetry emission for unqualified extension calls
- [ ] Regression test that is not masked by tests/Directory.Build.props
- [ ] CI tests green; changelog entry

## Notes

- Related: 517 (FlushTelemetryAsync no-provider delay), 520 (telemetry overhead benchmark, which will hit this when its apps have no global usings).

## Session

- Created: 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-10-22), from 517 follow-up (c); Steven asked to file it.
