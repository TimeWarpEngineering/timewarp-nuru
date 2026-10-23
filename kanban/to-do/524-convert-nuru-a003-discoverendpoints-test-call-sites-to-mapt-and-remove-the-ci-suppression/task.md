# Convert NURU_A003 DiscoverEndpoints() test call sites to Map<T>() and remove the CI suppression

## Description

Task 522 added NURU_A003: it reports an unfiltered `DiscoverEndpoints()` in a compilation that builds apps in two or more source files. In the CI multi-mode assembly it reports 49 call sites across 7 test files, and each of those apps pulls in all 121 `[NuruRoute]` endpoints. `tests/ci-tests/Directory.Build.props` suppresses NURU_A003 through `NoWarn`, with a comment naming task 522, so CI stays green until the call sites are converted. This task does the conversion and removes the suppression.

Root cause this fixes: `artifacts/generated/run-ci-tests.cs/TimeWarp.Nuru.Analyzers/TimeWarp.Nuru.Generators.NuruGenerator/NuruGenerated.g.cs` was about 230 MB and about 4.9 million lines for 992 apps. Roughly 80 `DiscoverEndpoints()` apps, at about 62,600 generated lines each, make up about 85% of that file (7,095 `__JsonBound_*` classes). CI test compile time grew from about 2.5 min (2026-10-18) to 7-13 min, and GitHub runners intermittently die with "The runner has received a shutdown signal" during Step 5 Test, with no test failing.

## Goal (implementer restatement)

Stop every app in the CI multi-mode test assembly from pulling all ~121 `[NuruRoute]` endpoints in through an unfiltered `DiscoverEndpoints()`. Each such call in a multi-included test file becomes explicit `.Map<TEndpoint>()` for the endpoints that test uses (or a scoped `DiscoverEndpoints(typeof(Group))` / `#pragma` with a reason where discovery itself is under test). Then the temporary `NURU_A003` entry leaves `NoWarn` in `tests/ci-tests/Directory.Build.props`, and the nuru-specific agent note stops saying the suppression remains.

Acceptance:
- The CI assembly builds with NURU_A003 not suppressed and reports zero NURU_A003.
- `ganda runfile cache --clear` then `dotnet run tests/ci-tests/run-ci-tests.cs` is green.
- Before/after numbers recorded in Results: `NuruGenerated.g.cs` size and line count, local multi-mode compile time. GitHub CI compile time is measured after merge (post-merge item).

## Checklist

- [x] List the 49 call sites: build the CI test assembly with the NURU_A003 `NoWarn` removed and record every reported location
- [x] Convert each site to explicit `.Map<TEndpoint>()` for the endpoints that test actually uses
- [x] Keep any test that exercises discovery itself standalone, or use `#pragma warning disable NURU_A003` with a reason (none of the 49 are expected to need this)
- [x] Remove the NURU_A003 `NoWarn` from `tests/ci-tests/Directory.Build.props`
- [x] Update the line in `.agent/local/nuru-specific.md` that says the suppression remains until 522 converts call sites
- [x] `ganda runfile cache --clear`, then a full `dotnet run tests/ci-tests/run-ci-tests.cs` is green
- [ ] Measure before and after (local numbers done; GitHub CI compile time is measured after merge): `NuruGenerated.g.cs` size and line count, local multi-mode compile time, and, after merge, the GitHub CI compile time (from "Running CI test suite" to "Multi-Mode Test Runner" in the job log)
- [x] Record the numbers in Results

## Notes

- Use `Map<T>()` in tests, not `DiscoverEndpoints()`.
- No trailing whitespace (build fails with RCS1037).
- Never move analyzer diagnostics to Shipped; they stay in `AnalyzerReleases.Unshipped.md`.
- Follow-ups if CI is still slow: split the CI assembly in two; a design note on how multi-mode Jaribu compilation interacts with Nuru code generation.
- Related: 522 (the analyzer), 521.

## Session

- Created: 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-10-23)
- Implement (2026-10-23): converted 49 sites, removed the NoWarn, measured, CI suite green.

## Results

Built the CI assembly with the NURU_A003 `NoWarn` removed. The generator reported 49 sites in 7 files, all inside the multi-mode compile:

| File | Sites | Mapped endpoints |
|------|------:|------------------|
| `auto/endpoint-nullable-option-01.cs` | 7 | `NullLongEndpoint`, `NullIntEndpoint`, `NullMultiEndpoint` (one per test) |
| `generator/generator-11-endpoints.cs` | 6 | `GreetCommand`, `DeployCommand`, `BuildCommand`, `ExecCommand` |
| `generator/generator-18-partial-class-options.cs` | 4 | `PartialTestCommand` |
| `group-options/group-options-01-basic.cs` | 18 | the one `Task419*Command` each test runs |
| `routing/routing-26-optional-int-skip.cs` | 3 | both `Issue152WorkspaceCommitsCommand` and `Issue152RepoSetupCommand` in every test, because the regression is the skip past the commits route |
| `routing/routing-27-nested-route-groups.cs` | 4 | the one `Issue160*Command` each test runs |
| `routing/routing-28-group-alias.cs` | 7 | the one `Issue178*Command` each test runs |

None of the 49 tests exercises discovery itself, so none needed `#pragma warning disable NURU_A003`. The other `DiscoverEndpoints()` calls in `tests/` are either scoped (`DiscoverEndpoints(typeof(Group))`), in standalone-only files (`CiTestExcludes` / `#if !JARIBU_MULTI`), or inside Roslyn-hosted source strings. None of them reports.

Other changes:
- `tests/ci-tests/Directory.Build.props`: removed `NURU_A003` from `NoWarn` and removed the TEMPORARY comment.
- `.agent/local/nuru-specific.md`: the line now says the CI assembly does not suppress NURU_A003.
- `generator-11-endpoints.cs`: the section comment now says each test maps its command with `Map<T>()`.

Measurements (local, WSL, warm ProjectReferences, `ganda runfile cache --clear` before each run-ci-tests compile):

| Metric | Before | After |
|--------|-------:|------:|
| `NuruGenerated.g.cs` size | 230,588,136 B (230.6 MB) | 107,257,345 B (107.3 MB) |
| `NuruGenerated.g.cs` lines | 5,003,970 | 1,787,521 |
| `class __JsonBound_*` count | 7,095 | 1,218 |
| `dotnet build tests/ci-tests/run-ci-tests.cs` | 4m00s (first cold run 4m43s) | 1m03s |

Full suite after `ganda runfile cache --clear`: `dotnet run tests/ci-tests/run-ci-tests.cs` exited 0 in 3m22s wall time. Multi-mode had 2,117 tests: 2,111 passed, 6 skipped, 0 failed. Standalone phase: 0 failed.

`ganda repo audit`: passes, nothing to fix.

Post-merge (still open): read the GitHub CI compile time from the job log, from "Running CI test suite" to "Multi-Mode Test Runner", and compare it with the 7-13 min range. The generated file is still 107 MB, so if CI is still slow the follow-ups in Notes apply: split the assembly in two, or write the design note.

### How to validate

Smoke:
```bash
ganda runfile cache --clear
dotnet build tests/ci-tests/run-ci-tests.cs 2>&1 | grep -c NURU_A003
ls -l artifacts/generated/run-ci-tests.cs/TimeWarp.Nuru.Analyzers/TimeWarp.Nuru.Generators.NuruGenerator/NuruGenerated.g.cs
dotnet run tests/ci-tests/run-ci-tests.cs
```

Expect: the grep count is `0` and the build succeeds with NURU_A003 not in `NoWarn`. `NuruGenerated.g.cs` is about 107 MB, not 230 MB. The test run exits 0 with 0 failed in both the multi-mode and standalone phases.

