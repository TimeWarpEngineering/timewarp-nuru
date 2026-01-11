# Migrate _repl-demo samples to Nuru DSL API

## Description

Migrate `samples/_repl-demo/` samples to use `NuruApp.CreateBuilder()` and Nuru interfaces.
Once complete, rename folder to `NN-repl/`.

## Dependencies

Requires timewarp-nuru-repl library to be migrated away from Mediator.

## Samples

- `_repl-demo/repl-basic-demo.cs`
- `_repl-demo/repl-custom-keybindings.cs`
- `_repl-demo/repl-interactive-mode.cs`
- `_repl-demo/repl-options-showcase.cs`
- `_repl-demo/repl-prompt-fix-demo.cs`

## Checklist

- [ ] Verify timewarp-nuru-repl library is migrated to Nuru interfaces
- [ ] Update `repl-basic-demo.cs` - remove Mediator, use CreateBuilder
- [ ] Update `repl-custom-keybindings.cs`
- [ ] Update `repl-interactive-mode.cs`
- [ ] Update `repl-options-showcase.cs`
- [ ] Update `repl-prompt-fix-demo.cs`
- [ ] Verify samples run correctly
- [ ] Rename folder to numbered convention (e.g., `13-repl/`)

## Notes

All samples currently reference:
- `#:package Mediator.Abstractions`
- `#:package Mediator.SourceGenerator`
- `.ConfigureServices(services => services.AddMediator())`

These need to be removed and replaced with Nuru patterns.
