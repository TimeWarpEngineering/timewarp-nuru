# 443 - Consume TimeWarp.Mediator 14-beta in Nuru

## Description

Nuru **consumes** TimeWarp.Mediator **14.0.0-beta**. It does **not** rewrite Mediator, implement `ISender<TScope>`, or extract ServiceGen.

Wait for mediator **005-003** (NuGet prerelease). Filename is historical; this H1 is the title.

**444** (ServiceGen) stays independent and is **not** a blocker.

## Children

- **443-001** Package + `AddGeneratedMediator`
- **443-002** Delete Nuru-local message/handler types; tests

## Requirements

- TimeWarp.Mediator 14.0.0-beta.1 packages
- Generator emits `global::TimeWarp.Mediator.*`
- Remove Nuru copies of `IMessage` / `IQuery` / `ICommand` / handlers / `Unit` once the package provides them
- Both Nuru DI paths resolve `ISender` / `IPublisher`
- Existing tests pass

## Out of scope

- Implementing `ISender<TScope>` (mediator 004-002 already did)
- TimeWarp.ServiceGen / `ServiceResolverEmitter` (**444**)
- TimeWarp.State (080)
- Stable 14.0.0

## Notes

- Cross-repo wait: timewarp-mediator **005-003**.
- Optional later: Nuru named pipelines after 443-002. Not this epic.

## Session

- Created: (original 443)
- Retargeted: 2026-08-31 consume (not rewrite)
- Epic children: 2026-09-01 — wait for 14.0.0-beta
