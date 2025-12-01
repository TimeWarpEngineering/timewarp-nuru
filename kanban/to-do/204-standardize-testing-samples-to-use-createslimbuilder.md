# Standardize Testing Samples to Use CreateSlimBuilder

## Description

Convert all testing samples from `new NuruAppBuilder()` to `NuruCoreApp.CreateSlimBuilder(args)`. Testing samples demonstrate output capture and terminal injection without Mediator, so they should use the slim builder pattern.

## Parent

MCP Builder Pattern Guidance Analysis - standardizing samples to prevent AI confusion

## Requirements

- Replace `new NuruAppBuilder()` with `NuruCoreApp.CreateSlimBuilder(args)` in all testing samples
- Add header comments explaining the builder choice
- Ensure all samples compile and run correctly

## Checklist

### Implementation
- [ ] Update `samples/testing/test-colored-output.cs` to use `NuruCoreApp.CreateSlimBuilder(args)`
- [ ] Update `samples/testing/test-terminal-injection.cs` to use `NuruCoreApp.CreateSlimBuilder(args)`
- [ ] Update `samples/testing/test-output-capture.cs` to use `NuruCoreApp.CreateSlimBuilder(args)`
- [ ] Add explanatory comments about builder choice
- [ ] Verify all samples compile successfully
- [ ] Verify all samples run correctly with expected output

## Notes

Testing samples demonstrate output capture patterns for unit testing CLI applications. They use delegate-based routing only, so `NuruCoreApp.CreateSlimBuilder(args)` is appropriate.

Reference analysis: `.agent/workspace/2025-12-01T21-15-00_mcp-builder-pattern-guidance-analysis.md`

Files to update:
- `samples/testing/test-colored-output.cs`
- `samples/testing/test-terminal-injection.cs`
- `samples/testing/test-output-capture.cs`
