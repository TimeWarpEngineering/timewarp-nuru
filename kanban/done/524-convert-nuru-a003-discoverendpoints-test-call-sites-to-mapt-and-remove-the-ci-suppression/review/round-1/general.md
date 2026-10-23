# Round 1 — general
**Date:** 2026-10-23
**Scope reviewed:** branch `task/524-convert-nuru-a003-discoverendpoints-test-call-site` vs `master` (`dbf5f780`). Seven test files, `tests/ci-tests/Directory.Build.props`, `.agent/local/nuru-specific.md`.

## Summary

The branch replaces 49 unfiltered `DiscoverEndpoints()` calls in the CI multi-mode assembly with `Map<T>()` for the endpoint each test runs, and it removes `NURU_A003` from `NoWarn`. Each mapped type's route matches that test's `RunAsync` arguments. `routing-26-optional-int-skip.cs` maps both `Issue152WorkspaceCommitsCommand` and `Issue152RepoSetupCommand` in every test, which is the skip the regression guards. The other six files map the single command the test invokes, including the group-option `--help` tests, whose asserted options live on that command's route-group bases. No test needed a `#pragma`. Remaining `.DiscoverEndpoints()` calls are scoped (`typeof(...)`), compiled only under `#if !JARIBU_MULTI` (`generator-19`, `generator-20`), or string literals inside Roslyn-hosted files listed in `CiTestExcludes`. `NURU_A003` is not in any other `NoWarn`. The agent note now says the CI assembly does not suppress it. The on-disk `NuruGenerated.g.cs` is 107,257,345 bytes and 1,787,521 lines, matching Results. The implementer restatement matches the task. Risk is low.

## Issues

<!-- none -->
