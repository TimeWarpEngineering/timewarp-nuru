# Round 1 — general
**Date:** 2026-09-27
**Scope reviewed:** branch `task/475-bump-timewarpmediator-to-1400-beta4-internal-host` vs `origin/master` — `Directory.Packages.props`, `source/timewarp-nuru/timewarp-nuru.csproj`, `changelog.md`

## Summary

The pin is `14.0.0-beta.4` for both Contracts and Generators. The library compile now runs the mediator generator: emitted `Mediator`, `MediatorManifest`, and `GeneratedMediatorServiceCollectionExtensions` are `internal`. `TimeWarpMediatorAssembly` stays `false`, and the library's own generated app code has no `AddGeneratedMediator()` call. Runtime-DI samples `basic.cs`, `advanced.cs`, and `fluent-runtime-di.cs` each emit one call. `dotnet build timewarp-nuru.slnx -c Release` is 0 warnings, 0 errors. `dotnet build tests/ci-tests/run-ci-tests.cs -c Release` reports exactly 4 `CS0436` warnings, all for `Mediator` imported from `TimeWarp.Nuru.Mcp`. MCP and search tests still use those assemblies' internals, so the friend attributes stay. No bugs, suggestions, or nits.

## Issues

<!-- none -->
