# Add Builder Choice Comments to MCP Samples

## Description

Add clarifying header comments to all samples served by MCP to help developers understand the patterns and avoid common pitfalls.

## Requirements

- Comments should explain WHY the builder was chosen
- Comments should list REQUIRED PACKAGES for each pattern
- Comments should include COMMON ERROR solutions
- Reference the template in `.agent/workspace/2025-12-01T20-45-00_sample-standardization-and-analyzer-plan.md`

## Checklist

### Implementation
- [ ] Create standard comment template for CreateBuilder samples
- [ ] Create standard comment template for CreateSlimBuilder samples
- [ ] Add header comments to mediator pattern samples
- [ ] Add header comments to delegate pattern samples
- [ ] Add header comments to configuration samples
- [ ] Add header comments to REPL samples
- [ ] Add header comments to pipeline samples
- [ ] Add header comments to testing samples
- [ ] Verify comments render properly in MCP tool responses

### Documentation
- [ ] Document the comment format standards

## Notes

Tags: samples, mcp, documentation

Example comment structure:
```csharp
// ============================================================================
// BUILDER: NuruApp.CreateBuilder (full-featured)
// WHY: This sample uses the Mediator pattern which requires DI configuration
// REQUIRED PACKAGES: TimeWarp.Nuru, Mediator, Mediator.Abstractions
// COMMON ERROR: "Cannot resolve Mediator" - ensure services.AddMediator() called
// ============================================================================
```
