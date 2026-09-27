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

- [x] Pin 14.0.0-beta.4
- [x] CS0436 count before/after recorded, zero if achievable
- [x] Generator-strip target removed or reason recorded
- [x] Build, tests, samples pass

## Results

Status: **done**. `TimeWarp.Mediator.Contracts` and `TimeWarp.Mediator.Generators` are pinned to
`14.0.0-beta.4` in `Directory.Packages.props`. Generated host types are `internal`.

**CS0436** in `dotnet build tests/ci-tests/run-ci-tests.cs -c Release` (warnings in
`MediatorServiceCollectionExtensions.g.cs`, each naming `Mediator` imported from `TimeWarp.Nuru.Mcp`):

- Before (14.0.0-beta.3): **4**
- After (14.0.0-beta.4): **4**

Zero is not achievable. The multi-mode assembly (`run-ci-tests`) needs internals from both apps:

- MCP tests call `GetSyntaxTool`, `ValidateRouteTool`, `GetExampleTool`, and `GitHubCacheService`
  (`GetSafeCacheFileName`, `TryResolveRawContentUri`, `IsSafeCacheId`).
- Search tests call `SearchIndex.SanitizeFtsQuery`, `SearchIndex.EscapeLikePattern`, the internal
  data-source constructor, `SearchIndexJsonContext`, and `DatabasePath.EnsureIndexPath`.

MCP grants `InternalsVisibleTo("run-ci-tests")` from `internals-visible-to.g.cs`. Search grants it from
`global-usings.cs`. That friend access re-exposes the internal generated `Mediator`, so the four warnings
stay. Removing the attributes would make those tests fail to compile. Left in place.

**`RemoveTimeWarpMediatorGenerator` removed.** With internal host types, `TimeWarp.Nuru.dll` emitting
`AddGeneratedMediator()` does not give apps a second visible method. `TimeWarpMediatorAssembly=false`
stays: the generator package defaults it to `true`, and Nuru's detector uses it so the library does not
call `AddGeneratedMediator()`. The property does not stop emission (the generator always treats the
current compilation as a host); it is not redundant.

Runtime-DI samples each call `AddGeneratedMediator(services)` once (`basic.cs`, `advanced.cs`,
`fluent-runtime-di.cs`). The library's generated output has zero call sites.

Verification:

- `dotnet build timewarp-nuru.slnx -c Release`: 0 warnings, 0 errors. Packed nuspec depends on
  `TimeWarp.Mediator.Contracts` and `TimeWarp.Mediator.Generators` 14.0.0-beta.4.
- `./bin/dev verify-samples`: 64/64.
- `dotnet tests/ci-tests/run-ci-tests.cs`: exit 0. Multi-mode grand total 1747, 1740 passed, 7 skipped,
  0 failed. Standalone phase passed.
- `ganda repo audit`: "Repository passes all audit checks." (`bin/dev` was produced with `self-install`;
  it is gitignored).

### How to validate

Smoke:

```bash
dotnet build timewarp-nuru.slnx -c Release
dotnet build tests/ci-tests/run-ci-tests.cs -c Release
dotnet tests/ci-tests/run-ci-tests.cs
./bin/dev verify-samples
rg -n 'RemoveTimeWarpMediatorGenerator' source/timewarp-nuru/timewarp-nuru.csproj || echo "strip target gone"
rg -n 'TimeWarp.Mediator' Directory.Packages.props
unzip -p artifacts/packages/TimeWarp.Nuru.3.0.0-beta.78.nupkg TimeWarp.Nuru.nuspec | grep -i mediator
ganda repo audit
```

Expect:

- Solution build: 0 warnings, 0 errors.
- The CI test build prints 4 `CS0436` warnings, all for `Mediator` imported from `TimeWarp.Nuru.Mcp`.
- The CI runner exits 0. Multi-mode total 1747, passed 1740, skipped 7, failed 0.
- `verify-samples` prints `64/64 samples built successfully`.
- `rg` prints "strip target gone".
- Both Mediator packages are `14.0.0-beta.4`.
- The nuspec lists `TimeWarp.Mediator.Contracts` and `TimeWarp.Mediator.Generators` at `14.0.0-beta.4`.
- Audit prints "Repository passes all audit checks."

## Notes

- Implementer: **commit and push your changes before reporting done.**
- Run the build and test gate in the foreground.
