# Round 1 — general
**Date:** 2026-09-25
**Scope reviewed:** commit `a334b716`: `Directory.Packages.props`, `source/timewarp-nuru/timewarp-nuru.csproj`,
`source/timewarp-nuru-analyzers/generators/{nuru-generator.cs,models/generator-model.cs,emitters/interceptor-emitter.cs,extractors/generated-mediator-detector.cs}`,
`tests/timewarp-nuru-tests/generator/generator-46-generated-mediator.cs`, `changelog.md`.

## Summary

The change adds TimeWarp.Mediator 14.0.0-beta.2 and has Nuru's runtime-DI service provider call the
app's generated `AddGeneratedMediator()`. It is low risk: the emitted call depends on a detector that
mirrors the mediator generator's own emission rule. Generators flow to consuming apps, and the generator
is removed from TimeWarp.Nuru's own compilation. No issues found.

## Verification (falsifiable claims re-checked)

- **Detector matches upstream emission.** The `TimeWarp.Mediator.Generators` 14.0.0-beta.2 `buildTransitive`
  props default `TimeWarpMediatorAssembly=true` and `TimeWarpMediatorProfile=Host`, and mark both as
  `CompilerVisibleProperty`. The decompiled generator emits `MediatorServiceCollectionExtensions.g.cs`
  only when `!IsAotProfile` (`Aot` or `Link`, case-insensitive), with a default of `Host`.
  `GeneratedMediatorDetector` uses the same rule.
- **Duplicate registration is harmless.** `AddGeneratedMediator()` uses plain `Add*`, but generated
  `Send`/`Publish` resolve handlers by concrete type (`GetRequiredService<THandler>`). A user who also
  calls it in `ConfigureServices` gets last-wins registrations, not double notification delivery.
- **Ordering.** Nuru emits the call before the user's `ConfigureServices` body, so user overrides win.
- **Nuru is not a host.** `source/timewarp-nuru/generated/` has no `TimeWarp.Mediator.Generators` folder,
  and `TimeWarpMediatorAssembly=false` overrides the props default because the props load before the
  project body.
- **Blast radius.** In-repo consumers of `timewarp-nuru` are apps, tools, samples, tests, and benchmarks.
  No in-repo library re-exports public mediator types. The CS0436 warnings in ci-tests come from the
  mcp and search tools and are already documented as an upstream follow-up.
- **Plan alignment.** The runtime-DI path is registered. Static-DI `ISender` / `IPublisher` belongs to
  443-002 ("Static DI and Microsoft DI both resolve `ISender` / `IPublisher`"). Nuru-local types are untouched.
- **Build and test.** `dotnet build timewarp-nuru.slnx -c Release`: 0 warnings, 0 errors. generator-46: 3/3 passed.
  It is not in `CiTestExcludes`, so the CI multi-runner includes it. `ganda repo audit`: passes.

## Issues

<!-- None. -->
