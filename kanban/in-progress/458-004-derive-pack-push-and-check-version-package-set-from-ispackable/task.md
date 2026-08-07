# Derive pack push and check-version package set from IsPackable

Parent: 458 (finding F5 in `458-*/review/findings.md`).

## Description

The five-package set is hand-maintained in three places: project paths in
`PackProjectsAsync`, package IDs in `PushPackagesAsync`
(`tools/dev-cli/endpoints/workflow-command.cs`), and the `packages` list in
`.timewarp/dev.jsonc`. Adding or renaming a packable project requires three edits;
missing one silently drops the package from pack, push, or the release gate.

Target (convention.md rule 8): zero hand-maintained lists. MSBuild already knows
the set — `IsPackable=true` projects (`timewarp-nuru-parsing` and
`timewarp-nuru-build` correctly opt out).

Suggested shape: pack the solution (packable projects only, respecting dependency
order via MSBuild), push every `artifacts/packages/*.{Version}.nupkg`, and have
check-version derive package IDs from packable csprojs or from a single generated
manifest. Remove the `packages` key from `.timewarp/dev.jsonc` once derived.

## Notes

### Implementation plan (Phase 2, 2026-08-08) — key decisions

- **D1:** authoritative derivation via `dotnet msbuild <csproj>
  -getProperty:IsPackable,PackageId` (no build/restore needed, ~0.4s/project,
  verified live returning exactly the 5 IDs). XML heuristics rejected: the
  flagship timewarp-nuru.csproj has NO explicit PackageId (derives from
  AssemblyName), and IsPackable is a two-level props default flip (root false
  → source true → csproj override). Pack = per-project loop over derived set
  (NOT slnx pack — dev build builds a curated subset); pack order irrelevant
  with --no-build; push = derived IDs with per-package exists-throw PLUS
  cross-check that no unexpected `*.{version}.nupkg` exists (stronger than
  glob-only); push order cosmetic (NuGet doesn't validate deps at push).
- **D2:** shared DevCli content `IPackableProjectService`/
  `PackableProjectService` (repoRoot passed in for fixture testability);
  call sites stay in workflow-command.cs.
- **D3:** `checkVersionConfig.packages` becomes an OPTIONAL OVERRIDE (kept in
  model; removed from this repo's dev.jsonc). Deleting the property outright
  would silently flip downstream repos to derivation on package bump —
  worse than the 458-003 silent-ignore precedent since the semantics here
  survive. Precedence: --package → config override → derived.
- **D4:** tests — pure `ParseGetPropertyOutput` matrix + temp-root fixture
  test reproducing the props-inherited-default and AssemblyName-derived-ID
  cases ("add fake packable project → appears in set"); live equality check:
  `dev check-version` must list exactly the current 5 IDs.
- AOT: add `MsBuildEvaluationOutput` to DevCliJsonContext.
- check-version-04 endpoint test ctor gains the new service param.

## Checklist

- [x] Pack: enumerate packable projects (or `dotnet pack` the solution) instead of the hardcoded array
- [x] Push: glob `artifacts/packages/*.{version}.nupkg` instead of the hardcoded ID array
- [x] check-version nuget-search: derive package IDs instead of `.timewarp/dev.jsonc` `packages`
- [x] Verify the derived set equals today's five packages exactly (no test/tool projects leak in)
- [x] Tests for the derivation (add a fake packable project → appears in set)
- [x] Update `.timewarp/dev.jsonc` and DevCli readme

## Session (2026-08-07)

Implemented per the Phase 2 plan (D1-D4 above). New shared DevCli content:
`ipackable-project-service.cs` (`IPackableProjectService`/`PackableProject` record) and
`packable-project-service.cs` (`PackableProjectService`, `MsBuildEvaluationOutput`,
pure `ParseGetPropertyOutput`). `check-version-command.cs` now resolves the package set
via `--package` → `checkVersionConfig.packages` → derived (only when both are unset;
an explicit override that parses to zero packages does NOT fall through to derivation).
`workflow-command.cs` derives the packable set once before Step 5/6 (pack/push), aborts
the pipeline if empty, and `PushPackagesAsync` cross-checks that no `*.{version}.nupkg`
in the artifacts dir falls outside the derived set. `.timewarp/dev.jsonc`'s `packages`
key removed (kept as an optional override, now empty/commented). Added
`tests/timewarp-nuru-tests/devcli/packable-projects-01-parse.cs` (13 tests, pure parse
matrix) and `packable-projects-02-derivation-fixture.cs` (2 tests, temp-root fixture
against real `dotnet msbuild`; no iteration needed — fixture csproj evaluated cleanly
on the first attempt). Updated `check-version-04-endpoint-zero-package.cs` for the new
4-arg `Handler` ctor and changed error text.

Verification: `dev check-version` lists exactly TimeWarp.Nuru, TimeWarp.Nuru.Analyzers,
TimeWarp.Nuru.DevCli, TimeWarp.Nuru.Mcp, TimeWarp.Nuru.Search (sorted), aborts exit 1
(beta.71 already published — expected). `--package TimeWarp.Nuru` override confirmed
single-package. `dotnet build timewarp-nuru.slnx`: 0 warnings/errors. Full CI suite
(`tests/ci-tests/run-ci-tests.cs`): exit 0, multi-mode assembly 1461 total / 1454 passed
/ 7 skipped / 0 failed, all standalone generator runs green. Changes left uncommitted
in the working tree per the delegating agent's instruction.

## Session (2026-08-07, round-1 review fixes)

Commit `789cadd1` reviewed (`review/round-1/merged.md`); applied the fix-batch (findings
1–5), left 6–7 as wontfix per the review's disposition:

- **1a:** `ParseGetPropertyOutput` no longer anchors on the first `{` in stdout (a log
  line with a stray brace before the real payload, e.g. `warning XY{123}: ...`, could
  mis-anchor). Now finds `"Properties"` and walks backward to the nearest preceding `{`
  (whitespace-only gap allowed) — handles both msbuild's real pretty-printed shape and a
  hypothetical compact shape. Verified live shape first (`dotnet msbuild ... -getProperty`
  → pretty-printed with `{\n  "Properties"`). New tests: noise-with-its-own-brace, compact
  shape; updated the two tests whose semantics shifted with the anchor change.
- **1b:** `GetPackableProjectsAsync` now throws `InvalidOperationException` naming the
  project when `IsPackable=true` evaluates to a null/blank `PackageId`, instead of
  silently dropping it. Fixture note: Microsoft.NET.Sdk's own `NuGet.Build.Tasks.Pack
  .targets` unconditionally backfills a blank `PackageId` from `AssemblyName`
  (`Condition="'$(PackageId)' == ''"`), so no ordinary SDK-style project — with or
  without an explicit `<PackageId></PackageId>` element, with or without a
  `Directory.Build.targets` override — can produce a genuinely blank evaluated
  `PackageId` to exercise this guard. Fixture uses a bare `<Project>` (no `Sdk=`) that
  manually imports `Directory.Build.props`, which is the only way to reproduce
  `IsPackable=true` + blank `PackageId` via real MSBuild evaluation; documented inline.
- **1c:** (i) extracted `PackableProjectService.ValidateDerivedSet` (pure static) —
  throws on a duplicate `PackageId` across two projects, naming both paths; also owns
  the final PackageId-ordinal sort. Unit-tested directly (pass-through, duplicate-throw,
  empty). (ii) `workflow-command.cs` now prints `"Packable set (N): ..."` after deriving.
- **2:** `check-version-command.cs` prints a one-line nudge — "Using configured package
  override; delete checkVersionConfig.packages to derive the set from IsPackable." — only
  when the package set came from `checkVersionConfig.packages` (not `--package`). Verified
  manually: no nudge with today's override-free `dev.jsonc`; temporarily set
  `checkVersionConfig.packages` to confirm the nudge fires; reverted.
- **3:** Derivation + empty-set abort moved from Step 5 to immediately after Step 2
  (Check Version), before Clean/Build — an empty derived set no longer wastes a
  clean+build cycle before failing. Same derived list threaded to Pack and Push.
- **4:** Readme migration note extended to name `PackableProject` and (explicitly
  flagged, generic-name) `MsBuildEvaluationOutput` alongside `IPackableProjectService`/
  `PackableProjectService`, mirroring the `TagAssertion` collision-callout precedent.
- **6, 7 (wontfix, per review disposition):** case-sensitive obj/bin exclusion; harmless
  double derivation per release (~2s, nothing mutates in between).

Verification re-run: parse tests 13→19 (added noise-with-brace, compact-shape,
`ValidateDerivedSet` pass-through/duplicate/empty), all pass; fixture tests 2→3 (added
blank-PackageId-throws), all pass; `dev check-version` still lists exactly the 5 sorted
IDs with no nudge line; manual override round-trip confirmed the nudge; `dotnet build
timewarp-nuru.slnx` clean (0/0); full CI suite exit 0, multi-mode 1468 total / 1461
passed / 7 skipped / 0 failed (net +7 tests vs. pre-fix run, all green). No trailing
whitespace introduced (pre-existing readme trailing whitespace in an unrelated XML
example block, unchanged). Still uncommitted per instruction.

No deviations from the requested fix batch. One judgment call: fixture 1b's project
shape (bare `<Project>`, no `Sdk=`) diverges from the review's literal suggestion
(`<PackageId></PackageId>` on an ordinary csproj) because the literal suggestion does
not reproduce under real MSBuild evaluation — verified empirically (three attempts:
plain empty element, `Directory.Build.targets` override, both backfilled by the SDK's
own pack-defaulting targets) before landing on the non-SDK project as the only real
reproduction.
