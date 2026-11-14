# Telemetry flush: drop the 1 s grace delay after a successful ForceFlush and bound flush time for a blackholed endpoint

## Description

Task 520's AOT benchmark (`benchmarks/telemetry-overhead/`, results `results/2026-11-15-telemetry-overhead.md`) shows two costs in `NuruApp.FlushTelemetryAsync` (`source/timewarp-nuru/nuru-app.cs`) for a `UseTelemetry()` CLI with `OTEL_EXPORTER_OTLP_ENDPOINT` set:

1. **Grace delay.** With a reachable local receiver, an AOT run takes about 1016 ms against 4.1 ms with telemetry off. Provider setup is about 5 ms and ForceFlush plus dispose about 3 ms. The fixed `Task.Delay(1000)` is about 1000 ms, or 99% of the overhead. `ForceFlush()` is synchronous and has already finished the export when the delay starts (517 finding 1).
2. **Blackholed endpoint.** When packets to the endpoint are dropped (tested with `http://192.0.2.1:4317`, TEST-NET-1), each run takes about **17 s** with the default exporter timeout. With `OTEL_EXPORTER_OTLP_TIMEOUT=1000` it takes 4.0 s, and with 2000 it takes 6.0 s. `ForceFlush()` and dispose each wait out the export timeout. A closed local port (connection refused) is cheap: about 1015 ms, the same as a reachable one.

## Requirements

- Skip the grace delay when `ForceFlush()` reports success, or replace the delay with `ForceFlush(timeoutMs)` / `Shutdown(timeoutMs)`. Decide whether any exporter needs time after a successful ForceFlush (517 follow-up a).
- Bound the total flush time of a CLI run with a short total budget, so a blackholed or slow endpoint costs a CLI run at most that budget (for example a couple of seconds), not several export timeouts. A user-set `OTEL_EXPORTER_OTLP_TIMEOUT` should still be respected.
- Re-run `benchmarks/telemetry-overhead/run-benchmark.sh` and add a dated results file showing case 3 near the case 2 cost.

## Checklist

- [ ] Grace delay skipped or replaced after a successful flush
- [ ] Flush time bounded for a blackholed endpoint
- [ ] Tests for both paths
- [ ] Benchmark re-run with results

## Notes

- Found by 520 (telemetry overhead benchmark). Related: 517 (no-provider early return, PR #347), 501 (startup performance).
- The blackhole timing was a one-off command, not part of `run-benchmark.sh`:
  `env OTEL_EXPORTER_OTLP_ENDPOINT=http://192.0.2.1:4317 publish/aot/bench-telemetry-on/bench-telemetry-on greet --name x`

## Session

- Created: 1493508 (2026-11-15), filed by the 520 implementer from benchmark results.
