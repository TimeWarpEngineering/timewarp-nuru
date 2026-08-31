# 443 - Consume TimeWarp.Mediator in Nuru

## Description

Nuru **consumes** the rewritten TimeWarp.Mediator. It does **not** own the rewrite, the source generator, or `ISender<TScope>`.

Those live in timewarp-mediator:

- **004** epic — source-gen rewrite + named pipelines
- **004-001** M1 — generated `Mediator` + analyzer (this task waits on M1 existing as a package)
- **004-002** M2 — `ISender<TScope>` (optional follow-on for Nuru; do not implement here)

This task used to be “extract Nuru abstractions and rewrite Mediator.” That work moved. This card is the Nuru adoption slice only.

## Requirements

- Do not start until timewarp-mediator **004-001** has shipped a usable package (or project reference equivalent)
- Add TimeWarp.Mediator as a package dependency
- Nuru generator emits `global::TimeWarp.Mediator.*` for message/handler/sender types
- Remove Nuru-local copies of extracted types (`IMessage`, `IQuery<T>`, `ICommand<T>`, `IIdempotentCommand<T>`, `Unit`, matching handler interfaces) once the Mediator package provides them
- Unscoped `ISender` / `IPublisher` work on both Nuru static DI and Microsoft DI paths
- Existing Nuru tests pass against abstractions sourced from TimeWarp.Mediator

## Checklist

- [ ] Confirm 004-001 is available (package or agreed project reference)
- [ ] Reference TimeWarp.Mediator; stop treating Mediator as something Nuru generates
- [ ] Point Nuru generator at TimeWarp.Mediator types
- [ ] Delete extracted duplicates from Nuru source
- [ ] Both DI paths resolve `ISender` / `IPublisher`
- [ ] Tests pass

## Out of scope

- Rewriting TimeWarp.Mediator (004 / 004-001)
- Implementing `ISender<TScope>` (004-002)
- TimeWarp.ServiceGen / replacing `ServiceResolverEmitter` (**444** — independent; not a blocker for this consume slice)
- TimeWarp.State switch (file in timewarp-state after 004-001 exists)

## Notes

- Filename still says “extract … with source-generated dispatch.” Treat the H1 as the title.
- Named pipelines (`ISender<ClientPipeline>` / `ISender<ServerPipeline>`) are Mediator M2. Nuru may use them later; it must not invent a second implementation.
- 444 remains the ServiceGen extraction. Mediator Host/State default is MS.DI; ServiceGen is the AOT/CLI profile.

## Session

- Created: (original 443)
- Retargeted: 2438044 (2026-08-31) — rewrite ownership moved to timewarp-mediator 004
