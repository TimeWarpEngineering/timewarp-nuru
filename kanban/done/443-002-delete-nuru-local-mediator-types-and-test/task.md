# Delete Nuru-local mediator types and test

## Description

Parent: **443**. After **443-001**, Nuru’s generator emits `global::TimeWarp.Mediator.*`. Delete Nuru-local `IMessage` / `IQuery` / `ICommand` / handlers / `Unit` (whatever the package now owns). Tests pass.

## Depends on

- 443-001

## Requirements

- Generator type names point at TimeWarp.Mediator
- No duplicate abstractions in Nuru source
- Static DI and Microsoft DI both resolve `ISender` / `IPublisher`
- Existing Nuru tests pass
- **Idempotency contracts come from Mediator 14.0.0-beta.2:** delete Nuru's `IIdempotent`,
  `IIdempotentCommand<T>`, and `IIdempotentCommandHandler`; use `TimeWarp.Mediator`'s.
- **Classification precedence:** in Mediator, `IIdempotentCommand<T>` derives `ICommand<T>`. The endpoint
  extractor (`InferMessageType` / `InferReturnTypeFromInterfaces` in
  `source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs`) returns the first matching
  interface name from `AllInterfaces`, so an idempotent command could be classified as a plain Command.
  Make precedence explicit: Query, then IdempotentCommand, then Command, regardless of interface order.
  Prefer matching by fully qualified symbol (`TimeWarp.Mediator.*`) over short name.
- **Regression test:** a route implementing `IIdempotentCommand<T>` reports `IdempotentCommand` in help
  and in `--capabilities` output; `IQuery<T>` reports `Query`; `ICommand<T>` reports `Command`.
- This removes public types from `TimeWarp.Nuru`. Document the breaking change (namespace move to
  `TimeWarp.Mediator`) in the changelog and bump the Nuru version accordingly.

## Update 2026-09-25: pin TimeWarp.Mediator 14.0.0-beta.3

The first implement pass (commit 20c368fd) finished the Nuru side but the build failed on 14.0.0-beta.2
(CS0311 Unit-response handlers, TWM001 array-response handlers). Those generator bugs are fixed in
TimeWarp.Mediator **14.0.0-beta.3**, now on NuGet.org (mediator task 009, issue #68 closed).

- Bump `TimeWarp.Mediator.Contracts` and `TimeWarp.Mediator.Generators` in `Directory.Packages.props` to
  `14.0.0-beta.3` (clear `timewarp.mediator.*` from the local NuGet cache if a `-local` build is cached).
- Re-run the Release build and the full CI test gate; all requirements above must pass.
- Keep the existing 20c368fd work; do not redo it.

## Checklist

- [x] Generator emit
- [x] Delete duplicates
- [x] Tests

## Out of scope

- 444 ServiceGen / ServiceResolverEmitter
- ISender&lt;TScope&gt; implementation

## Results

Status: **done**. `TimeWarp.Mediator.Contracts` and `TimeWarp.Mediator.Generators` are pinned to
`14.0.0-beta.3` in `Directory.Packages.props`. The CS0311 (Unit-response) and TWM001 (array-response)
failures from beta.2 are gone, and no other Nuru change was needed. The stale `14.0.0-beta.3-local443`
packages were removed from the local NuGet cache. The changelog's dependency entry now says beta.3.

Work from the first pass (commit 20c368fd), kept as is:

- **Deleted** Nuru-local `IMessage`/`IQuery`/`ICommand`/`IIdempotentCommand`/handler interfaces/`Unit`/`IIdempotent`
  (`source/timewarp-nuru/abstractions/message-interfaces.cs`, `abstractions/handler-interfaces.cs`, `iidempotent.cs`).
- **Generator:** the endpoint kind is classified by matching fully qualified `TimeWarp.Mediator.*` interfaces,
  with precedence Query > IdempotentCommand > Command regardless of interface order (`endpoint-extractor.cs`).
  `Unit` detection uses `global::TimeWarp.Mediator.Unit`.
- **Static (source-gen) DI** resolves `ISender`/`IPublisher`/`IMediator` through a lazily built generated-mediator
  provider. Microsoft DI resolves them via `AddGeneratedMediator` (443-001).
- **Migration** of samples, tests, tools and docs to `using TimeWarp.Mediator;`, `Task<T> Handle` and `Unit.Task`/`Unit.Value`.
- **Tests:** `capabilities-07-mediator-message-kind.cs` (7 tests) covers the query, command and idempotent-command
  kinds and their precedence. `generator-46-generated-mediator.cs` gained 4 source-gen DI tests.
- **Changelog:** BREAKING entry with migration steps. Version bumped `3.0.0-beta.77` to `3.0.0-beta.78`.

Deviation: help output (`--help` and per-route help) does not show a message kind anywhere, so the kind is
asserted through `--capabilities` only.

Verification on the committed beta.3 pin:

- `dotnet build timewarp-nuru.slnx -c Release`: 0 warnings, 0 errors.
- `dotnet tests/ci-tests/run-ci-tests.cs`: exit 0; 1738 total, 1731 passed, 7 skipped, 0 failed.
- capabilities-07: 7/7. generator-46: 7/7.
- `./bin/dev verify-samples`: 64/64. `./bin/dev format`: passes. `ganda repo audit`: passes.

### How to validate

Smoke:

```bash
dotnet build timewarp-nuru.slnx -c Release
dotnet run tests/timewarp-nuru-tests/capabilities/capabilities-07-mediator-message-kind.cs
dotnet run tests/timewarp-nuru-tests/generator/generator-46-generated-mediator.cs
dotnet tests/ci-tests/run-ci-tests.cs
rg -n 'interface I(Message|Query|Command|IdempotentCommand|Idempotent)\b|struct Unit\b' source/timewarp-nuru || echo "no local mediator types"
rg -n 'TimeWarp.Mediator' Directory.Packages.props
ganda repo audit
```

Expect:

- Build: 0 warnings, 0 errors.
- capabilities-07: 7/7 pass (query, command and idempotent-command kinds; precedence does not depend on interface order).
- generator-46: 7/7 pass (ISender/IPublisher resolve under source-gen DI).
- The CI runner exits 0 with 0 failed.
- `rg` prints "no local mediator types".
- Both Mediator packages are shown at `14.0.0-beta.3`.
- Audit prints "Repository passes all audit checks."

### Implementation review

- **Disposition:** `accepted-exceptions`. 1 round, effort 1, roster: general.
- **Final counts:** bug 0; suggestion 1 wontfix; nit 2 wontfix; 0 open.
- **Exceptions:**
  - M1 (suggestion): the source-gen mediator bridge does not register `IOptions<T>` or typed `HttpClient`
    services. Replacing the source-gen service graph belongs to task 444, which is out of scope here.
    Runtime DI covers this case today.
  - M2 (nit): the dropped bridge provider is not disposed. Disposing it would also dispose instances the
    app owns (the terminal and singletons).
  - M3 (nit): the comment layout in `repl-session.cs` is set by the formatter.
- **Re-verified by review:** Release build 0 warnings / 0 errors. capabilities-07 7/7, generator-46 7/7,
  `run-ci-tests.cs` exit 0, and `ganda repo audit` passes.
- **Artifacts:** `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`,
  `review/disposition.md`.

## Session

- Created: 158299 (2026-09-01)
- Review: ganda task-work review oracle, general reviewer, effort 1 (2026-09-25)
