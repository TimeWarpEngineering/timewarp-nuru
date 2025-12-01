# Standardize hello-world and calc-delegate Samples to Use CreateSlimBuilder

## Description

Convert `hello-world/hello-world.cs` and `calculator/calc-delegate.cs` from `new NuruAppBuilder()` to `NuruCoreApp.CreateSlimBuilder(args)`. These samples demonstrate delegate-based routing without Mediator, so they should use the slim builder pattern.

## Parent

MCP Builder Pattern Guidance Analysis - standardizing samples to prevent AI confusion

## Requirements

- Replace `new NuruAppBuilder()` with `NuruCoreApp.CreateSlimBuilder(args)`
- Add header comments explaining the builder choice
- Ensure samples compile and run correctly
- Verify no Mediator packages are needed for delegate-only patterns

## Checklist

### Implementation
- [ ] Update `samples/hello-world/hello-world.cs` to use `NuruCoreApp.CreateSlimBuilder(args)`
- [ ] Update `samples/calculator/calc-delegate.cs` to use `NuruCoreApp.CreateSlimBuilder(args)`
- [ ] Add explanatory comments about builder choice (delegate = slim, mediator = full)
- [ ] Verify both samples compile successfully
- [ ] Verify both samples run correctly with expected output

## Notes

These samples are delegate-only (no `Map<TCommand>` with Mediator), so they should use `NuruCoreApp.CreateSlimBuilder(args)` which is lighter weight and doesn't include DI, Configuration, or auto-help by default.

Reference analysis: `.agent/workspace/2025-12-01T21-15-00_mcp-builder-pattern-guidance-analysis.md`

Current builder pattern in these files:
```csharp
// WRONG - uses direct constructor
var builder = new NuruAppBuilder();
```

Should become:
```csharp
// CORRECT - uses factory method for delegate-based routing
var builder = NuruCoreApp.CreateSlimBuilder(args);
```
