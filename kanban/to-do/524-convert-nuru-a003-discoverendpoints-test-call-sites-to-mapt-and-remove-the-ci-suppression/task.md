# Convert NURU_A003 DiscoverEndpoints() test call sites to Map<T>() and remove the CI suppression

## Description

Task 522 added NURU_A003: it reports an unfiltered `DiscoverEndpoints()` in a compilation that builds apps in two or more source files. In the CI multi-mode assembly it reports 49 call sites across 7 test files, and each of those apps pulls in all 121 `[NuruRoute]` endpoints. `tests/ci-tests/Directory.Build.props` suppresses NURU_A003 through `NoWarn`, with a comment naming task 522, so CI stays green until the call sites are converted. This task does the conversion and removes the suppression.

Root cause this fixes: `artifacts/generated/run-ci-tests.cs/TimeWarp.Nuru.Analyzers/TimeWarp.Nuru.Generators.NuruGenerator/NuruGenerated.g.cs` was about 230 MB and about 4.9 million lines for 992 apps. Roughly 80 `DiscoverEndpoints()` apps, at about 62,600 generated lines each, make up about 85% of that file (7,095 `__JsonBound_*` classes). CI test compile time grew from about 2.5 min (2026-10-18) to 7-13 min, and GitHub runners intermittently die with "The runner has received a shutdown signal" during Step 5 Test, with no test failing.

## Checklist

- [ ] List the 49 call sites: build the CI test assembly with the NURU_A003 `NoWarn` removed and record every reported location
- [ ] Convert each site to explicit `.Map<TEndpoint>()` for the endpoints that test actually uses
- [ ] Keep any test that exercises discovery itself standalone, or use `#pragma warning disable NURU_A003` with a reason (none of the 49 are expected to need this)
- [ ] Remove the NURU_A003 `NoWarn` from `tests/ci-tests/Directory.Build.props`
- [ ] Update the line in `.agent/local/nuru-specific.md` that says the suppression remains until 522 converts call sites
- [ ] `ganda runfile cache --clear`, then a full `dotnet run tests/ci-tests/run-ci-tests.cs` is green
- [ ] Measure before and after: `NuruGenerated.g.cs` size and line count, local multi-mode compile time, and, after merge, the GitHub CI compile time (from "Running CI test suite" to "Multi-Mode Test Runner" in the job log)
- [ ] Record the numbers in Results

## Notes

- Use `Map<T>()` in tests, not `DiscoverEndpoints()`.
- No trailing whitespace (build fails with RCS1037).
- Never move analyzer diagnostics to Shipped; they stay in `AnalyzerReleases.Unshipped.md`.
- Follow-ups if CI is still slow: split the CI assembly in two; a design note on how multi-mode Jaribu compilation interacts with Nuru code generation.
- Related: 522 (the analyzer), 521.

## Session

- Created: 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-10-23)
