# Update Configuration Samples to Use CreateBuilder

## Description

Convert all configuration samples from `new NuruAppBuilder()` to `NuruApp.CreateBuilder(args)`. Configuration samples demonstrate the configuration system which is auto-enabled by the factory method.

## Parent

MCP Builder Pattern Guidance Analysis - standardizing samples to prevent AI confusion

## Requirements

- Replace `new NuruAppBuilder()` with `NuruApp.CreateBuilder(args)` in all configuration samples
- Add header comments explaining the builder choice
- For samples using Mediator commands, implement canonical Mediator registration pattern
- Ensure all samples compile and run correctly

## Checklist

### Implementation
- [x] Update `samples/configuration/configuration-basics.cs` to use `NuruApp.CreateBuilder(args)`
- [x] Update `samples/configuration/configuration-validation.cs` to use `NuruApp.CreateBuilder(args)`
- [x] Update `samples/configuration/command-line-overrides.cs` to use `NuruApp.CreateBuilder(args)`
- [x] Update `samples/configuration/user-secrets-property.cs` to use `NuruApp.CreateBuilder(args)`
- [x] Update `samples/configuration/user-secrets-demo/program.cs` to use `NuruApp.CreateBuilder(args)`
- [x] Add Mediator package directives where Map<TCommand> is used
- [x] Add canonical ConfigureServices pattern for Mediator samples
- [x] Add explanatory comments about builder choice and configuration features
- [x] Verify all samples compile successfully
- [x] Verify all samples run correctly with expected configuration behavior

### Additional changes
- [x] Fixed incorrect project paths (`Source/TimeWarp.Nuru` → `source/timewarp-nuru`)
- [x] Updated `user-secrets-demo.csproj` with correct project reference and Mediator packages

## Notes

Configuration samples benefit from `NuruApp.CreateBuilder(args)` because it auto-enables:
- `AddConfiguration(args)` - Sets up configuration from command line, environment, appsettings.json, user secrets
- DI container for injecting IConfiguration
- Auto-help generation

Files to update:
- `samples/configuration/configuration-basics.cs`
- `samples/configuration/configuration-validation.cs`
- `samples/configuration/command-line-overrides.cs`
- `samples/configuration/user-secrets-property.cs`
- `samples/configuration/user-secrets-demo/program.cs`

Reference analysis: `.agent/workspace/2025-12-01T21-15-00_mcp-builder-pattern-guidance-analysis.md`
