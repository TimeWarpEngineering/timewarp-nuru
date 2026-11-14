# Telemetry overhead benchmark: AOT startup with telemetry off, on with no endpoint, and on with an endpoint

## Description

Add a benchmark project that measures what `UseTelemetry()` costs a Native AOT Nuru CLI per run, in the three cases a user meets. 517 measured these with a JIT build: about 35 ms with no endpoint after its fix, about 1.2 s with an endpoint set, and about 120 ms of provider setup that it attributed mostly to first-run JIT. Shipped CLIs are AOT, so the JIT numbers do not say what users pay. This benchmark replaces the guesses with repeatable AOT numbers.

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

- [ ] Benchmark apps for the off and on variants, AOT-published
- [ ] Local OTLP receiver for the reachable case, with start instructions
- [ ] `run-benchmark.sh` covering cases 1 to 4 plus the JIT comparison
- [ ] Results file with the numbers and the grace-delay breakdown
- [ ] Readme in `benchmarks/telemetry-overhead/` saying how to run it

## Notes

- Depends on 517 (PR #347): measure after its no-provider fix is on master, so case 2 reflects the fix.
- 517 follow-ups this informs: skip the 1 s grace delay after a successful `ForceFlush()`; daemon-hosted providers (likely unnecessary if AOT setup is cheap).
- Related: 501 (startup performance and benchmark suite), `benchmarks/aot-benchmarks/`.

## Session

- Created: 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-10-22), from Steven asking for a real benchmark of the three telemetry cases after 517.
