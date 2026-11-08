# Round 1 — general
**Date:** 2026-11-09
**Scope reviewed:** branch `task/569-devcli-check-version-packable-project-scan-takes-m` at 93650965 vs merge-base d87d67dd (master). Product diff: `packable-project-service.cs`, `packable-projects-01/02/03` tests, `generate-internals-visible-to.cs`, regenerated `internals-visible-to.g.cs` (3 copies), changelog, audit-fix files (`.gitignore`, `.vscode/settings.json` untracked, `.timewarp/line-cap-baseline.json`).

## Summary

The change replaces the sequential per-project `dotnet msbuild -getProperty` loop with
`Parallel.ForEachAsync` bounded by `Environment.ProcessorCount`, stores each result at its
enumerated index so `ValidateDerivedSet` sees input order, collects every non-cancellation
failure, and writes one stderr progress line before the scan. All fail-loud guards (nonzero
exit, unparseable JSON, blank PackageId, duplicate PackageId) are kept and moved into
`ToPackableProject`. No path skip, no XML parse. Design region is updated with measured
numbers. The implementation matches both the task and the implementer restatement; risk is
low. Findings: one suppression added to make a naming analyzer pass, plus two smaller items.

Verified locally: `packable-projects-01-parse` 21/21, `packable-projects-02-derivation-fixture`
4/4 (real MSBuild, includes the new broken-project case), `packable-projects-03-parallel` 8/8.
The 03 file is a `devcli/` test without a `CiTestExcludes` entry, so it joins the CI assembly;
it uses `EvaluateProjectsAsync` directly (no `DiscoverEndpoints`), so no NURU_A003 risk.
Audit-fix files are policy and not reviewed as findings.

## Issues

### Issue 1 — Severity: bug
- File: tests/timewarp-nuru-tests/devcli/packable-projects-01-parse.cs:6
- Description: The only change to this file is a file-wide `#pragma warning disable IDE1006`
  so that 17 local constants named `stdout` stop tripping the naming rule when the file runs
  standalone. That silences IDE1006 (naming convention) instead of doing what it measures. Unlike
  the precedent in `auto/operation-camelcase-members.cs` (camelCase is the MCP parameter contract
  under test), there is no external contract here — `stdout` is just a local name. Policy:
  a suppression whose only effect is to make an analyzer pass is severity bug.
- Suggestion: Rename the local constants (e.g. `const string Stdout = …` or `MsBuildStdout`) and
  their uses, and remove the pragma and its comment.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/timewarp-nuru-devcli/content/any/services/packable-project-service.cs:93
- Description: Production progress goes to the static `TimeWarp.Terminal.Terminal.WriteErrorLine`,
  while every DevCli endpoint (check-version, release, clean) writes through an injected
  `ITerminal`. The progress line therefore bypasses the app's configured terminal: in
  `check-version-07-endpoint-fail-closed.cs`, which builds the handler with a `TestTerminal` and a
  real `new PackableProjectService()`, the line leaks to the real process stderr and is not
  observable by the test, and an app that calls `UseTerminal(...)` cannot redirect it.
- Suggestion: Give `PackableProjectService` an `ITerminal` (constructor injection, as the endpoint
  handlers already receive one) and pass `terminal.WriteErrorLine` into `EvaluateProjectsAsync`;
  update the `new PackableProjectService()` call sites in tests. Alternatively, have the callers
  (check-version / release handlers) report progress through their own `ITerminal`.
- Status: open

### Issue 3 — Severity: nit
- File: source/timewarp-nuru-devcli/content/any/services/packable-project-service.cs:181
- Description: When two or more projects fail, the combined `InvalidOperationException` keeps only
  the messages; the original exceptions (and their stack traces) are dropped.
- Suggestion: Pass `new AggregateException(failures)` as the inner exception so the detail is not lost.
- Status: open
