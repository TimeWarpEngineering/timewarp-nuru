# Round 1 — merged findings
**Date:** 2026-11-21
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/timewarp-nuru-devcli/content/any/services/process-invocation-benchmark.cs:24
- File: source/timewarp-nuru-devcli/content/any/services/reported-metric-benchmark.cs:25
- Description: `ProcessInvocationBenchmark` and `ReportedMetricBenchmark` are internal classes in content that `build/TimeWarp.Nuru.DevCli.props` always compiles (`content/any/services/*.cs`). A consumer build treats CA1812 as an error (task 573; `tests/devcli-consumer-check` sets `NuruConsumerCheck` so root props leave CA1812 out of `NoWarn`). Nothing in that compilation constructs either type unless the repo's `AddRepoBenchmarks` does. A repo that registers no benchmarks, or only one of the two, fails to compile. That breaks the readme promise that a repo with no benchmarks writes nothing, and it fails Nuru's samples-stage consumer check (`.github/workflows/workflow.yml` "DevCli consumer check"). Nuru's own `./bin/dev` build stays quiet because root props suppress CA1812 there and `tools/dev-cli/benchmarks/nuru-benchmarks.cs` constructs both types. The recorded `## Results` do not include this gate.
- Checked: `dotnet build tests/devcli-consumer-check/devcli-consumer-check.csproj --nologo -v q` (one project, to test the suspected CA1812 break). Result: 2 errors, 0 warnings. CA1812 at `process-invocation-benchmark.cs(24,23)` and `reported-metric-benchmark.cs(25,23)`.
- Suggestion: Keep CA1812 enabled. Do not suppress it, and do not add a `new` whose only job is to satisfy the analyzer (task 573). Move the two ready-made types out of the always-compiled `services/*.cs` glob, and compile each file only in a repo that constructs that type from `AddRepoBenchmarks`. `dotnet build` in `tests/devcli-consumer-check` must finish with 0 warnings. Point the DevCli readme and `tools/dev-cli/Directory.Build.props` at the new include.
- Source: general
- Disposition notes: implement-fix-2 moved both files to `content/any/benchmarks/` (not globbed by the props), added `$(NuruDevCliContentDirectory)` for opt-in `Compile Include`, pointed tools/dev-cli props, test props, readme and changelog at it. `dotnet build` in `tests/devcli-consumer-check` -> 0 warnings, 0 errors.

## Duplicates / conflicts

- One finding. No overlap.
