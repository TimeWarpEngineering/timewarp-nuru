# Round 1 — merged findings
**Date:** 2026-10-14
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: tests/ci-tests/run-ci-tests.cs:72
- Description: The parallel standalone `dotnet run` invocations each build and repack the shared
  ProjectReferences (`timewarp-nuru`, `timewarp-nuru-analyzers`, `timewarp-nuru-build`) because of
  `GeneratePackageOnBuild`. Up to `ProcessorCount` processes therefore write the same `obj/Debug` trees
  and the same `artifacts/packages/TimeWarp.Nuru*.nupkg` files at once. I confirmed the repack with
  `dotnet build tests/timewarp-nuru-tests/generator/generator-53-invalid-route-patterns.cs`. This is the
  race that `verify-samples` now avoids. Here it is unguarded, so it can cause flaky merge-gate failures
  or a torn nupkg in the master `Packages-*` artifact (no layout check runs after step 5).
- Suggestion: Stop the standalone builds from building or packing the shared projects. Options: a global
  `-p:GeneratePackageOnBuild=false` or `-p:BuildProjectReferences=false`; or `dotnet build --no-dependencies`
  then `dotnet run --no-build`. Then confirm the standalone phase logs no "Successfully created package".
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: tests/Directory.Build.props:16
- Description: The analyzer and code-style switches sit at `tests/` scope. They apply to every
  single-file test run, both local and the 28 standalone CI runs, not only the multi-mode compile they
  were measured on. As a result, test code is no longer linted anywhere, RCS1037 trailing whitespace
  included. The comment and Decisions do not say this.
- Suggestion: Move the three properties to `tests/ci-tests/Directory.Build.props`, or document that
  dropping test-code lint everywhere is intended.
- Source: general
- Disposition notes:

### M3 — Severity: nit — Status: open
- File: tools/dev-cli/endpoints/verify-samples-command.cs:142
- Description: The failed-sample summary is in completion order, so it changes from run to run.
- Suggestion: Print `failedSamples.Order()`.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None. There was one reviewer.

## Out-of-scope observation

- Test-phase Debug builds repack into the configuration-independent `artifacts/packages/`, overwriting
  the Release nupkg that step 3 layout-checked. This was already true before this diff. Check separately
  whether promoted master artifacts contain Debug assemblies. See `general.md`.
