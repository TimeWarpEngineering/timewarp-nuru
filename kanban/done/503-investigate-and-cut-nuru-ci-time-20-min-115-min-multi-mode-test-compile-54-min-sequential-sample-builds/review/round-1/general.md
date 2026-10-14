# Round 1 — general
**Date:** 2026-10-14
**Scope reviewed:** same as framework (branch vs master, 5 product files)

## Summary

The change adds a NuGet cache to setup-dotnet. It turns off diagnostic analyzers and code style for every
build under `tests/`. Samples now build in parallel: three shared projects build once, then each sample
builds with `--no-dependencies`. The 28 standalone tests now run in parallel, and each step prints how long
it took.

The workflow, timing, and sample changes are correct. Every sample references only `timewarp-nuru` and
`timewarp-nuru-analyzers`, so `--no-dependencies` after the shared prebuild is safe. Nuru has no
`DiagnosticAnalyzer` types; all NURU diagnostics come from the generator, so `RunAnalyzersDuringBuild=false`
keeps them. `dotnet build tools/dev-cli/dev.cs -c Release` builds clean.

The main risk is the standalone fan-out. It runs into the same package race that the samples change works
around, and it does nothing to prevent it.

## Issues

### Issue 1 — Severity: bug
- File: tests/ci-tests/run-ci-tests.cs:72
- Description: Every standalone `dotnet run <test>.cs` builds the shared ProjectReferences from
  `tests/Directory.Build.props`. Because of `GeneratePackageOnBuild`, each build also repacks those projects
  into `artifacts/packages/`. I checked this locally: `dotnet build tests/timewarp-nuru-tests/generator/generator-53-invalid-route-patterns.cs`
  printed "Successfully created package … TimeWarp.Nuru.Analyzers.3.0.0-beta.80.nupkg" and
  "… TimeWarp.Nuru.3.0.0-beta.80.nupkg", even with warm outputs. With `Parallel.ForEachAsync` at
  `ProcessorCount`, up to N processes build the same `obj/Debug` trees at once. They also write the same
  two nupkg paths at once. This is the race the implementer found for samples and fixed in
  `verify-samples-command.cs` (shared prebuild plus `--no-dependencies`). The standalone path has no
  such guard. There are two consequences. Standalone runs can fail at random on the merge gate. And on
  master, `Upload Artifacts` publishes `artifacts/packages/*.nupkg` after the test step, and no layout
  check runs after the standalone phase. A torn TimeWarp.Nuru nupkg from overlapping writes could become
  the promoted `Packages-*` artifact. The local 32-wide pass does not rule this out: the race is timing-dependent.
- Suggestion: Keep the standalone builds from building or packing the shared projects. The multi-mode
  compile has already built them in Debug just before, so options include: pass a global
  `-p:GeneratePackageOnBuild=false` (or `-p:BuildProjectReferences=false`) to each standalone run. Or
  build each file with `dotnet build … --no-dependencies` and then `dotnet run --no-build`. Or build
  all 28 files one at a time and run them in parallel with `--no-build`. Then confirm the step 5 log
  shows no "Successfully created package" lines from the standalone phase.
- Status: open

### Issue 2 — Severity: suggestion
- File: tests/Directory.Build.props:16
- Description: `RunAnalyzersDuringBuild=false`, `EnforceCodeStyleInBuild=false`, and `ReportAnalyzer=false`
  are in `tests/Directory.Build.props`, so they apply to every build under `tests/`. That covers each
  standalone `dotnet run` of a single test file, both in CI and on a dev box, not only the multi-mode
  compile that costs the 437 s. As a result, nothing in CI or in the local loop enforces analyzers or
  code style on test code anymore, including the repo rule "NO TRAILING WHITESPACE — build fails with
  RCS1037". The comment explains why the multi-mode compile is slow. It does not say that test-code
  lint is gone everywhere.
- Suggestion: Move the three properties to `tests/ci-tests/Directory.Build.props`, which imports
  `tests/Directory.Build.props`. That keeps the full multi-mode win. Single-file runs, local and the 28
  standalone CI runs, keep analyzing. If dropping test lint everywhere is intended, say so in the
  props comment and in the task Decisions.
- Status: open

### Issue 3 — Severity: nit
- File: tools/dev-cli/endpoints/verify-samples-command.cs:142
- Description: `failedSamples` is filled in completion order from the parallel loop. The `Failed samples:`
  summary list therefore changes order from run to run, which makes CI logs harder to compare.
- Suggestion: Iterate `failedSamples.Order()` in the summary.
- Status: open

## Out-of-scope observation (not a finding on this diff)

- The Issue 1 repro also shows that test-phase Debug builds overwrite the Release
  `TimeWarp.Nuru*.nupkg` that step 3 packed and layout-checked, because `PackageOutputPath` does not
  depend on configuration. Before this change the multi-mode and sequential standalone runs already did
  this. If master's `Packages-*` artifact is the one promoted to nuget.org, it may contain Debug-built
  assemblies. Check this separately: inspect a promoted nupkg's `TimeWarp.Nuru.dll` for
  `DebuggableAttribute` / optimizations. The fix for Issue 1 (`GeneratePackageOnBuild=false` for
  test-phase builds) would also stop this for the standalone phase.
