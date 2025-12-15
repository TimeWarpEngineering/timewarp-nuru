# Implement Attributed Routes (Phase 1)

## Description

IRequest classes with `[NuruRoute]` attributes auto-register without explicit `Map()` calls. This is the **first releasable phase** - production use case for IRequest-based CLIs with clean, attribute-driven development.

**Goal:** Users can decorate IRequest classes with attributes and they auto-register. No `Map()` calls needed.

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
- [ ] Create `[NuruRoute]` attribute - route pattern on IRequest class
- [ ] Create `[NuruRouteAlias]` attribute - additional patterns for same IRequest
- [ ] Create `[NuruRouteGroup]` attribute - shared prefix and options
- [ ] Create `[Parameter]` attribute - positional parameter on property/parameter
- [ ] Create `[Option]` attribute - flag or valued option on property/parameter
- [ ] Create `[GroupOption]` attribute - marks parameter from group's shared options

### Source Generator
- [ ] Create or extend generator to find classes with `[NuruRoute]` attribute
- [ ] Read attributes from IRequest classes
- [ ] Emit `CompiledRouteBuilder` calls for each attributed IRequest
- [ ] Generate route pattern string for help display
- [ ] Emit `NuruRouteRegistry.Register<T>()` calls via `[ModuleInitializer]`

### Attribute Features
- [ ] Support empty route `[NuruRoute("")]` for default route
- [ ] Support nested literals `[NuruRoute("docker compose up")]`
- [ ] Infer optional from nullability (`string?` = optional parameter)
- [ ] Infer option type from property type (`bool` = flag, else valued)
- [ ] Support `IsCatchAll` on `[Parameter]` attribute

### Testing
- [ ] Test simple IRequest with `[NuruRoute]`
- [ ] Test IRequest with `[Parameter]` attributes
- [ ] Test IRequest with `[Option]` attributes (flags and valued)
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
    
    [Option("--force", "-f", Description = "Skip confirmation")]
    public bool Force { get; set; }
    
    [Option("--config", "-c")]
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
- IRequest classes use classes with properties, NOT records or primary constructors
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
- No IRequest/Handler class generation needed (user provides both)
- No delegate body extraction or parameter rewriting
- Just read attributes -> emit builder calls -> register

### Releasable

Yes - this phase provides immediate value for IRequest-based CLIs.

## Clarifying Questions

The following questions need answers before implementation begins:

### 1. `CompiledRouteBuilder` Visibility

The builder is currently `internal`. Should it be made `public` so the generated code can access it, or should the generator emit the builder code into the same namespace with internal visibility?

**Options:**
- A) Make `CompiledRouteBuilder` public
- B) Generator emits into `TimeWarp.Nuru` namespace with `internal` access
- C) Use `[InternalsVisibleTo]` for generated code assembly

### 2. Attribute Dash Convention

The design doc shows `[Option("--force", "-f")]` with dashes. Should users include dashes or should we accept either form?

**Options:**
- A) Require dashes (exactly as shown: `"--force"`, `"-f"`)
- B) Strip dashes automatically (accept `"force"` or `"--force"`)
- C) Require no dashes (user writes `"force"`, `"f"`)

### 3. IBaseRequest Constraint

The design references `IBaseRequest`, but the codebase uses `IRequest` and `IRequest<T>`. Which interface should the `NuruRouteRegistry.Register<T>()` constraint use?

**Options:**
- A) `where TRequest : IBaseRequest` (MediatR's common base for both)
- B) No constraint (just `where TRequest : class`)
- C) Two overloads: one for `IRequest`, one for `IRequest<T>`

### 4. Group Inheritance Mechanism

For `[NuruRouteGroup]`, how should the generator discover group membership?

**Options:**
- A) Look at base class for `[NuruRouteGroup]` attribute (inheritance-based)
- B) Each IRequest explicitly declares `[NuruRouteGroup("prefix")]` (explicit membership)
- C) Both: inherit from base OR declare explicitly

### 5. Sample Application

Should a sample application demonstrating attributed routes be created as part of this task?

**Options:**
- A) Yes, create `samples/attributed-routes/` with example IRequest classes
- B) No, add examples to existing samples later
- C) Add to documentation only
