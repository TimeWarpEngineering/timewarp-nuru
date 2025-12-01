# Update REPL Demo Samples to Use CreateBuilder

## Description

Convert all REPL demo samples from `new NuruAppBuilder()` to `NuruApp.CreateBuilder(args)`. REPL functionality requires the full builder pattern with DI, Configuration, and extensions enabled.

## Parent

MCP Builder Pattern Guidance Analysis - standardizing samples to prevent AI confusion

## Requirements

- Replace `new NuruAppBuilder()` with `NuruApp.CreateBuilder(args)` in all REPL samples
- Add header comments explaining the builder choice
- For samples using Mediator commands, implement canonical Mediator registration pattern
- Ensure all samples compile and run correctly

## Checklist

### Implementation
- [ ] Update `samples/repl-demo/repl-basic-demo.cs` to use `NuruApp.CreateBuilder(args)`
- [ ] Update `samples/repl-demo/repl-interactive-mode.cs` to use `NuruApp.CreateBuilder(args)`
- [ ] Update `samples/repl-demo/repl-prompt-fix-demo.cs` to use `NuruApp.CreateBuilder(args)`
- [ ] Update `samples/repl-demo/repl-custom-keybindings.cs` to use `NuruApp.CreateBuilder(args)`
- [ ] Update `samples/repl-demo/repl-options-showcase.cs` to use `NuruApp.CreateBuilder(args)`
- [ ] Add Mediator package directives where Map<TCommand> is used
- [ ] Add canonical ConfigureServices pattern for Mediator samples
- [ ] Add explanatory comments about builder choice and REPL requirements
- [ ] Verify all samples compile successfully
- [ ] Verify all samples run correctly in REPL mode

## Notes

REPL samples require `NuruApp.CreateBuilder(args)` because REPL functionality is enabled through `UseAllExtensions()` which the factory method provides. The full builder includes:
- DI container setup
- Configuration
- Auto-help
- REPL support
- Dynamic completion
- Telemetry

Files to update:
- `samples/repl-demo/repl-basic-demo.cs`
- `samples/repl-demo/repl-interactive-mode.cs`
- `samples/repl-demo/repl-prompt-fix-demo.cs`
- `samples/repl-demo/repl-custom-keybindings.cs`
- `samples/repl-demo/repl-options-showcase.cs`

Reference analysis: `.agent/workspace/2025-12-01T21-15-00_mcp-builder-pattern-guidance-analysis.md`
