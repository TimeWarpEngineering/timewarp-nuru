# Round 1 — core-runtime
**Date:** 2026-09-04
**Scope reviewed:** `source/timewarp-nuru/` excluding `repl/` and `completion/` — especially `nuru-app.cs`, `nuru-app-builder.cs`, `builders/` (including `nuru-app-builder/`), `attributes/`, `type-conversion/`, `capabilities/`, `telemetry/`, `configuration/`, `endpoints/`, `abstractions/`, `services/`, `serialization/`, `help/`, `io/`, `extensions/`, `logging/`, `check-updates/`, `options/repl-options.cs` (DI/runtime surface only); cross-checked binding/telemetry/capabilities against `source/timewarp-nuru-analyzers/generators/emitters/{type-conversion-map,route-matcher-emitter,capabilities-emitter,telemetry-emitter,help-emitter}.cs` and related IR/DSL hooks

## Summary

Core runtime is largely a compile-time DSL surface: builders, attributes, and options types are stubs whose real behavior is emitted by the source generator. Type conversion (InvariantCulture, `BooleanConverter`, enum `IsDefined` / Flags named-parts) and capabilities DTOs look solid and match the post-454 remediation. Dominant theme for new defects: several public configure APIs (`UseTelemetry(Action<>)`, `ConfigureHelp` / `HelpOptions`, `Services`) document runtime behavior that is never applied because options are discarded or never read. Three bugs and a few nits; 454-008 / 454-009 / 454-027 cited items are not still present in the core files they named.

## 454 regression check

| 454 ID | Still present? | Evidence |
|--------|----------------|----------|
| 454-008 | no | `source/timewarp-nuru/type-conversion/converters/enum-type-converter.cs:56-60` — non-Flags gated with `Enum.IsDefined`; Flags path requires named parts (`:48-54`, `:77-90`). Mirrored in `default-type-converters.cs:205-231`. Covered by `tests/.../routing/routing-29-enum-undefined-values.cs` and `type-conversion-02-runtime-parity.cs`. |
| 454-009 | no | `default-type-converters.cs` uses `CultureInfo.InvariantCulture` on numeric/date parses (e.g. `:21`, `:77`, `:109`, `:133`); bool via `BooleanConverter.TryParse` (`:91-97`). Public dead converter classes removed; only `EnumTypeConverter<T>` remains. Generator map (`type-conversion-map.cs:30-49`) matches. |
| 454-027 | no | Dead `TypeConverterRegistry` field gone from builder; `AddTypeConverter` documented compile-time no-op (`nuru-app-builder.routes.cs:128-141`). ToCamelCase remark corrected to `"dryrun"` and divergence documented (`compiled-route-builder.cs:105-109`, `:238-247`). `AppNameDetector` skips `"dotnet"` host (`app-name-detector.cs:22-33`, `:51-52`). `TelemetryBehavior` records `Elapsed.TotalMilliseconds` (`telemetry-behavior.cs:49`, `:67`). (Note for analyzers reviewer: generated twin still uses `__sw.ElapsedMilliseconds` in `telemetry-emitter.cs:105,124` — outside this area’s 454-027 citations.) |

## Issues

### Issue 1 — Severity: bug
- File: `source/timewarp-nuru/builders/nuru-app-builder/nuru-app-builder.configuration.cs:179-185`
- Description: `UseTelemetry(Action<NuruTelemetryOptions> configure)` never invokes `configure` and returns immediately. `NuruTelemetryOptions` (`nuru-telemetry-options.cs:13-62`) documents `ServiceName`, `ServiceVersion`, `EnableTracing` / `EnableMetrics` / `EnableLogging`, and `OtlpEndpoint`, but nothing in core or the generated setup path reads those properties — emitted setup only consults `OTEL_*` environment variables (`telemetry-emitter.cs:42-48`). Callers who pass a configure lambda get a silent no-op relative to the documented API.
- Suggestion: Either invoke `configure` at runtime and thread the resulting options into generated setup (or extract the lambda at compile time into the IR), or remove/obsolete the `Action<>` overload and keep only parameterless `UseTelemetry()` plus env-var configuration until options are wired.
- Status: open

### Issue 2 — Severity: bug
- File: `source/timewarp-nuru/help/help-options.cs:9-37`
- Description: `HelpOptions` documents filtering of per-command help routes, REPL commands, completion routes, and `ExcludePatterns`. `ConfigureHelp` mutates the builder field (`nuru-app-builder.cs:90-95`), but no consumer in `source/timewarp-nuru/` or the help emitters reads `ShowPerCommandHelpRoutes`, `ShowReplCommandsInCli`, `ShowCompletionRoutes`, `ExcludePatterns`, `ReplCommandPatterns`, or `CompletionRoutePrefixes` (repo-wide search of those identifiers only hits `help-options.cs`). `HelpEmitter` lists all routes with no filter (`help-emitter.cs:111-116`). Documented help filtering does not take effect.
- Suggestion: Wire `HelpOptions` into help emission (extract via DSL / pass into `HelpModel`, or filter at PrintHelp generation time), or mark the options and `ConfigureHelp` as not-yet-implemented and stop advertising filtering defaults as live behavior.
- Status: open

### Issue 3 — Severity: bug
- File: `source/timewarp-nuru/builders/nuru-app-builder/nuru-app-builder.cs:26-37`
- Description: Public `Services` getter always throws `InvalidOperationException` telling the caller to `Call AddDependencyInjection() first`. There is no `AddDependencyInjection` method on the builder (opt-in is `UseMicrosoftDependencyInjection` at `nuru-app-builder.configuration.cs:226-231`). `ServiceCollection` is declared (`nuru-app-builder.cs:13`) but never assigned anywhere under `source/timewarp-nuru/` (`ServiceCollection =` has zero matches), so the getter cannot succeed even after `UseMicrosoftDependencyInjection()`.
- Suggestion: Either populate `ServiceCollection` when runtime DI is enabled and fix the exception text to name `UseMicrosoftDependencyInjection()`, or remove/obsolete the `Services` property and point callers at `ConfigureServices` (compile-time) only.
- Status: open

### Issue 4 — Severity: nit
- File: `source/timewarp-nuru/nuru-app.cs:135-147`
- Description: `CreateBuilder` XML docs cref `NuruAppBuilder.AddHelp(Action{HelpOptions})`, which does not exist (public API is `ConfigureHelp`). The example uses `.Map("greet {name}", handler)` — a two-argument `Map` overload that also does not exist (`nuru-app-builder.routes.cs:91`, `:120`). Related: `ConfigureServices` examples still show `.AddDependencyInjection()` (`nuru-app-builder.configuration.cs:51`, `:79`).
- Suggestion: Update crefs/examples to `ConfigureHelp`, `.Map(...).WithHandler(...).Done()`, and `UseMicrosoftDependencyInjection()` (or drop the obsolete method names).
- Status: open

### Issue 5 — Severity: nit
- File: `source/timewarp-nuru/services/empty-service-provider.cs:6-12`
- Description: `EmptyServiceProvider` is internal and unreferenced outside its own file (no other `.cs` hits). Same pattern for unused `NuruLoggingBuilder` / `NuruMetricsBuilder` wrappers (`nuru-logging-builder.cs`, `nuru-metrics-builder.cs`). Dead DI leftovers after the source-gen / `UseMicrosoftDependencyInjection` split.
- Suggestion: Delete the unused types, or wire them if a runtime path still needs an empty `IServiceProvider`.
- Status: open
