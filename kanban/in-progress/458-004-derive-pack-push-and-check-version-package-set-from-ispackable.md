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

## Checklist

- [ ] Pack: enumerate packable projects (or `dotnet pack` the solution) instead of the hardcoded array
- [ ] Push: glob `artifacts/packages/*.{version}.nupkg` instead of the hardcoded ID array
- [ ] check-version nuget-search: derive package IDs instead of `.timewarp/dev.jsonc` `packages`
- [ ] Verify the derived set equals today's five packages exactly (no test/tool projects leak in)
- [ ] Tests for the derivation (add a fake packable project → appears in set)
- [ ] Update `.timewarp/dev.jsonc` and DevCli readme
