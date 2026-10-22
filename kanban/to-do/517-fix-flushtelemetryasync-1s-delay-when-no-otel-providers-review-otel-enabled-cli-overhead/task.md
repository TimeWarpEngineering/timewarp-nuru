# Fix FlushTelemetryAsync 1s delay when no OTEL providers; review OTEL-enabled CLI overhead

## Description

An app built with `UseTelemetry()` pays ~1 second per run even when `OTEL_EXPORTER_OTLP_ENDPOINT` is unset and no exporter/provider exists.

Root cause (verified against master and against `4.0.0-beta.5` as pinned by timewarp-ganda):

1. Generator setup_emission is gated correctly: `TelemetryEmitter.EmitTelemetrySetup` (`source/timewarp-nuru-analyzers/generators/emitters/telemetry-emitter.cs`) emits a runtime check of `OTEL_EXPORTER_OTLP_ENDPOINT`; with no endpoint, `app.TracerProvider` / `app.MeterProvider` / `app.LoggerFactory` stay null.
2. The flush is not gated: `InterceptorEmitter.EmitAppInterceptorMethod` (`source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs`) emits `try { ExecuteRouteAsync } finally { await app.FlushTelemetryAsync(); }` unconditionally whenever `UseTelemetry()` was called.
3. `NuruApp.FlushTelemetryAsync(int delayMs = 1000)` (`source/timewarp-nuru/nuru-app.cs`) null-conditional no-ops `ForceFlush()` / `Dispose()` on null providers, but `await Task.Delay(delayMs)` runs unconditionally — the fixed 1s cost.

The null-safe no-ops make the flush "safe," but the unconditional delay makes the no-op cost 1s per command. A CLI that exits immediately then waits a fixed second it does not need.

## Requirements

- Fix (Option 1, behavior-preserving for both cases): in `NuruApp.FlushTelemetryAsync`, return immediately when there is nothing to flush:

```csharp
public async Task FlushTelemetryAsync(int delayMs = 1000)
{
    if (TracerProvider is null && MeterProvider is null && LoggerFactory is null)
      return;

    TracerProvider?.ForceFlush();
    // ... rest unchanged
}
```

- Keep the 1000 ms grace delay unchanged for the has-endpoint case (ForceFlush runs before it; the delay remains the pre-dispose grace period for batched exporters).
- Do NOT change emitters and do NOT switch to `FlushTelemetryAsync(delayMs: 0)` — that would remove the grace period when an endpoint IS set.
- Verify the fix against whichever version is current at implementation time; there is recent telemetry churn (e.g. error.tag-parity fix landed after this analysis).
- Add/adjust a test covering: telemetry enabled, no `OTEL_EXPORTER_OTLP_ENDPOINT`, command completes and returns fast (no ~1s wall time).

## Checklist

- [ ] Add no-provider early return to `FlushTelemetryAsync`
- [ ] Test: `UseTelemetry()` + unset endpoint runs without the ~1s delay
- [ ] Test: `UseTelemetry()` + set endpoint still flushes and exports (regression guard)
- [ ] Perf review with OTEL enabled (see Notes): measure startup provider build, ForceFlush latency, full per-command overhead against a live OTLP receiver; record findings in task notes

## Session

- Created: ses_e9c4b73edffeTIp9QOJ2z6Av4Q (2026-10-22)

## Notes

Analysis (2026-10-22, ses_e9c4b73edffeTIp9QOJ2z6Av4Q): read from nuru master sources in nuru-app.cs and the emitters; consumer-side note in timewarp-ganda `source/timewarp-ganda/services/command-usage-log.cs` documents that ganda deliberately does NOT call `UseTelemetry()` because of this flush cost — surfaces as `Nuru's FlushTelemetryAsync waits about a second even when OTEL_EXPORTER_OTLP_ENDPOINT is unset`.

Perf review with OTEL enabled — dimensions to cover:

1. Startup: `Sdk.CreateTracerProviderBuilder()` / `CreateMeterProviderBuilder()` build + OTLP exporter setup, per command invocation (providers are built and disposed per run by generated `RunAsync_Intercepted`).
2. Steady-state exit: `TracerProvider.ForceFlush()` block time on the batch export processor when a real collector is reachable.
3. End-to-end: wall time of a trivial command with endpoint set + receiver running, vs endpoint set without the early-return fix, vs endpoint unset after this fix.
4. Judge whether per-run provider construction/dispose itself is the larger cost (a CLI invoking the binary dozens of times per hour) — if so, note a follow-up (shared/session provider or daemon-hosted flush) as a separate decision, do not bundle it here.
