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

## Checklist

- [ ] Generator emit
- [ ] Delete duplicates
- [ ] Tests

## Out of scope

- 444 ServiceGen / ServiceResolverEmitter
- ISender&lt;TScope&gt; implementation

## Session

- Created: 158299 (2026-09-01)
