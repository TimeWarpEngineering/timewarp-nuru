# Update timewarp-nuru-sample to Use CreateBuilder

## Description

Convert `timewarp-nuru-sample/program.cs` from `new NuruAppBuilder()` to `NuruApp.CreateBuilder(args)`. This is a general sample that should follow the recommended factory method pattern.

## Parent

MCP Builder Pattern Guidance Analysis - standardizing samples to prevent AI confusion

## Requirements

- Replace `new NuruAppBuilder()` with `NuruApp.CreateBuilder(args)`
- Add header comments explaining the builder choice
- For samples using Mediator commands, implement canonical Mediator registration pattern
- Ensure sample compiles and runs correctly

## Checklist

### Implementation
- [x] Update `samples/timewarp-nuru-sample/program.cs` to use `NuruApp.CreateBuilder(args)`
- [x] Add Mediator package references to .csproj if Map<TCommand> is used (already present)
- [x] Add canonical ConfigureServices pattern for Mediator commands
- [x] Add explanatory comments about builder choice
- [x] Verify sample compiles successfully
- [x] Verify sample runs correctly with expected output

### Additional changes
- [x] Converted from multi-statement builder pattern to fluent chain pattern

## Notes

This sample serves as a general reference for Nuru CLI applications and should demonstrate the recommended patterns.

File to update:
- `samples/timewarp-nuru-sample/program.cs`

Reference analysis: `.agent/workspace/2025-12-01T21-15-00_mcp-builder-pattern-guidance-analysis.md`
