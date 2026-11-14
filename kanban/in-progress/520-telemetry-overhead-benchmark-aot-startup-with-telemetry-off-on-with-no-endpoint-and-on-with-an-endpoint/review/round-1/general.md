# Round 1 — general
**Date:** 2026-11-15
**Scope reviewed:** branch `task/520-telemetry-overhead-benchmark-aot-startup-with` vs `master`. `benchmarks/telemetry-overhead/` (apps, receiver, script, readme, dated results and raw hyperfine exports) and the audit-scaffolded `.githooks/prepare-commit-msg` runfile.

## Summary

The benchmark measures one `greet --name x` run. `bench-telemetry-off` and `bench-telemetry-on` compile the same `shared/program.cs`; the on project only appends `BENCH_TELEMETRY`, which adds `.UseTelemetry()`. AOT settings match `benchmarks/aot-benchmarks/` (`PublishAot`, partial trim, speed, invariant globalization, `linux-x64`). `run-benchmark.sh` clears `OTEL_*`, refuses busy receiver ports and a listener on the closed port, publishes AOT and JIT (`-p:PublishAot=false`), and drives hyperfine `-N` with warmup over case 1, case 2, case 3 (gRPC and http/protobuf), case 4, and JIT for cases 1, 2, and 3 gRPC. The in-repo receiver answers gRPC `/{service}/Export` and `POST /v1/{signal}`, and `GET /stats` is how the script waits and records counts. The readme documents the run and a hand start. Nothing under `.github/` adds this to CI.

`results/2026-11-15-telemetry-overhead.md` has machine, OS, SDK 11.0.100, Nuru 4.0.0-beta.14, mean ± σ, min, and max. Those figures match `results/raw/2026-11-15/*.json` (500 runs for the fast cases, 30 for the endpoint cases, every recorded exit code 0). The case 3 split attributes 1000 ms to `FlushTelemetryAsync`'s `Task.Delay(1000)`, which still runs only after a provider exists (`nuru-app.cs`). The timeline puts handler-to-exit at about 1003 ms for AOT gRPC, and the arithmetic in the hand-written analysis (setup 4.8, flush and dispose 3.2, residual 4.0, 1000/1012 = 98.8%) follows that table. Receiver counts taken before the timeline runs are one trace and two metric exports per hyperfine invocation, including warmup: 66 gRPC traces and 132 gRPC metrics for the two gRPC series, 33 HTTP traces and 66 HTTP metrics for the HTTP series. Published sizes match the results file (`du -h` 4.7M and 12M). `ICSharpCode.Decompiler.dll` is beside both AOT binaries, as the notes say.

The implementer restatement matches the task. Extra http/protobuf and case 3 JIT rows sit on top of the required cases. The recorded build, test, and audit do not compile this folder (it is outside the solution); Results say both apps were AOT- and JIT-published with no warnings and that one full `run-benchmark.sh` produced these files. That record covers the change. Defects stay out of product code: task 590 is in `kanban/to-do/` for the grace delay and the blackholed endpoint. The prepare-commit-msg hook is the disclosed `ganda repo audit --fix` scaffold (task 645), not a telemetry behavior change.

## Issues

No issues were raised.
