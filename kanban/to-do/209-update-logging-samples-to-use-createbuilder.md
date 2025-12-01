# Update Logging Samples to Use CreateBuilder

## Description

Convert logging samples from `new NuruAppBuilder()` to `NuruApp.CreateBuilder(args)`. Logging samples demonstrate integration with Microsoft.Extensions.Logging and Serilog which require DI.

## Parent

MCP Builder Pattern Guidance Analysis - standardizing samples to prevent AI confusion

## Requirements

- Replace `new NuruAppBuilder()` with `NuruApp.CreateBuilder(args)` in all logging samples
- Add header comments explaining the builder choice
- For samples using Mediator commands, implement canonical Mediator registration pattern
- Ensure all samples compile and run correctly

## Checklist

### Implementation
- [ ] Update `samples/logging/console-logging.cs` to use `NuruApp.CreateBuilder(args)`
- [ ] Update `samples/logging/serilog-logging.cs` to use `NuruApp.CreateBuilder(args)`
- [ ] Add Mediator package directives where Map<TCommand> is used
- [ ] Add canonical ConfigureServices pattern for Mediator samples
- [ ] Add explanatory comments about builder choice and logging integration
- [ ] Verify both samples compile successfully
- [ ] Verify both samples run correctly with expected logging output

## Notes

Logging samples require `NuruApp.CreateBuilder(args)` because:
- DI is required for ILogger injection
- Configuration is needed for logging configuration
- Serilog integration requires DI registration

Files to update:
- `samples/logging/console-logging.cs`
- `samples/logging/serilog-logging.cs`

Reference analysis: `.agent/workspace/2025-12-01T21-15-00_mcp-builder-pattern-guidance-analysis.md`
