# Update Pipeline and Unified Middleware Samples with Canonical Pattern

## Description

Convert `pipeline-middleware.cs` and `unified-middleware.cs` to use `NuruApp.CreateBuilder(args)` with the canonical Mediator registration pattern. These samples demonstrate pipeline behaviors and require proper Mediator configuration.

## Parent

MCP Builder Pattern Guidance Analysis - standardizing samples to prevent AI confusion

## Requirements

- Replace `new NuruAppBuilder()` with `NuruApp.CreateBuilder(args)`
- Migrate from old Mediator registration pattern to canonical pattern
- Add comprehensive header comments explaining pipeline behavior configuration
- Include required package directives

## Checklist

### Implementation
- [x] Update `samples/pipeline-middleware/pipeline-middleware.cs` to use `NuruApp.CreateBuilder(args)`
- [x] Update `samples/unified-middleware/unified-middleware.cs` to use `NuruApp.CreateBuilder(args)`
- [x] Add `#:package Mediator.Abstractions` directive
- [x] Add `#:package Mediator.SourceGenerator` directive
- [x] Migrate from old pattern (`services.AddMediator()` + explicit IPipelineBehavior registrations) to canonical pattern
- [x] Use `options.PipelineBehaviors = [typeof(Behavior<,>)]` for pipeline configuration
- [x] Add comments explaining pipeline behavior execution order (first = outermost)
- [x] Verify both samples compile successfully
- [x] Verify both samples run correctly with expected output

### Additional fixes
- [x] Fixed incorrect project paths (`Source/TimeWarp.Nuru` → `source/timewarp-nuru`)
- [x] Removed redundant `.AddAutoHelp()` calls (included in CreateBuilder)

## Notes

**Old pattern (to be replaced):**
```csharp
services.AddMediator();
// Then explicit registrations for each command:
services.AddSingleton<IPipelineBehavior<EchoCommand, Unit>, LoggingBehavior<EchoCommand, Unit>>();
```

**Canonical pattern (target):**
```csharp
services.AddMediator(options =>
{
  // Pipeline behaviors execute in order: first = outermost (wraps everything)
  options.PipelineBehaviors = [typeof(LoggingBehavior<,>), typeof(TelemetryBehavior<,>)];
});
```

The canonical pattern is:
- AOT-compatible
- Cleaner (one place to configure all behaviors)
- Uses open generics for pipeline behaviors

Reference analysis: `.agent/workspace/2025-12-01T21-15-00_mcp-builder-pattern-guidance-analysis.md`
