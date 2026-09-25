# Nuru take TimeWarp.Mediator 14-beta package

## Description

Parent: **443**. Add **TimeWarp.Mediator 14.0.0-beta.2** and register the generated mediator. Wait for mediator **008** (adds the idempotency contracts; beta.1 lacks them).

## Requirements

- Package refs for 14.0.0-beta.2 (Contracts + Generators as required)
- Host/CLI uses `AddGeneratedMediator()` (not martinothamar, not legacy `AddMediator()` unless a documented leftover)
- `[assembly: MediatorAssembly]` (or Nuru’s equivalent membership) so handlers link
- Do not delete Nuru-local types yet (**443-002**)

## Checklist

- [x] 14.0.0-beta.2 on nuget.org (mediator 008)
- [x] Package refs
- [x] Generated registration
- [x] Build

## Out of scope

- Deleting ICommand/IQuery copies (443-002)
- ServiceGen (444)
- Named pipelines

## Session

- Created: 158299 (2026-09-01)

## Results

TimeWarp.Mediator 14.0.0-beta.2 (`Contracts`, `Generators`) is on nuget.org and now referenced by
TimeWarp.Nuru. Every Nuru app compilation is the generated-mediator host; apps using
`.UseMicrosoftDependencyInjection()` call `AddGeneratedMediator()` from Nuru's generated service
provider setup, so handlers can inject `ISender` / `IPublisher` / `IMediator`.

### What changed

- `Directory.Packages.props`: `TimeWarp.Mediator.Contracts` and `TimeWarp.Mediator.Generators` at
  `14.0.0-beta.2`.
- `source/timewarp-nuru/timewarp-nuru.csproj`:
  - References Contracts, and Generators with `PrivateAssets="none"` so the generator (analyzers +
    `buildTransitive` props) flows to consuming apps. The packed nuspec lists Generators with
    `include="All"`.
  - Target `RemoveTimeWarpMediatorGenerator` strips the generator from TimeWarp.Nuru's own compile.
    The generator emits a **public** `Microsoft.Extensions.DependencyInjection.GeneratedMediatorServiceCollectionExtensions.AddGeneratedMediator()`
    into every compilation it runs in; a copy in TimeWarp.Nuru.dll would make the call ambiguous in apps.
  - `TimeWarpMediatorAssembly=false`: Nuru ships no mediator handlers, so it is not a graph member.
- Membership: no `[assembly: MediatorAssembly]` is needed in apps. The Generators `buildTransitive`
  props set `TimeWarpMediatorAssembly=true` / `TimeWarpMediatorProfile=Host` in every consuming app,
  so the app's own handlers link. Domain libraries with handlers still opt in with `[assembly: MediatorAssembly]`.
- Nuru generator (`timewarp-nuru-analyzers`):
  - New `GeneratedMediatorDetector`: true when `build_property.TimeWarpMediatorAssembly` is true, the
    profile is not `Aot`/`Link` (those profiles emit no registration extension), and
    `TimeWarp.Mediator.ISender` resolves.
  - `GeneratorModel.HasGeneratedMediator` carries the flag; `InterceptorEmitter` emits
    `global::Microsoft.Extensions.DependencyInjection.GeneratedMediatorServiceCollectionExtensions.AddGeneratedMediator(services);`
    in `GetServiceProvider` before the user's `ConfigureServices` body, so user registrations can override.
- Test: `tests/timewarp-nuru-tests/generator/generator-46-generated-mediator.cs`. It covers a runtime-DI
  app resolving `ISender` (sends a `TimeWarp.Mediator.IQuery<string>`), `IPublisher` (publishes an
  `INotification` to a handler), and `IMediator` (is `TimeWarp.Mediator.Generated.Mediator`).
- `changelog.md`: Unreleased/Added entry.
- Nuru-local `IMessage` / `IQuery` / `ICommand` / `IIdempotent*` / handlers / `Unit` are untouched (443-002).
  No legacy `AddMediator()` or martinothamar Mediator anywhere.

### Verification

- `dotnet build timewarp-nuru.slnx -c Release`: 0 warnings, 0 errors.
- `tests/ci-tests/run-ci-tests.cs`: 1727 total, 1720 passed, 7 skipped, 0 failed. All standalone tests pass.
- `dotnet tools/dev-cli/dev.cs self-install`: AOT publish of the dev CLI (which now carries a generated
  mediator) succeeds; `./bin/dev --capabilities` runs.
- `ganda repo audit`: "Repository passes all audit checks." (it needed `bin/dev` built locally; that
  file is gitignored).

### Follow-ups / notes

- **Static (source-gen) DI path** does not register the mediator yet. A handler injecting `ISender`
  without `.UseMicrosoftDependencyInjection()` still hits Nuru's unregistered-service diagnostics. The
  epic assigns "both DI paths resolve `ISender` / `IPublisher`" to **443-002**.
- **Upstream (timewarp-mediator):** Host-profile output (`Mediator`, `MediatorManifest`,
  `GeneratedMediatorServiceCollectionExtensions`) is `public`. A compilation that references two apps
  sees duplicate types. `tests/ci-tests/run-ci-tests.cs` references the mcp and search apps and gets 4
  `CS0436` warnings in the mediator's generated `MediatorServiceCollectionExtensions.g.cs` (warning only;
  tests are not warnings-as-errors). Suggest the generator emit `internal` for hosts.
- Pre-existing, unrelated: Nuru's delegate emitter produces `await await ...` for expression-bodied
  async lambdas whose body starts with `await` (for example `async (ISender s) => await s.Send(q)`). The new test
  uses block bodies.
- Pre-existing, unrelated: `bin/dev format` reports whitespace/import violations in files this task
  did not touch (`repl-session.cs`, `endpoint-extractor.cs`, `referenced-method-decompiler.cs`,
  `handler-parameter-mismatch-exception.cs`).

### How to validate

Smoke:

```bash
dotnet build timewarp-nuru.slnx -c Release
dotnet tests/timewarp-nuru-tests/generator/generator-46-generated-mediator.cs
rg -n "AddGeneratedMediator" artifacts/generated/generator-46-generated-mediator.cs/TimeWarp.Nuru.Analyzers/TimeWarp.Nuru.Generators.NuruGenerator/NuruGenerated.g.cs
ls source/timewarp-nuru/generated/
unzip -p artifacts/packages/TimeWarp.Nuru.3.0.0-beta.77.nupkg TimeWarp.Nuru.nuspec | grep -i mediator
```

Expect:

- Build succeeds with 0 warnings and 0 errors.
- generator-46 reports `Total: 3`, `Passed: 3` (`Should_resolve_generated_sender_under_runtime_di`,
  `Should_resolve_generated_publisher_under_runtime_di`, `Should_resolve_generated_mediator_type`).
- `NuruGenerated.g.cs` contains one `GeneratedMediatorServiceCollectionExtensions.AddGeneratedMediator(services);`
  line per runtime-DI app.
- `source/timewarp-nuru/generated/` has no `TimeWarp.Mediator.Generators` folder (Nuru itself does not
  run the mediator generator).
- The nuspec lists `TimeWarp.Mediator.Contracts` 14.0.0-beta.2 and `TimeWarp.Mediator.Generators`
  14.0.0-beta.2 with `include="All"`.
