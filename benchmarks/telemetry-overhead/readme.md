# Telemetry overhead benchmark

Measures how much `UseTelemetry()` adds to one run of a Native AOT Nuru CLI (task 520).
Every case runs the same command, `greet --name x`, which writes one line. Only the telemetry
setup changes between cases.

| Case | App | Environment |
|:---|:---|:---|
| 1 off (baseline) | `bench-telemetry-off` | `OTEL_EXPORTER_OTLP_ENDPOINT` unset |
| 2 on, no endpoint | `bench-telemetry-on` | `OTEL_EXPORTER_OTLP_ENDPOINT` unset |
| 3 on, reachable | `bench-telemetry-on` | endpoint = local OTLP receiver, gRPC and http/protobuf |
| 4 on, unreachable | `bench-telemetry-on` | endpoint = a closed local port (connection refused) |

Cases 1 and 2, and case 3 over gRPC, also run as JIT builds (`dotnet <app>.dll`), so the AOT
and JIT gap sits next to task 517's JIT numbers.

## Layout

- `shared/program.cs`: the program both apps compile. `bench-telemetry-on` defines
  `BENCH_TELEMETRY`, which adds `.UseTelemetry()`. Nothing else differs.
- `shared/bench-timeline.cs`: with `NURU_BENCH_TIMELINE=1`, the app writes
  `timeline handler=<ms> end=<ms>` to stderr. The two values are the time from Main to the
  handler (provider setup happens here) and from Main to RunAsync returning (adds ForceFlush,
  the grace delay, and dispose). The script uses this to split case 3 into its parts.
- `otlp-receiver/otlp-receiver.cs`: a minimal OTLP receiver (runfile, ASP.NET Core). It accepts
  gRPC on one port and http/protobuf on another, answers every export with success, and drops
  the payload. `GET /stats` on the HTTP port returns export counts.
- `run-benchmark.sh`: publishes, starts the receiver, runs hyperfine and the timeline, and
  writes `results/<date>-telemetry-overhead.md`. Raw hyperfine exports go to `results/raw/<date>/`.

## Prerequisites

- .NET SDK from `global.json`
- [hyperfine](https://github.com/sharkdp/hyperfine) on `PATH` (`apt install hyperfine`, or the
  release binary)
- `curl`
- Free local ports 24317 and 24318 for the receiver, and nothing listening on 24319

## Run

```bash
./benchmarks/telemetry-overhead/run-benchmark.sh
```

The script clears every `OTEL_*` variable from the environment, so a shell set up for another
collector does not leak into the cases. It takes about 6 minutes: the endpoint cases are about
1 s per run because of the grace delay in `NuruApp.FlushTelemetryAsync`.

| Variable | Default | Meaning |
|:---|---:|:---|
| `FAST_RUNS` / `FAST_WARMUP` | 500 / 20 | hyperfine runs for cases 1 and 2 |
| `SLOW_RUNS` / `SLOW_WARMUP` | 30 / 3 | hyperfine runs for cases 3 and 4 |
| `TIMELINE_RUNS` | 30 | runs per case for the timeline means |
| `GRPC_PORT` / `HTTP_PORT` | 24317 / 24318 | receiver ports |
| `CLOSED_PORT` | 24319 | port for case 4. The script refuses to run if it has a listener |
| `SKIP_PUBLISH` | 0 | `1` reuses `publish/` from an earlier run |

The script overwrites that day's results file. Add the hand-written analysis (the
`## Analysis` section in earlier results files) after the run.

## Start the receiver by hand

```bash
dotnet run benchmarks/telemetry-overhead/otlp-receiver/otlp-receiver.cs -- 24317 24318
# another shell:
OTEL_EXPORTER_OTLP_ENDPOINT=http://127.0.0.1:24317 \
  benchmarks/telemetry-overhead/publish/aot/bench-telemetry-on/bench-telemetry-on greet --name x
curl -s http://127.0.0.1:24318/stats
```

For http/protobuf, set `OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf` and use port 24318. A real
collector also works, for example the `otel/opentelemetry-collector` container that 517 used.
A real collector does more work per export, so expect slightly higher case 3 numbers.

## Not in CI

This benchmark is not part of CI. See the latest results file for whether it is stable enough
to track there (task 501).
