# Implement Attributed Routes (Phase 1)

## Description

Commands with `[Route]` attributes auto-register without explicit `Map()` calls. This is the **first releasable phase** - production use case for Command-based CLIs with clean, attribute-driven development.

**Goal:** Users can decorate Command classes with attributes and they auto-register. No `Map()` calls needed.

## Parent

148-generate-command-and-handler-from-delegate-map-calls

## Dependencies

- Task 149: Implement CompiledRouteBuilder (Phase 0) - must be complete

## Checklist

### NuruRouteRegistry Infrastructure
- [ ] Create `NuruRouteRegistry` static class for route registration
- [ ] Implement `Register<TCommand>(CompiledRoute route, string pattern)` method
- [ ] Store registered routes for lookup by `NuruApp`
- [ ] Integrate with `IEndpointCollectionBuilder` so registered routes are included
- [ ] Ensure thread-safe registration (module initializers run early)

### Attribute Design
- [ ] Create `[Route]` attribute - route pattern on Command class
- [ ] Create `[RouteAlias]` attribute - additional patterns for same command
- [ ] Create `[RouteGroup]` attribute - shared prefix and options
- [ ] Create `[Parameter]` attribute - positional parameter on property/parameter
- [ ] Create `[Option]` attribute - flag or valued option on property/parameter
- [ ] Create `[GroupOption]` attribute - marks parameter from group's shared options

### Source Generator
- [ ] Create or extend generator to find classes with `[Route]` attribute
- [ ] Read attributes from Command classes
- [ ] Emit `CompiledRouteBuilder` calls for each attributed Command
- [ ] Generate route pattern string for help display
- [ ] Emit `NuruRouteRegistry.Register<T>()` calls via `[ModuleInitializer]`

### Attribute Features
- [ ] Support empty route `[Route("")]` for default route
- [ ] Support nested literals `[Route("docker compose up")]`
- [ ] Infer optional from nullability (`string?` = optional parameter)
- [ ] Infer option type from property type (`bool` = flag, else valued)
- [ ] Support `IsCatchAll` on `[Parameter]` attribute

### Testing
- [ ] Test simple command with `[Route]`
- [ ] Test command with `[Parameter]` attributes
- [ ] Test command with `[Option]` attributes (flags and valued)
- [ ] Test `[RouteAlias]` multiple patterns
- [ ] Test `[RouteGroup]` with shared options
- [ ] Test default route `[Route("")]`
- [ ] Test catch-all parameter
- [ ] Verify auto-registration works without `Map()` calls

## Notes

### Reference

- **Design doc:** `kanban/to-do/148-generate-command-and-handler-from-delegate-map-calls/fluent-route-builder-design.md` (lines 610-833)

### Example Usage

```csharp
// User writes - no Map() call needed!
[Route("deploy", Description = "Deploy to an environment")]
public sealed class DeployCommand : IRequest<int>
{
    [Parameter(Description = "Target environment")]
    public string Env { get; set; } = string.Empty;
    
    [Option("--force", "-f", Description = "Skip confirmation")]
    public bool Force { get; set; }
    
    [Option("--config", "-c")]
    public string? ConfigFile { get; set; }
}

public sealed class DeployCommandHandler : IRequestHandler<DeployCommand, int>
{
    public Task<int> Handle(DeployCommand request, CancellationToken ct) { ... }
}

// Generated route: "deploy {env} --force,-f --config,-c {configFile?}"
// Auto-registered via [ModuleInitializer]
```

**Notes:**
- Commands use classes with properties, NOT records or primary constructors
- Actual interfaces are `IRequest<TResponse>` / `IRequestHandler<TRequest, TResponse>` (Mediator)
- We use "Command" in class names as it's more natural CLI terminology

### Generated Output

```csharp
// Source generator emits:
private static readonly CompiledRoute __Route_DeployCommand = new CompiledRouteBuilder()
    .WithLiteral("deploy")
    .WithParameter("env")
    .WithOption("force", shortForm: "f")
    .WithOption("config", shortForm: "c", expectsValue: true, isOptional: true)
    .Build();

internal static class GeneratedRouteRegistration
{
    [ModuleInitializer]
    public static void Register()
    {
        NuruRouteRegistry.Register<DeployCommand>(__Route_DeployCommand, "deploy {env} --force,-f --config,-c {configFile?}");
    }
}
```

### Why Phase 1 Before Phase 2

Attributed routes are simpler than delegate generation:
- No Command/Handler class generation needed (user provides both)
- No delegate body extraction or parameter rewriting
- Just read attributes -> emit builder calls -> register

### Releasable

Yes - this phase provides immediate value for Command-based CLIs.
