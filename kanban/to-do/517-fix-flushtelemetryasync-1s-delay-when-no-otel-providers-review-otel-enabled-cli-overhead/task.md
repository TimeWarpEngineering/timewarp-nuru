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

## Goal (implementer restatement)

An app that calls `UseTelemetry()` but has no OTLP endpoint ends up with null tracer, meter, and logger providers. It still waits one second on every run because `NuruApp.FlushTelemetryAsync` always runs `Task.Delay(1000)`. The fix is to return early from `FlushTelemetryAsync` when all three providers are null. The emitters stay as they are, and the 1000 ms grace delay stays for runs that do have an endpoint.

Acceptance criteria:
- `FlushTelemetryAsync` returns at once when `TracerProvider`, `MeterProvider`, and `LoggerFactory` are all null.
- When any provider is set, the method keeps its old behavior: ForceFlush, then the grace delay, then dispose and null out.
- A test shows that `UseTelemetry()` with `OTEL_EXPORTER_OTLP_ENDPOINT` unset finishes a command well under one second.
- A regression test shows that with a provider set, flush still exports buffered spans, keeps the grace delay, and disposes the providers.
- A perf review with OTEL on, run against a live OTLP receiver, covers provider build time, ForceFlush time, and end-to-end per-command time. Findings go in the task notes. Any larger follow-up is only noted here, not done.

## Checklist

- [x] Add no-provider early return to `FlushTelemetryAsync`
- [x] Test: `UseTelemetry()` + unset endpoint runs without the ~1s delay
- [x] Test: `UseTelemetry()` + set endpoint still flushes and exports (regression guard)
- [x] Perf review with OTEL enabled (see Notes): measure startup provider build, ForceFlush latency, full per-command overhead against a live OTLP receiver; record findings in task notes

## Session

- Created: ses_e9c4b73edffeTIp9QOJ2z6Av4Q (2026-10-22)
- Implement (implementer-claude, headless ganda task work, 2026-10-22): early return, telemetry-03 tests, perf review against a live otel/opentelemetry-collector 0.163.2 container.

## Notes

Analysis (2026-10-22, ses_e9c4b73edffeTIp9QOJ2z6Av4Q): read from nuru master sources in nuru-app.cs and the emitters; consumer-side note in timewarp-ganda `source/timewarp-ganda/services/command-usage-log.cs` documents that ganda deliberately does NOT call `UseTelemetry()` because of this flush cost — surfaces as `Nuru's FlushTelemetryAsync waits about a second even when OTEL_EXPORTER_OTLP_ENDPOINT is unset`.

Perf review with OTEL enabled — dimensions to cover:

1. Startup: `Sdk.CreateTracerProviderBuilder()` / `CreateMeterProviderBuilder()` build + OTLP exporter setup, per command invocation (providers are built and disposed per run by generated `RunAsync_Intercepted`).
2. Steady-state exit: `TracerProvider.ForceFlush()` block time on the batch export processor when a real collector is reachable.
3. End-to-end: wall time of a trivial command with endpoint set + receiver running, vs endpoint set without the early-return fix, vs endpoint unset after this fix.
4. Judge whether per-run provider construction/dispose itself is the larger cost (a CLI invoking the binary dozens of times per hour) — if so, note a follow-up (shared/session provider or daemon-hosted flush) as a separate decision, do not bundle it here.

### Perf review findings (2026-10-22, implementer)

Setup: Release build of a minimal `UseTelemetry()` + one `noop` query route, run as `dotnet app.dll noop` (7 runs each). The collector was `otel/opentelemetry-collector` 0.163.2 in docker on 127.0.0.1:14317 (gRPC) and :14318 (HTTP). It confirmed receiving the spans. Baseline `dotnet --version` was about 118 ms. Machine: WSL2.

| Scenario | Wall time per run |
|---|---|
| Endpoint unset, after this fix | **34–36 ms** |
| Endpoint unset, before this fix | ~1035 ms (same path plus the fixed 1000 ms `Task.Delay`; the telemetry-03 tests time out at 2.03 s for 2 runs with the early return disabled) |
| Endpoint set, collector reachable (gRPC) | 1187–1208 ms |
| Endpoint set, collector reachable (http/protobuf) | 1172–1204 ms |
| Endpoint set, nothing listening (port 9) | 1182–1208 ms |

The fix does not touch the endpoint-set path, so "endpoint set without the fix" is the same as the endpoint-set rows.

Provider microbenchmark (tracer + meter + OTLP logger factory, in-process, 20 iterations, collector reachable):

- Build providers and exporters: **~117–122 ms on the first build in a process** (type loading and JIT), ~1 ms on later builds.
- `ForceFlush()` on tracer + meter with one span: **~35–45 ms the first time**, ~1–2 ms after.
- Dispose of all three: ~6–7 ms the first time, under 1 ms after.

Conclusions:

1. Once a collector is configured, the fixed 1000 ms grace delay is about 85% of the per-command cost. In a fresh process the cost is roughly 120 ms build + 40 ms flush + 1000 ms delay + dispose. `ForceFlush()` is synchronous and already waits for the batch export to finish, so a successful flush makes the delay redundant. That holds even with a dead endpoint, where the flush fails fast and the delay still runs.
2. Building providers per run costs about 120 ms, but nearly all of it is the first-in-process cost. A shared or session provider would not remove it for a CLI, because each invocation is a new process. Only a daemon-hosted provider would remove it.
3. Follow-ups, not bundled here per the requirements:
   - (a) Skip or shorten the grace delay when `ForceFlush()` returns true, or replace it with `ForceFlush(timeoutMs)` / `Shutdown(timeoutMs)`. This needs a decision on whether any exporter needs time after ForceFlush.
   - (b) Daemon-hosted telemetry providers for apps that run the binary many times per hour.
   - (c) Separate defect found while building the harness: the code generated for `UseTelemetry()` calls `ILoggingBuilder.SetMinimumLevel` / `AddOpenTelemetry` without emitting `using Microsoft.Extensions.Logging;`. A consumer that does not have that namespace as a global using gets CS1061 in `NuruGenerated.g.cs`. The test projects hide this because `tests/Directory.Build.props` adds `<Using Include="Microsoft.Extensions.Logging" />`. This should be its own task.

## Results

- `NuruApp.FlushTelemetryAsync` (`source/timewarp-nuru/nuru-app.cs`) now returns immediately when `TracerProvider`, `MeterProvider`, and `LoggerFactory` are all null. When any of them is set, the method behaves as before: ForceFlush, the 1000 ms default grace delay, then dispose and null out. Emitters are unchanged.
- New test `tests/timewarp-nuru-tests/telemetry/telemetry-03-flush-without-providers.cs` (picked up by the CI glob):
  - `Should_run_command_without_grace_delay_when_endpoint_unset`: `UseTelemetry()` with the env var cleared. The command returns 0, every provider is null, and the timed second run takes under 500 ms.
  - `Should_return_immediately_when_no_provider_exists`: `FlushTelemetryAsync(delayMs: 5000)` with no providers returns in under 1 s.
  - `Should_flush_delay_and_dispose_when_tracer_provider_exists`: regression guard. A tracer provider with a batch processor (60 s scheduled delay) and a counting exporter: flush exports the buffered span, honors the grace delay, shuts the exporter down, and nulls `TracerProvider`.
  - With the early return disabled, the first two tests fail (2.03 s and 5.00 s) and the third still passes.
- `./bin/dev build` and `./bin/dev test` (multi-mode + standalone phases) are green, and FlushWithoutProviders is 3/3 in multi-mode. `ganda repo audit` passes. The pre-push audit flagged `runfile-executable` on the new test file, so it is now committed as mode 100755.
- Verified against the current master (`d39e6c0e`, version 4.0.0-beta.5) at implementation time.

### How to validate

Smoke:

```bash
dotnet run tests/timewarp-nuru-tests/telemetry/telemetry-03-flush-without-providers.cs
```

Expect: 3 tests passed. The timed unset-endpoint run takes well under 500 ms, compared with about 1 s per run before. The tracer-provider test takes about 0.3 s (its grace delay).

Optional end-to-end: in a Release build of a `UseTelemetry()` CLI (the consumer needs `global using Microsoft.Extensions.Logging;`, see follow-up (c)), `env -u OTEL_EXPORTER_OTLP_ENDPOINT dotnet app.dll <cmd>` should take tens of ms. With `OTEL_EXPORTER_OTLP_ENDPOINT` set it should still take about 1.2 s, and spans should arrive at the collector.
