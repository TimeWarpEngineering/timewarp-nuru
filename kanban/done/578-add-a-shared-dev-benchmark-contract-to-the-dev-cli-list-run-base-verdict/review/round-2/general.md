# Round 2 — general
**Date:** 2026-11-21
**Scope reviewed:** fix delta since round 1, commit `744cd3a5` (`fix(devcli): make ready-made benchmarks opt-in so consumers pass CA1812`) and the attest-retry note `5021e768`. Not the whole branch.

## Summary

Round 1's M1 is fixed. `ProcessInvocationBenchmark` and `ReportedMetricBenchmark` now live in `content/any/benchmarks/`, and `build/TimeWarp.Nuru.DevCli.props` does not compile that folder. A consumer that registers nothing, which is what `tests/devcli-consumer-check` does, no longer compiles the two internal classes, so CA1812 stays on without a suppression or a dummy `new`. Nuru's own dev CLI compiles both files from `tools/dev-cli/Directory.Build.props` and constructs both in `benchmarks/nuru-benchmarks.cs`. The readme, changelog, and the two test `Directory.Build.props` files point at the new paths. The recorded fix-pass results cover this delta: `./bin/dev build` with 0 warnings, `dotnet build` in `tests/devcli-consumer-check` with 0 warnings (the M1 gate), self-install and `benchmark list`, the three benchmark runfiles (9, 9, 3), the ci-tests compile, and `ganda repo audit` after the runfile executable-bit fix.

The implementer restatement still says the DevCli version is bumped. The original task does not require a bump, and the branch stays on unreleased 4.0.0-beta.17. That misread is already under `## Learnings`.

One targeted command checked a suspected hole in the new opt-in: a `Compile` include of `$(NuruDevCliContentDirectory)` written in `Directory.Build.props`, which the SDK imports before NuGet package props. Command: `dotnet msbuild /tmp/nuru-578-prop-order/check.proj -t:Show -nologo -v:m`, a throwaway project that imports a consumer-style `Directory.Build.props` and then `TimeWarp.Nuru.DevCli.props` in the same order as `Microsoft.Common.props`. A property read at props time was empty (`benchmarks/process-invocation-benchmark.cs`). The `Compile` item, expanded after properties, was `build/../content/any/benchmarks/process-invocation-benchmark.cs` and that file exists. Mediator and jaribu keep their `Compile` items in `tools/dev-cli/Directory.Build.props`, and that item still resolves. Not a defect.

No new issues.
