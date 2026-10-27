# TelemetryModel: configurable extra ActivitySources for downstream tools (ganda task spans)

## Description

Nuru's generated OTel provider registers exactly two traced sources when `UseTelemetry()` is called (verified against master, `TelemetryEmitter.EmitTelemetrySetup` in `source/timewarp-nuru-analyzers/generators/emitters/telemetry-emitter.cs`):

```csharp
.AddSource(__activitySource.Name)                       // "TimeWarp.Nuru"
.AddSource("TimeWarp.Nuru.Behavior")                    // TelemetryBehavior
```

Any downstream tool that starts its own `ActivitySource` under the same provider (same process, single OTLP endpoint) is silently dropped: spans are created but never exported, with no error. Timewarp ganda hit this designing its `ganda.task.transition` / `ganda.task.walk` lifecycle spans (ganda task 490): task spans are created with an `ActivitySource("TimeWarp.Ganda")` that the provider doesn't listen to.

Filing on the nuru side because source registration belongs where the provider is built — ganda cannot reach the `AddSource` chain through `UseTelemetry()` alone.

## Requirements

- Allow extra sources via builder configuration, surviving source generation: on `NuruAppBuilder.UseTelemetry(...)`, an optional configure callback (or parameter) capturing source names into `TelemetryModel`, which `EmitTelemetrySetup` emits as extra `.AddSource("...")` literals.
- Symbolic default worth considering (to keep cross-tool wiring painless): also honor `OTEL_ADDITIONAL_SOURCES` (comma-separated) at runtime in the generated provider construction, so tools can register sources without any app-side change. Two mechanisms, either acceptable; option A (builder-captured) is primary, B documented as an env-only convenience. Do not invent a new env var name if a standard one exists — search OTel .NET conventions first and note the finding.
- Keep AOT cleanliness (only string literals into `AddSource`).
- Generator/model tests: `UseTelemetry` with no extra sources → emitted code unchanged (regression); with extra sources → `.AddSource` lines emitted and sources present on the built provider (spans from the extra source appear at the collector).

## Checklist

- [ ] `TelemetryModel.ExtraSources` (builder-side capture) implemented
- [ ] `EmitTelemetrySetup` emits the extra `.AddSource(...)` calls
- [ ] Decision recorded re: `OTEL_ADDITIONAL_SOURCES` or standard equivalent (or deliberately none)
- [ ] Generator tests: no-change regression + extra-source emission
- [ ] End-to-end test: span from a custom source name reaches the test collector
- [ ] README/XML docs for `UseTelemetry` updated

## Depends on

(none — small, self-contained; ganda task 490 becomes able to depend on this one explicitly once filed there.)

## Session

- Created: ses_e9c4b73edffeTIp9QOJ2z6Av4Q (2026-10-27)

## Notes

Filing background: ganda task 490 (task lifecycle spans for DevTools UX / AI verification) designed attributes and emitters; hit this constraint as the "known constraint to resolve first" — 490 currently records this as "file the nuru task during pickup". Filed directly instead so the dependency chain is explicit and the nuru-side work can start in parallel with ganda 488 (the `UseTelemetry` wiring, which needs none of this).