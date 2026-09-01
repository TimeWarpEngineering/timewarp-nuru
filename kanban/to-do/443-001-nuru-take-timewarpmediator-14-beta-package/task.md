# Nuru take TimeWarp.Mediator 14-beta package

## Description

Parent: **443**. Add **TimeWarp.Mediator 14.0.0-beta.1** and register the generated mediator. Wait for mediator **005-003**.

## Requirements

- Package refs for 14.0.0-beta.1 (Contracts + Generators as required)
- Host/CLI uses `AddGeneratedMediator()` (not martinothamar, not legacy `AddMediator()` unless a documented leftover)
- `[assembly: MediatorAssembly]` (or Nuru’s equivalent membership) so handlers link
- Do not delete Nuru-local types yet (**443-002**)

## Checklist

- [ ] 14.0.0-beta.1 on nuget.org (005-003)
- [ ] Package refs
- [ ] Generated registration
- [ ] Build

## Out of scope

- Deleting ICommand/IQuery copies (443-002)
- ServiceGen (444)
- Named pipelines

## Session

- Created: 158299 (2026-09-01)
