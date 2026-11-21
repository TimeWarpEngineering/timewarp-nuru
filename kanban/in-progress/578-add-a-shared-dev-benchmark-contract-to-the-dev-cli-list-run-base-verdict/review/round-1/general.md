# Round 1 — general
**Date:** 2026-11-21
**Scope reviewed:** branch `task/578-add-a-shared-dev-benchmark-contract-to-the-dev-cli` vs merge-base `aee2e073` (`master...HEAD`)

## Summary

The branch adds the shared `dev benchmark` contract in the DevCli source-only package: `list`, `run --base`, interleaved samples, a head/base ratio with a noise band, and a `win` / `noise` / `regression` verdict. Nuru registers `aot-startup` and `operation-dispatch`. The stored absolute baseline and the script that wrote it are gone. The restatement matches the original task; the extra "version is bumped" line is recorded under `## Learnings` and is not a defect. Statistics, the runner, the checkout, and the JSON shape match the documented contract, and the recorded Jaribu runs and smokes cover those paths. The shipped ready-made benchmark types fail the consumer compile that task 573 keeps CA1812 on for.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru-devcli/content/any/services/process-invocation-benchmark.cs:24
- File: source/timewarp-nuru-devcli/content/any/services/reported-metric-benchmark.cs:25
- Description: `ProcessInvocationBenchmark` and `ReportedMetricBenchmark` are internal classes in content that `build/TimeWarp.Nuru.DevCli.props` always compiles (`content/any/services/*.cs`). A consumer build treats CA1812 as an error (task 573; `tests/devcli-consumer-check` sets `NuruConsumerCheck` so root props leave CA1812 out of `NoWarn`). Nothing in that compilation constructs either type unless the repo's `AddRepoBenchmarks` does. A repo that registers no benchmarks, or only one of the two, fails to compile. That breaks the readme promise that a repo with no benchmarks writes nothing, and it fails Nuru's samples-stage consumer check (`.github/workflows/workflow.yml` "DevCli consumer check"). Nuru's own `./bin/dev` build stays quiet because root props suppress CA1812 there and `tools/dev-cli/benchmarks/nuru-benchmarks.cs` constructs both types. The recorded `## Results` do not include this gate.
- Checked: `dotnet build tests/devcli-consumer-check/devcli-consumer-check.csproj --nologo -v q` (one project, to test the suspected CA1812 break). Result: 2 errors, 0 warnings. CA1812 at `process-invocation-benchmark.cs(24,23)` and `reported-metric-benchmark.cs(25,23)`.
- Suggestion: Keep CA1812 enabled. Do not suppress it, and do not add a `new` whose only job is to satisfy the analyzer (task 573). Move the two ready-made types out of the always-compiled `services/*.cs` glob, and compile each file only in a repo that constructs that type from `AddRepoBenchmarks`. `dotnet build` in `tests/devcli-consumer-check` must finish with 0 warnings. Point the DevCli readme and `tools/dev-cli/Directory.Build.props` at the new include.
- Status: open
