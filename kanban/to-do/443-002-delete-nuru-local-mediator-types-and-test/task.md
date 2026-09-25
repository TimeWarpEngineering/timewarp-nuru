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

## Checklist

- [ ] Generator emit
- [ ] Delete duplicates
- [ ] Tests

## Out of scope

- 444 ServiceGen / ServiceResolverEmitter
- ISender&lt;TScope&gt; implementation

## Session

- Created: 158299 (2026-09-01)
