# Migrate _completion samples to Nuru DSL API

## Description

Migrate `samples/_dynamic-completion-example/` and `samples/_shell-completion-example/` 
samples to use `NuruApp.CreateBuilder()` and Nuru interfaces.
Once complete, rename folders to `NN-completion/` or consolidate.

## Dependencies

Requires timewarp-nuru-completion library to be migrated away from Mediator (if applicable).

## Samples

- `_dynamic-completion-example/dynamic-completion-example.cs` - Dynamic tab completion
- `_shell-completion-example/shell-completion-example.cs` - Shell completion scripts

## Checklist

- [ ] Verify timewarp-nuru-completion library compatibility
- [ ] Update `dynamic-completion-example.cs` - use CreateBuilder
- [ ] Update `shell-completion-example.cs` - use CreateBuilder
- [ ] Verify tab completion works in shells (bash, zsh, fish, PowerShell)
- [ ] Rename folders to numbered convention (e.g., `15-completion/`)

## Notes

These samples demonstrate shell completion features. Need to verify the completion
library doesn't have Mediator dependencies.
