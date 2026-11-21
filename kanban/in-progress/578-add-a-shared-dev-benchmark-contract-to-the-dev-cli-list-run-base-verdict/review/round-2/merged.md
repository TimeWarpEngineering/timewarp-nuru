# Round 2 — merged findings
**Date:** 2026-11-21
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-devcli/build/TimeWarp.Nuru.DevCli.props:24
- File: source/timewarp-nuru-devcli/content/any/benchmarks/process-invocation-benchmark.cs:24
- File: source/timewarp-nuru-devcli/content/any/benchmarks/reported-metric-benchmark.cs:25
- Description: Round 1 found that `ProcessInvocationBenchmark` and `ReportedMetricBenchmark` were internal classes in the always-compiled `content/any/services/*.cs` glob, so a consumer that did not construct them failed CA1812 (`tests/devcli-consumer-check`, two errors). The fix moves both files to `content/any/benchmarks/`, which the props do not glob, and documents an opt-in `Compile` include through `$(NuruDevCliContentDirectory)`.
- Suggestion: Keep CA1812 enabled. Compile each ready-made file only in a repo whose `AddRepoBenchmarks` constructs that type.
- Source: general
- Disposition notes: Fixed in `744cd3a5`. Verified on the fix diff. The consumer check imports the props and does not compile `content/any/benchmarks/`. Recorded `dotnet build` in `tests/devcli-consumer-check` is 0 warnings, 0 errors. `tools/dev-cli/Directory.Build.props` compiles both files, and `benchmarks/nuru-benchmarks.cs` constructs both. No suppression and no dummy `new`. The recorded build, list smoke, runfile tests, and audit cover the delta.

## New findings

- None.

## Duplicates / conflicts

- One prior finding, carried forward as fixed. No overlap and no new findings.
