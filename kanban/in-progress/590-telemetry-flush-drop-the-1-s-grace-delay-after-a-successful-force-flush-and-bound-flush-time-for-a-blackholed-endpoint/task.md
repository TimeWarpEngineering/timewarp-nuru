# Telemetry flush: drop the 1 s grace delay after a successful ForceFlush and bound flush time for a blackholed endpoint

## Description

Task 520's AOT benchmark (`benchmarks/telemetry-overhead/`, results `results/2026-11-15-telemetry-overhead.md`) shows two costs in `NuruApp.FlushTelemetryAsync` (`source/timewarp-nuru/nuru-app.cs`) for a `UseTelemetry()` CLI with `OTEL_EXPORTER_OTLP_ENDPOINT` set:

1. **Grace delay.** With a reachable local receiver, an AOT run takes about 1016 ms against 4.1 ms with telemetry off. Provider setup is about 5 ms and ForceFlush plus dispose about 3 ms. The fixed `Task.Delay(1000)` is about 1000 ms, or 99% of the overhead. `ForceFlush()` is synchronous and has already finished the export when the delay starts (517 finding 1).
2. **Blackholed endpoint.** When packets to the endpoint are dropped (tested with `http://192.0.2.1:4317`, TEST-NET-1), each run takes about **17 s** with the default exporter timeout. With `OTEL_EXPORTER_OTLP_TIMEOUT=1000` it takes 4.0 s, and with 2000 it takes 6.0 s. `ForceFlush()` and dispose each wait out the export timeout. A closed local port (connection refused) is cheap: about 1015 ms, the same as a reachable one.

## Requirements

- Skip the grace delay when `ForceFlush()` reports success, or replace the delay with `ForceFlush(timeoutMs)` / `Shutdown(timeoutMs)`. Decide whether any exporter needs time after a successful ForceFlush (517 follow-up a).
- Bound the total flush time of a CLI run with a short total budget, so a blackholed or slow endpoint costs a CLI run at most that budget (for example a couple of seconds), not several export timeouts. A user-set `OTEL_EXPORTER_OTLP_TIMEOUT` should still be respected.
- Re-run `benchmarks/telemetry-overhead/run-benchmark.sh` and add a dated results file showing case 3 near the case 2 cost.

## Goal (implementer restatement)

`NuruApp.FlushTelemetryAsync` should stop costing a CLI run a fixed second, and a dead endpoint
should not cost it many seconds.

- No grace delay. The flush finishes as soon as the export has finished.
- One short total time budget (default 2 s) for flushing and disposing all providers. A
  blackholed endpoint costs at most that budget, not several exporter timeouts.
- A set `OTEL_EXPORTER_OTLP_TIMEOUT` is the budget instead of the default.

Acceptance:

- A flush with a working exporter exports the buffered span, shuts the exporter down, clears the
  providers, and returns without a 1 s wait.
- A flush whose exporter hangs returns after about the budget (an explicit `timeoutMs`, or
  `OTEL_EXPORTER_OTLP_TIMEOUT`), not after the hang.
- Tests for both paths.
- `run-benchmark.sh` re-run, with a dated results file where case 3 is close to case 2, plus a
  blackhole timing.

## Checklist

- [x] Grace delay skipped or replaced after a successful flush
- [x] Flush time bounded for a blackholed endpoint
- [x] Tests for both paths
- [x] Benchmark re-run with results

## Review

- Round 1: Done

## Notes

- Found by 520 (telemetry overhead benchmark). Related: 517 (no-provider early return, PR #347), 501 (startup performance).
- The blackhole timing was a one-off command, not part of `run-benchmark.sh`:
  `env OTEL_EXPORTER_OTLP_ENDPOINT=http://192.0.2.1:4317 publish/aot/bench-telemetry-on/bench-telemetry-on greet --name x`

## Session

- Created: 1493508 (2026-11-15), filed by the 520 implementer from benchmark results.
- 2026-11-19 implementer (Claude Opus 5.5, `ganda task work`): replaced the grace delay with a
  bounded parallel shutdown, added telemetry-07, re-ran the benchmark with a new blackhole case 5.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 4ed09d35-117b-46d2-a11b-3ab17572538d, max-turns 500, Done — 2026-11-18T17:13:36Z
- Review: Grok session 01a1efa6-70e4-7740-ac80-1004c8ea1c50 (2026-11-19), effort 3, roster general, round 1.
- Review oracle: review by reviewer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1efa6-70e4-7740-ac80-1004c8ea1c50, max-turns 200, artifacts general.md, merged.md, disposition.md — 2026-11-18T17:23:23Z

## Results

### Decision

- **Grace delay removed (517 follow-up a).** No exporter needs time after the export returns. In
  OpenTelemetry 1.21 (checked by decompiling), `BatchExportProcessor` and
  `PeriodicExportingMetricReader` export on their worker thread and `Shutdown(timeout)` joins
  that thread, so a successful return means the export has finished. `FlushTelemetryAsync` now
  calls `Shutdown(budget)` on the tracer and meter providers and disposes them, and disposes the
  logger factory. The three run in parallel on the thread pool. The call waits at most the budget
  (`Task.WhenAll(...).WaitAsync(budget)`).
- **Budget.** The `timeoutMs` argument, else a positive `OTEL_EXPORTER_OTLP_TIMEOUT`, else
  `NuruApp.DefaultTelemetryFlushTimeoutMs` (2000). `Timeout.Infinite` (-1) waits for the export,
  and a value below -1 throws.
- **Why a blackholed run stops at the budget.** `Shutdown(timeout)` bounds the tracer and meter
  providers. The OTLP exporter's shutdown also cancels pending HTTP requests. `ILoggerFactory` has
  no timed shutdown, so only the outer wait bounds it. SDK worker threads are background threads,
  and they catch `ObjectDisposedException`. A late export thread neither keeps the process alive
  nor crashes it.
- **API change (beta package, 4.0.0-beta.16).** `FlushTelemetryAsync(int delayMs = 1000)` is now
  `FlushTelemetryAsync(int? timeoutMs = null)`. Generated code calls it with no argument, so
  generated apps are unaffected. A caller that passed `delayMs:` gets a compile error. The
  changelog lists the change under Changed.
- Each run now sends 1 metric export instead of 2, because there is no `ForceFlush` before
  dispose.

### Files

- `source/timewarp-nuru/nuru-app.cs`: new `FlushTelemetryAsync`, `DefaultTelemetryFlushTimeoutMs`, and `ResolveTelemetryFlushTimeoutMs`.
- `tests/timewarp-nuru-tests/telemetry/telemetry-07-flush-budget.cs` (new): has no grace delay after a successful export, returns after an explicit timeout when the export hangs, honours `OTEL_EXPORTER_OTLP_TIMEOUT`, bounds a hanging logger factory dispose, and rejects a timeout below -1.
- `tests/timewarp-nuru-tests/telemetry/telemetry-03-flush-without-providers.cs`: uses the new parameter, and the delay assertion is gone. The local const was renamed to `SourceName` (IDE1006).
- `benchmarks/telemetry-overhead/run-benchmark.sh` and `readme.md`: new case 5 for a blackholed endpoint (`BLACKHOLE_ENDPOINT`, `BLACKHOLE_RUNS`), and the grace-delay wording is gone.
- `benchmarks/telemetry-overhead/results/2026-11-19-telemetry-overhead.md` and `results/raw/2026-11-19/`: the new run, with the hand-written analysis.
- `documentation/user/features/telemetry.md`: `OTEL_EXPORTER_OTLP_TIMEOUT` row and a "Flush at exit" section.
- `changelog.md`: entry under Changed.

### Benchmark (AOT, `greet --name x`)

| Case | 2026-11-15 | 2026-11-19 |
|:---|---:|---:|
| 2 on, no endpoint | 5.4 ms | 5.8 ms |
| 3 on, reachable, gRPC | 1016 ms | 13.1 ms |
| 3 on, reachable, http/protobuf | 1014 ms | 12.7 ms |
| 4 on, closed port | 1015 ms | 12.3 ms |
| 5 on, blackholed (default) | about 17 s | 2.01 s |
| 5 on, blackholed, `OTEL_EXPORTER_OTLP_TIMEOUT=500` | — | 0.51 s |

### Gates

- `./bin/dev build`: pass. The first run failed on IDE0028 (`new(3)` list). It was fixed to `[]` and the rerun passed.
- `dotnet run --file tests/timewarp-nuru-tests/telemetry/telemetry-07-flush-budget.cs`: pass, 5/5.
- `dotnet run --file tests/timewarp-nuru-tests/telemetry/telemetry-03-flush-without-providers.cs`: pass, 3/3. The first run failed on IDE1006 (local const `sourceName`). It was renamed and the rerun passed.
- `dotnet run --file tests/timewarp-nuru-tests/telemetry/telemetry-04-otlp-endpoint-paths.cs`: pass, 9/9 (end-to-end OTLP export through the new flush).
- `dotnet run --file tests/timewarp-nuru-tests/telemetry/telemetry-05-use-telemetry-command-spans.cs`: pass, 7/7.
- `dotnet run --file tests/timewarp-nuru-tests/telemetry/telemetry-06-extra-activity-sources.cs`: pass, 2/2.
- `benchmarks/telemetry-overhead/run-benchmark.sh`: exit 0, wrote `results/2026-11-19-telemetry-overhead.md`.
- `ganda repo audit`: pass (exit 0), with one advisory warning (`vscode-window-icon`, git-ignored `.vscode/settings.json`).
- Commit attestation then failed on `runfile-executable` (telemetry-07 had no executable bit) and `package-pin-currency` (TimeWarp.Jaribu 1.0.0-beta.17 was behind beta.18; the TimeWarp.Amuru probe also hit a transient nuget.org connection reset). Fix: `chmod +x` on telemetry-07, then `ganda repo audit --fix`. That bumped TimeWarp.Jaribu to 1.0.0-beta.18 in `Directory.Packages.props` and set the ignored VS Code window title. Result: pass, 45 passed, 0 failed.
- After the Jaribu bump: `./bin/dev build` passed, telemetry-07 passed 5/5, and telemetry-03 passed 3/3.

### How to validate

Smoke:

```bash
dotnet run --file tests/timewarp-nuru-tests/telemetry/telemetry-07-flush-budget.cs
dotnet run --file tests/timewarp-nuru-tests/telemetry/telemetry-03-flush-without-providers.cs
# optional, a few minutes; needs hyperfine:
./benchmarks/telemetry-overhead/run-benchmark.sh
```

Expect:

- telemetry-07 passes 5/5. The successful flush finishes in under 500 ms, and the hanging flushes
  finish in about 300 ms or 400 ms (the env var), not after the hang.
- telemetry-03 passes 3/3.
- In the benchmark results, case 3 is within about 10 ms of case 2, and case 5 is about 2.0 s
  (about 0.5 s with `OTEL_EXPORTER_OTLP_TIMEOUT=500`).

### Review

- Round 1, effort 3, roster: general (Grok session 01a1efa6-70e4-7740-ac80-1004c8ea1c50, 2026-11-19).
- Counts: bug 0 open / 0 fixed / 0 wontfix, suggestion 0, nit 0. Final open count 0.
- Disposition: clean. The goal restatement matches the original task. No wontfix and no escalation.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.
