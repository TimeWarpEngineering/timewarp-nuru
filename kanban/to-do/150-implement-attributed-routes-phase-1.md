# Implement Attributed Routes (Phase 1)

## Description

Request classes with `[NuruRoute]` attributes auto-register without explicit `Map()` calls. This is the **first releasable phase** - production use case for request-based CLIs with clean, attribute-driven development.

**Goal:** Users can decorate request classes (that implement `IRequest`) with attributes and they auto-register. No `Map()` calls needed.

## Parent

148-generate-command-and-handler-from-delegate-map-calls

## Dependencies

- Task 149: Implement CompiledRouteBuilder (Phase 0) - must be complete

## Checklist

### NuruRouteRegistry Infrastructure
- [ ] Create `NuruRouteRegistry` static class for route registration
- [ ] Implement `Register<TRequest>(CompiledRoute route, string pattern)` method
- [ ] Store registered routes for lookup by `NuruApp`
- [ ] Integrate with `IEndpointCollectionBuilder` so registered routes are included
- [ ] Ensure thread-safe registration (module initializers run early)

### Attribute Design
- [ ] Create `[NuruRoute]` attribute - route pattern on request class
- [ ] Create `[NuruRouteAlias]` attribute - additional patterns for same request
- [ ] Create `[NuruRouteGroup]` attribute - shared prefix and options
- [ ] Create `[Parameter]` attribute - positional parameter on property/parameter
- [ ] Create `[Option]` attribute - flag or valued option on property/parameter
- [ ] Create `[GroupOption]` attribute - marks parameter from group's shared options
- [ ] Make `CompiledRouteBuilder` public (currently internal)

### Source Generator
- [ ] Create or extend generator to find classes with `[NuruRoute]` attribute
- [ ] Read attributes from request classes
- [ ] Emit `CompiledRouteBuilder` calls for each attributed request
- [ ] Generate route pattern string for help display
- [ ] Emit `NuruRouteRegistry.Register<T>()` calls via `[ModuleInitializer]`

### Attribute Features
- [ ] Support empty route `[NuruRoute("")]` for default route
- [ ] Support nested literals `[NuruRoute("docker compose up")]`
- [ ] Infer optional from nullability (`string?` = optional parameter)
- [ ] Infer option type from property type (`bool` = flag, else valued)
- [ ] Support `IsCatchAll` on `[Parameter]` attribute

### Testing
- [ ] Test simple request with `[NuruRoute]`
- [ ] Test request with `[Parameter]` attributes
- [ ] Test request with `[Option]` attributes (flags and valued)
- [ ] Test `[NuruRouteAlias]` multiple patterns
- [ ] Test `[NuruRouteGroup]` with shared options
- [ ] Test default route `[NuruRoute("")]`
- [ ] Test catch-all parameter
- [ ] Verify auto-registration works without `Map()` calls

## Notes

### Reference

- **Design doc:** `kanban/in-progress/148-nuru-3-unified-route-pipeline/fluent-route-builder-design.md` (lines 610-833)

### Example Usage

```csharp
// User writes - no Map() call needed!
[NuruRoute("deploy", Description = "Deploy to an environment")]
public sealed class DeployRequest : IRequest<Unit>  // Unit = no result (side-effect only)
{
    [Parameter(Description = "Target environment")]
    public string Env { get; set; } = string.Empty;
    
    [Option("force", "f", Description = "Skip confirmation")]
    public bool Force { get; set; }
    
    [Option("config", "c")]
    public string? ConfigFile { get; set; }
}

public sealed class DeployRequestHandler : IRequestHandler<DeployRequest, Unit>
{
    public Task<Unit> Handle(DeployRequest request, CancellationToken ct) 
    {
        // Do deployment...
        return Unit.Task;  // Exit code 0 on success, non-zero on exception
    }
}

// Generated route: "deploy {env} --force,-f --config,-c {configFile?}"
// Auto-registered via [ModuleInitializer]
```

**Notes:**
- Request classes use classes with properties, NOT records or primary constructors
- `IRequest<Unit>` for side-effect requests, `IRequest<T>` for requests that return results
- Exit code is automatic: 0 on success, non-zero on exception

### Generated Output

```csharp
// Source generator emits:
private static readonly CompiledRoute __Route_DeployRequest = new CompiledRouteBuilder()
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
        NuruRouteRegistry.Register<DeployRequest>(__Route_DeployRequest, "deploy {env} --force,-f --config,-c {configFile?}");
    }
}
```

### Why Phase 1 Before Phase 2

Attributed routes are simpler than delegate generation:
- No request/handler class generation needed (user provides both)
- No delegate body extraction or parameter rewriting
- Just read attributes -> emit builder calls -> register

### Releasable

Yes - this phase provides immediate value for request-based CLIs.

## Design Decisions

### 1. `CompiledRouteBuilder` Visibility

**Decision: Make it public**

The builder needs to be public so generated code can access it. Generating into another namespace with internal access is unnecessarily complex.

### 2. Attribute Dash Convention

**Decision: No dashes in attribute - use two separate parameters**

The `[Option]` attribute takes `longForm` and `shortForm` as separate parameters without dashes:

```csharp
[Option("force", "f")]  // Generator adds -- and - when building route
public bool Force { get; set; }
```

Attribute constructor signature:
```csharp
public OptionAttribute(string longForm, string? shortForm = null)
```

The generator adds `--` and `-` prefixes when building the route pattern. This is cleaner than making users type dashes that would just be stripped anyway.

## Open Questions

The following questions still need answers before implementation begins:

### 3. IBaseRequest Constraint

Which interface should the `NuruRouteRegistry.Register<T>()` constraint use?

**Options:**
- A) `where TRequest : IBaseRequest` (MediatR's common base for both) - **Recommended**
- B) No constraint (just `where TRequest : class`)
- C) Two overloads: one for `IRequest`, one for `IRequest<T>`

### 4. Group Inheritance Mechanism

For `[NuruRouteGroup]`, how should the generator discover group membership?

**Options:**
- A) Look at base class for `[NuruRouteGroup]` attribute (inheritance-based) - **Recommended**
- B) Each request explicitly declares `[NuruRouteGroup("prefix")]` (explicit membership)
- C) Both: inherit from base OR declare explicitly

### 5. Sample Application

Should a sample application demonstrating attributed routes be created as part of this task?

**Options:**
- A) Yes, create `samples/attributed-routes/` with example request classes
- B) No, add examples to existing samples later
- C) Add to documentation only
