# Round 1 — general
**Date:** 2026-10-23
**Scope reviewed:** branch `task/521-generated-usetelemetry-code-fails-with-cs1061-with` vs `master` (`d896df9f`)

## Summary

`UseTelemetry()` setup no longer calls `ILoggingBuilder` or OpenTelemetry extensions by instance syntax. Resource, tracing, metrics, and exporter calls are static and spelled with `global::` declaring types that match OpenTelemetry 1.19.1, OpenTelemetry.Exporter.OpenTelemetryProtocol 1.19.1, and Microsoft.Extensions.Logging 10.0.12. The six generated `using global::OpenTelemetry.*` directives are gone, and nothing else in the emitters relied on them.

`SetMinimumLevel` is not emitted as `LoggingBuilderExtensions.SetMinimumLevel`. That type is declared in both Microsoft.Extensions.Logging and Microsoft.Extensions.Logging.Configuration (OpenTelemetry depends on the configuration package), so the static call is CS0433. The emitter instead calls `OptionsServiceCollectionExtensions.Configure<LoggerFilterOptions>`, which sets `MinLevel` the same way `SetMinimumLevel` does: `SetMinimumLevel` registers `DefaultLoggerLevelConfigureOptions`, whose action assigns `options.MinLevel`. `Configure` registers a `ConfigureNamedOptions` for `Options.DefaultName` that assigns `MinLevel`. `LoggerFactory.Create` already targets that default options instance. Both `UseTelemetry()` shapes were compiled here against `TimeWarp.Nuru`'s dependency closure with only `using TimeWarp.Nuru;` in the app. Each compilation had zero errors, the generated logging section contained the static `AddOpenTelemetry` call, and it did not contain `builder.SetMinimumLevel`, `builder.AddOpenTelemetry`, or an OpenTelemetry using.

`AddSource`, `AddMeter`, and `OpenTelemetryLoggerOptions.SetResourceBuilder` are instance methods on the types the generated variables already have (`TracerProviderBuilder` and `MeterProviderBuilder` in OpenTelemetry.Api 1.19.1; `OpenTelemetryLoggerOptions` in OpenTelemetry 1.19.1). The goal restatement matches the task. The `SetMinimumLevel` substitution is the compilable form of that requirement, not a misread.

generator-63 compiles the app in memory, so `tests/Directory.Build.props` cannot inject `using Microsoft.Extensions.Logging`. It is excluded from the multi-mode assembly and listed in the CI standalone phase, same as the other Roslyn-hosted generator tests.

## Issues
