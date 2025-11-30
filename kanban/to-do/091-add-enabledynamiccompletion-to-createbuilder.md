# Add EnableDynamicCompletion to CreateBuilder

## Description

`NuruApp.CreateBuilder()` should automatically enable dynamic shell completion by calling `EnableDynamicCompletion()` in `UseAllExtensions()`.

Shell completion (Tab-completion in bash, zsh, pwsh, fish) is distinct from REPL tab-completion:
- **Shell completion**: External process - shell invokes the CLI app to get completions before command execution
- **REPL completion**: In-process - the running REPL handles Tab keypresses internally

The comment in `UseAllExtensions()` incorrectly claims "Completion is already included via the REPL package dependency" - the package is referenced but the shell completion routes are never registered.

## Requirements

- `UseAllExtensions()` must call `EnableDynamicCompletion()` to register shell completion routes
- Add `ConfigureCompletion` option to `NuruAppOptions` for customization (similar to `ConfigureRepl` and `ConfigureTelemetry`)
- Update the misleading comment in `UseAllExtensions()`
- Ensure routes `--generate-completion {shell}`, `__complete`, and `--install-completion` are registered by default

## Checklist

### Implementation
- [ ] Add `Action<CompletionSourceRegistry>? ConfigureCompletion` property to `NuruAppOptions`
- [ ] Update `UseAllExtensions()` to call `EnableDynamicCompletion()` with the configure action
- [ ] Fix the misleading comment about completion being included via REPL

### Documentation
- [ ] Update `NuruAppOptions` XML docs to include completion configuration example

## Notes

Files to modify:
- `source/timewarp-nuru/nuru-app-options.cs` - Add `ConfigureCompletion` property
- `source/timewarp-nuru/nuru-app-builder-extensions.cs` - Add `EnableDynamicCompletion()` call in `UseAllExtensions()`

Dynamic completion is preferred over static because it supports runtime-computed completions (e.g., file paths, database values) and the `--install-completion` convenience route.
