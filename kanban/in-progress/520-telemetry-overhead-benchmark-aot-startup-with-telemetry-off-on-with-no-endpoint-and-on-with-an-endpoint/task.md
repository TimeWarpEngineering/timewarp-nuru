# Telemetry overhead benchmark: AOT startup with telemetry off, on with no endpoint, and on with an endpoint

## Description

Add a benchmark project that measures what `UseTelemetry()` costs a Native AOT Nuru CLI per run, in the three cases a user meets. 517 measured these with a JIT build: about 35 ms with no endpoint after its fix, about 1.2 s with an endpoint set, and about 120 ms of provider setup that it attributed mostly to first-run JIT. Shipped CLIs are AOT, so the JIT numbers do not say what users pay. This benchmark replaces the guesses with repeatable AOT numbers.

## Goal (implementer restatement)

Put repeatable AOT numbers on what `UseTelemetry()` costs one CLI run, so 517's JIT guesses are replaced.

Acceptance:
- `benchmarks/telemetry-overhead/` has an off app and an on app that run the same one-line command and differ only by `UseTelemetry()`, both AOT-published.
- A local OTLP receiver (in-repo) accepts exports for case 3, with documented start steps.
- `run-benchmark.sh` drives hyperfine `-N` with warmup over: off, on with no endpoint, on with a reachable endpoint, on with a closed port, plus JIT runs of off and on-no-endpoint.
- A dated results file reports mean, σ, min, max per case, machine/OS/SDK/Nuru version, splits case 3 into the grace delay versus setup and export, and says whether it is stable enough for CI (501).
- A readme says how to run it. No product code changes; any defect found goes under Notes and into a filed task. Not added to CI.

## Requirements

- New folder `benchmarks/telemetry-overhead/`, following the `benchmarks/aot-benchmarks/` pattern: small AOT-published apps plus a `run-benchmark.sh` that drives `hyperfine` (`-N`, warmup, enough runs for stable means), and a dated results file under `results/`.
- The same trivial command in every variant (for example `greet --name x`, writing one line), so the only difference is telemetry.
- Cases:
  1. **Off:** an app built without `UseTelemetry()`. This is the baseline.
  2. **On, no endpoint:** built with `UseTelemetry()`, `OTEL_EXPORTER_OTLP_ENDPOINT` unset.
  3. **On, endpoint reachable:** endpoint set to a local OTLP receiver that accepts exports. Use a minimal in-repo receiver or a collector container; document which and how to start it.
  4. **On, endpoint unreachable:** endpoint set to a closed local port, to show the failure-path cost.
- Also run cases 1 and 2 as a JIT build (`dotnet` the published dll, or `dotnet run` of a runfile) so the AOT and JIT gap is on record next to 517's numbers.
- Report mean, standard deviation, min and max per case in the results file, with machine, OS, .NET SDK and Nuru version. Break out how much of case 3 is the fixed grace delay in `FlushTelemetryAsync` against provider setup and export.
- Do not add the benchmark to CI in this task. Note in the results whether it looks stable enough to track there (see 501).
- No product code changes. If a measurement shows a defect, record it under Notes and file a task.

## Checklist

- [x] Benchmark apps for the off and on variants, AOT-published
- [x] Local OTLP receiver for the reachable case, with start instructions
- [x] `run-benchmark.sh` covering cases 1 to 4 plus the JIT comparison
- [x] Results file with the numbers and the grace-delay breakdown
- [x] Readme in `benchmarks/telemetry-overhead/` saying how to run it

## Notes

- Depends on 517 (PR #347): measure after its no-provider fix is on master, so case 2 reflects the fix.
- 517 follow-ups this informs: skip the 1 s grace delay after a successful `ForceFlush()`; daemon-hosted providers (likely unnecessary if AOT setup is cheap).
- Related: 501 (startup performance and benchmark suite), `benchmarks/aot-benchmarks/`.
- Defects found (filed as **590**, in to-do): (1) the fixed 1000 ms grace delay in `FlushTelemetryAsync` is 98.8% of the AOT cost with an endpoint (1000 of 1012 ms); (2) a blackholed endpoint (`http://192.0.2.1:4317`) costs about 17 s per run with the default exporter timeout (4.0 s at `OTEL_EXPORTER_OTLP_TIMEOUT=1000`). A closed local port costs the same as a reachable receiver.
- AOT provider setup is 4.8 ms (JIT: 129 ms), so daemon-hosted providers are not worth building for startup cost.
- `ICSharpCode.Decompiler.dll` is copied next to the in-repo AOT binaries through the analyzer `ProjectReference`; harmless, not loaded, NuGet consumers unaffected. No task.

## Session

- Created: 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-10-22), from Steven asking for a real benchmark of the three telemetry cases after 517.
- 2026-11-15 implementer (ganda task work): built `benchmarks/telemetry-overhead/`, ran the full benchmark, filed 590. Installed hyperfine 1.19.0 to `~/.local/bin` (it was missing on this machine).
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session b5a23b14-2325-41a2-a306-eb79624b9ba8, max-turns 500, Done — 2026-11-14T20:38:02Z
- Review oracle round 1: Grok 4.7 (grok, vendor xai), body tw-implementation-review, effort 3, roster general, turn cap 200 (cursor session uncapped; no --max-turns flag), session not reported, disposition clean — 2026-11-15. No findings. The restatement matches the task; no misread. See `review/round-1/merged.md` and `review/disposition.md`.
- Review oracle: review by reviewer-cursor (cursor, vendor xai, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 200), artifacts general.md, merged.md, disposition.md — 2026-11-14T20:46:34Z

## Results

Added `benchmarks/telemetry-overhead/`:

- `bench-telemetry-off/`, `bench-telemetry-on/`: two AOT apps that compile the same `shared/program.cs` (`greet --name x`, one line). The on project defines `BENCH_TELEMETRY`, which adds `.UseTelemetry()`. `shared/bench-timeline.cs` writes Main-to-handler and Main-to-exit times to stderr when `NURU_BENCH_TIMELINE=1`; that splits case 3.
- `otlp-receiver/otlp-receiver.cs`: in-repo minimal OTLP receiver runfile (gRPC h2c and http/protobuf, always answers success, `GET /stats` counts exports).
- `run-benchmark.sh`: publishes AOT and JIT builds, starts the receiver, runs hyperfine `-N` (cases 1 and 2 AOT and JIT, 500 runs; cases 3 gRPC and http/protobuf, 4, and 3 JIT, 30 runs), runs the timeline, and writes the dated results file. Clears `OTEL_*` first and refuses busy ports.
- `results/2026-11-15-telemetry-overhead.md` (+ `results/raw/2026-11-15/`): numbers, environment, grace-delay breakdown, AOT against JIT against 517, CI stability note.
- `readme.md`: prerequisites, how to run, knobs, how to start the receiver by hand.

Headline numbers (AOT, Ryzen 9 7950X3D, WSL2, SDK 11.0.100, Nuru 4.0.0-beta.14):

| Case | Mean ± σ | Over off |
|:---|---:|---:|
| 1 off | 4.1 ± 0.2 ms | — |
| 2 on, no endpoint | 5.4 ± 0.3 ms | +1.3 ms |
| 3 on, reachable (gRPC) | 1016 ± 1 ms | +1012 ms (setup 4.8, grace delay 1000, flush+dispose 3.2, outside Main 4.0) |
| 4 on, closed port | 1015 ± 1 ms | +1011 ms |
| JIT: 1 / 2 / 3 | 30.5 / 37.0 / 1224 ms | provider setup 129 ms under JIT against 4.8 ms AOT |

Follow-up filed: **590** (skip the grace delay after a successful ForceFlush; bound flush time, since a blackholed endpoint costs about 17 s per run). No product code changed.

Boy-scout: `ganda repo audit --fix` cleared two advisory warnings. `memsearch-scaffold` added the ganda-managed `.githooks/prepare-commit-msg` hook and `vscode-window-icon` wrote the ignored `.vscode/settings.json`.

### Gates

| Gate | Command | Result |
|:---|:---|:---|
| Build | `./bin/dev build` | PASS: 0 warnings, 0 errors |
| Test | `./bin/dev test` | PASS: multi-mode 2233 total, 2227 passed, 6 skipped, 0 failed; standalone 48 files, 0 failed |
| Audit | `ganda repo audit` | PASS with 2 advisory warnings (memsearch-scaffold, vscode-window-icon). After `ganda repo audit --fix`: passes all checks |

The benchmark folder is outside the solution, so build and test do not cover it. It was checked by AOT- and JIT-publishing both apps with no warnings and by one full `run-benchmark.sh` run.

### How to validate

Smoke:

```bash
dotnet publish benchmarks/telemetry-overhead/bench-telemetry-on -c Release -o benchmarks/telemetry-overhead/publish/aot/bench-telemetry-on
dotnet run benchmarks/telemetry-overhead/otlp-receiver/otlp-receiver.cs -- 24317 24318 &   # wait for "otlp-receiver listening"
env -u OTEL_EXPORTER_OTLP_ENDPOINT NURU_BENCH_TIMELINE=1 benchmarks/telemetry-overhead/publish/aot/bench-telemetry-on/bench-telemetry-on greet --name x
env OTEL_EXPORTER_OTLP_ENDPOINT=http://127.0.0.1:24317 NURU_BENCH_TIMELINE=1 benchmarks/telemetry-overhead/publish/aot/bench-telemetry-on/bench-telemetry-on greet --name x
curl -s http://127.0.0.1:24318/stats
# full run (about 6 min, needs hyperfine): ./benchmarks/telemetry-overhead/run-benchmark.sh
```

Expect: both runs print `Hello, x`. The no-endpoint run prints `timeline handler=<under 1> end=<under 1>`. The endpoint run prints `handler` of about 5 ms and `end` of about 1003 ms. `/stats` shows one `grpc …TraceService` export and two `grpc …MetricsService` exports per endpoint run. A full `run-benchmark.sh` writes `results/<today>-telemetry-overhead.md` with case 1 at about 4 ms, case 2 about 1.3 ms higher, and cases 3 and 4 at about 1.015 s.

### Review

Round 1 (2026-11-15): tw-implementation-review, effort 3, roster general, Grok 4.7, turn cap 200 (cursor uncapped; no `--max-turns` flag), session not reported. Disposition clean. Counts: bug 0 open / 0 fixed / 0 wontfix; suggestion 0; nit 0. The restatement matches the task; no misread. Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.

## Blocked

2026-11-14: unsandboxed git add of the written kitchen paths under kanban/in-progress/520-telemetry-overhead-benchmark-aot-startup-with-telemetry-off-on-with-no-endpoint-and-on-with-an-endpoint (git index is outside the worktree sandbox)
