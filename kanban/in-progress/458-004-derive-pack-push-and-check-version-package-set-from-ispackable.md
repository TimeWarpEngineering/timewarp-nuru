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

- [ ] Pack: enumerate packable projects (or `dotnet pack` the solution) instead of the hardcoded array
- [ ] Push: glob `artifacts/packages/*.{version}.nupkg` instead of the hardcoded ID array
- [ ] check-version nuget-search: derive package IDs instead of `.timewarp/dev.jsonc` `packages`
- [ ] Verify the derived set equals today's five packages exactly (no test/tool projects leak in)
- [ ] Tests for the derivation (add a fake packable project → appears in set)
- [ ] Update `.timewarp/dev.jsonc` and DevCli readme
