# Update Calculator Mediator Samples with Canonical Pattern

## Description

Convert `calc-mediator.cs` and `calc-mixed.cs` to use `NuruApp.CreateBuilder(args)` with the canonical Mediator registration pattern. These samples use `Map<TCommand>` which requires Mediator packages and proper configuration.

## Parent

MCP Builder Pattern Guidance Analysis - standardizing samples to prevent AI confusion

## Requirements

- Replace `new NuruAppBuilder()` with `NuruApp.CreateBuilder(args)`
- Implement canonical Mediator registration pattern with `ConfigureServices`
- Add comprehensive header comments explaining Mediator requirements
- Include required package directives

## Checklist

### Implementation
- [ ] Update `samples/calculator/calc-mediator.cs` to use `NuruApp.CreateBuilder(args)`
- [ ] Update `samples/calculator/calc-mixed.cs` to use `NuruApp.CreateBuilder(args)`
- [ ] Add `#:package Mediator.Abstractions` directive
- [ ] Add `#:package Mediator.SourceGenerator` directive
- [ ] Implement canonical `ConfigureServices` pattern with `services.AddMediator(options => {...})`
- [ ] Add comprehensive header comments explaining how Mediator.SourceGenerator works
- [ ] Include "COMMON ERROR" section about missing IMediator registration
- [ ] Verify both samples compile successfully
- [ ] Verify both samples run correctly with expected output

## Notes

The canonical pattern (from `aspire-host-otel/nuru-client.cs`):

```csharp
#!/usr/bin/env -S dotnet run --
#:package Mediator.Abstractions
#:package Mediator.SourceGenerator
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj

// ═══════════════════════════════════════════════════════════════════════════════
// MEDIATOR PATTERN - CANONICAL EXAMPLE
// ═══════════════════════════════════════════════════════════════════════════════
//
// REQUIRED PACKAGES:
//   #:package Mediator.Abstractions    - Interfaces (IRequest, IRequestHandler)
//   #:package Mediator.SourceGenerator - Generates AddMediator() in YOUR assembly
//
// HOW IT WORKS:
//   Mediator.SourceGenerator scans YOUR assembly at compile time for:
//   - IRequest implementations (commands)
//   - IRequestHandler<> implementations (handlers)
//   Then generates a type-safe AddMediator() extension method specific to YOUR project.
//
// COMMON ERROR:
//   "No service for type 'Mediator.IMediator' has been registered"
//   SOLUTION: Install BOTH packages AND call services.AddMediator(options => {...})
// ═══════════════════════════════════════════════════════════════════════════════

NuruCoreApp app = NuruApp.CreateBuilder(args)
  .ConfigureServices(ConfigureServices)
  .Map<MyCommand>(pattern: "mycommand {param}", description: "Does something")
  .Build();

return await app.RunAsync(args);

static void ConfigureServices(IServiceCollection services)
{
  services.AddMediator(options =>
  {
    options.PipelineBehaviors = [typeof(TelemetryBehavior<,>)];
  });
}
```

Reference analysis: `.agent/workspace/2025-12-01T21-15-00_mcp-builder-pattern-guidance-analysis.md`
