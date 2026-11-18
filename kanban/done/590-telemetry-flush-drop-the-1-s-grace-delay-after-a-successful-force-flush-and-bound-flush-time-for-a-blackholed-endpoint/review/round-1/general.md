# Round 1 — general
**Date:** 2026-11-19
**Scope reviewed:** branch `task/590-telemetry-flush-drop-the-1-s-grace-delay-after-a` vs merge-base `0e1bd801`. Product surface: `source/timewarp-nuru/nuru-app.cs` (`FlushTelemetryAsync`, `DefaultTelemetryFlushTimeoutMs`, `ResolveTelemetryFlushTimeoutMs`), `tests/timewarp-nuru-tests/telemetry/telemetry-03-flush-without-providers.cs`, `tests/timewarp-nuru-tests/telemetry/telemetry-07-flush-budget.cs`, `benchmarks/telemetry-overhead/run-benchmark.sh` and `readme.md`, `benchmarks/telemetry-overhead/results/2026-11-19-telemetry-overhead.md` plus `results/raw/2026-11-19/`, `documentation/user/features/telemetry.md`, `changelog.md`, `Directory.Packages.props` (Jaribu 1.0.0-beta.18). Call site checked: `EmitTelemetryFlush` still calls `FlushTelemetryAsync()` with no argument.

## Summary

`FlushTelemetryAsync` no longer waits a fixed second after export. It clears the three provider properties, then on the thread pool calls `Shutdown(budget)` for the tracer and meter providers and disposes the logger factory. The await is `Task.WhenAll(...).WaitAsync` for the same budget: an explicit `timeoutMs`, else a positive `OTEL_EXPORTER_OTLP_TIMEOUT`, else `DefaultTelemetryFlushTimeoutMs` (2000). `Timeout.Infinite` (-1) waits for the export. A value below -1 throws before any shutdown. A timeout leaves the shutdowns on background threads and returns, which is what a CLI exit needs.

The goal restatement matches the original task. The task allows replacing the grace delay with `Shutdown(timeoutMilliseconds)`, asks for one short total budget so a blackholed endpoint does not pay several exporter timeouts, and says a set `OTEL_EXPORTER_OTLP_TIMEOUT` is still respected. Using that variable as the single budget, instead of the 2 s default, is that reading. The recorded benchmark shows case 3 within about 7 ms of case 2, and case 5 at 2.01 s (0.51 s when the variable is 500).

`## Results` and `### How to validate` cover the change: `./bin/dev build`, telemetry-03 and telemetry-07 (and telemetry-04, -05, -06), `run-benchmark.sh`, and `ganda repo audit` after the executable bit and the Jaribu pin. This pass did not re-run those gates. A read of `results/raw/2026-11-19/{fast,slow,blackhole}.json` matches the table in `2026-11-19-telemetry-overhead.md` (means, run counts, exit codes all 0). Receiver stats are one trace export and one metric export per run (gRPC 66 = 33 AOT + 33 JIT, HTTP 33). telemetry-03's removed lower bound on the old grace delay is the contract change this task asked for; telemetry-07 asserts the successful flush finishes in under 500 ms and that a hang returns on the budget.

## Issues

<!-- None. -->
