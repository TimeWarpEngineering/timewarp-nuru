# Bump TimeWarp.Mediator to 14.0.0-beta.4 (internal host types)

## Description

TimeWarp.Mediator 14.0.0-beta.4 (mediator task 010, on NuGet.org 2026-09-27) emits the host-profile
generated types (`Mediator`, `MediatorManifest`, `GeneratedMediatorServiceCollectionExtensions`) as
`internal` instead of `public`. Nuru is on 14.0.0-beta.3.

Two things this may let Nuru clean up (443-001 follow-ups):

1. **CS0436 warnings:** `tests/ci-tests/run-ci-tests.cs` references the mcp and search apps and got 4
   `CS0436` duplicate-type warnings in `MediatorServiceCollectionExtensions.g.cs`. Caveat from mediator 010's
   review: internal types are re-exposed when both hosts grant `InternalsVisibleTo` to the referencing
   project, and Nuru's generated `internals-visible-to.g.cs` files do list test assemblies.
2. **`RemoveTimeWarpMediatorGenerator` target** in `source/timewarp-nuru/timewarp-nuru.csproj` strips the
   generator from Nuru's own compile because a public `AddGeneratedMediator()` in TimeWarp.Nuru.dll was
   ambiguous in apps. With internal emission it may no longer be needed.

## Requirements

- Bump `TimeWarp.Mediator.Contracts` and `TimeWarp.Mediator.Generators` to `14.0.0-beta.4` in
  `Directory.Packages.props`. Release build and full CI test gate pass.
- Count the CS0436 warnings in the CI test build before and after; record both in Results.
- If warnings remain because of `InternalsVisibleTo`: stop the mcp and search apps from granting
  InternalsVisibleTo to `run-ci-tests` (or the multi-mode CI assembly) when those tests do not need their
  internals, so the warnings go to zero. If they do need internals, record why and leave it.
- Try removing `RemoveTimeWarpMediatorGenerator` (and `TimeWarpMediatorAssembly=false` only if it becomes
  redundant). Keep the removal only if the build, tests, and samples pass and apps still call exactly one
  `AddGeneratedMediator()`. Otherwise keep it and record why.
- Changelog entry under Unreleased.

## Checklist

- [ ] Pin 14.0.0-beta.4
- [ ] CS0436 count before/after recorded, zero if achievable
- [ ] Generator-strip target removed or reason recorded
- [ ] Build, tests, samples pass

## Notes

- Implementer: **commit and push your changes before reporting done.**
- Run the build and test gate in the foreground.
