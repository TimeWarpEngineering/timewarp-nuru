# Implement Attributed Routes (Phase 1)

## Type

Epic

## Description

Request classes with `[NuruRoute]` attributes auto-register without explicit `Map()` calls. This is the **first releasable phase** - production use case for request-based CLIs with clean, attribute-driven development.

**Goal:** Users can decorate request classes (that implement `IMessage`) with attributes and they auto-register. No `Map()` calls needed.

## Parent

148-generate-command-and-handler-from-delegate-map-calls

## Dependencies

- Task 149: Implement CompiledRouteBuilder (Phase 0) - must be complete

## Subtasks

- [x] 186: Add Source Generation Verification Tests (with test utilities)
- [ ] 187: Add Route Matching Tests for Attributed Routes
- [ ] 188: Update Existing Basic Tests with Missing Coverage

## Checklist

### NuruRouteRegistry Infrastructure
- [ ] Create `NuruRouteRegistry` static class for route registration
- [ ] Implement `Register<TRequest>(CompiledRoute route, string pattern) where TRequest : IMessage` method
- [ ] Store registered routes for lookup by `NuruApp`
- [ ] Integrate with `IEndpointCollectionBuilder` so registered routes are included
- [ ] Ensure thread-safe registration (module initializers run early)

### Attribute Design
- [ ] Create `[NuruRoute]` attribute - route pattern on request class
- [ ] Create `[NuruRouteAlias]` attribute - additional patterns for same request
- [ ] Create `[NuruRouteGroup]` attribute - shared prefix and options (applied to base class)
- [ ] Create `[Parameter]` attribute - positional parameter on property/parameter
- [ ] Create `[Option]` attribute - flag or valued option on property/parameter (no dashes, two params)
- [ ] Create `[GroupOption]` attribute - marks parameter from group's shared options
- [ ] Make `CompiledRouteBuilder` public (currently internal)

### Source Generator
- [ ] Create or extend generator to find classes with `[NuruRoute]` attribute
- [ ] Read attributes from request classes
- [ ] Look up inheritance chain for `[NuruRouteGroup]` on base classes
- [ ] Emit `CompiledRouteBuilder` calls for each attributed request
- [ ] Generate route pattern string for help display
- [ ] Emit `NuruRouteRegistry.Register<T>()` calls via `[ModuleInitializer]`
- [ ] Read `IQuery<T>`, `ICommand<T>`, `IIdempotent` from request class → emit `WithMessageType()` call

### Attribute Features
- [ ] Support empty route `[NuruRoute("")]` for default route
- [ ] Support nested literals `[NuruRoute("docker compose up")]`
- [ ] Infer optional from nullability (`string?` = optional parameter)
- [ ] Infer option type from property type (`bool` = flag, else valued)
- [ ] Support `IsCatchAll` on `[Parameter]` attribute

### Sample Application
- [ ] Create `samples/attributed-routes/` directory
- [ ] Implement example requests demonstrating all attribute features
- [ ] Include grouped requests example (inheritance-based)
- [ ] Include default route example
- [ ] Include aliases example

### Testing
- [ ] Test simple request with `[NuruRoute]`
- [ ] Test request with `[Parameter]` attributes
- [ ] Test request with `[Option]` attributes (flags and valued)
- [ ] Test `[NuruRouteAlias]` multiple patterns
- [ ] Test `[NuruRouteGroup]` with shared options (inheritance-based)
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

### Grouped Requests Example (Inheritance-Based)

```csharp
// Base class defines the group - applied once
[NuruRouteGroup("docker")]
public abstract class DockerRequestBase
{
    [GroupOption("debug", "D")]
    public bool Debug { get; set; }
}

// Requests inherit group membership
[NuruRoute("run")]
public sealed class DockerRunRequest : DockerRequestBase, IRequest<Unit>
{
    [Parameter]
    public string Image { get; set; } = string.Empty;
}

[NuruRoute("build")]
public sealed class DockerBuildRequest : DockerRequestBase, IRequest<Unit>
{
    [Parameter]
    public string Path { get; set; } = ".";
}

// Generated routes:
// "docker run {image} --debug,-D"
// "docker build {path} --debug,-D"
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
    .WithOption("config", shortForm: "c", expectsValue: true, parameterIsOptional: true)
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

### 3. IMessage Constraint

**Decision: Use `where TRequest : IMessage`**

```csharp
public static void Register<TRequest>(CompiledRoute route, string pattern, string? description = null)
    where TRequest : IMessage
```

`IMessage` is the base interface in Martin Mediator for all message types (`IBaseRequest`, `IBaseCommand`, `IBaseQuery`). If something doesn't implement `IMessage`, it's not a valid message to register as a route.

**Note on Query/Command Distinction:** ~~The current design (string syntax, fluent API, attributes) does not distinguish between `IBaseQuery` (read operations), `IBaseCommand` (write operations), and `IBaseRequest`.~~ **Resolved by Task 156:** `MessageType` enum and `IIdempotent` marker interface are now implemented. For attributed routes, users write classes implementing `IQuery<T>`, `ICommand<T>`, or `ICommand<T>, IIdempotent` directly - the source generator just reads these interfaces and emits the appropriate `WithMessageType()` call.

### 4. Group Inheritance Mechanism

**Decision: Inheritance-based**

The `[NuruRouteGroup]` attribute is placed on a base class. Request classes inherit group membership by extending that base class:

```csharp
[NuruRouteGroup("docker")]
public abstract class DockerRequestBase
{
    [GroupOption("debug", "D")]
    public bool Debug { get; set; }
}

[NuruRoute("run")]
public sealed class DockerRunRequest : DockerRequestBase, IRequest<Unit>
{
    [Parameter]
    public string Image { get; set; } = string.Empty;
}
```

The generator walks up the inheritance chain to find `[NuruRouteGroup]` attributes and combines the group prefix with the route pattern.

### 5. Sample Application

**Decision: Yes, create `samples/attributed-routes/`**

A sample application will be created demonstrating:
- Simple requests with `[NuruRoute]`
- Parameters and options
- Grouped requests (inheritance-based)
- Default routes
- Aliases
- Catch-all parameters

This provides a runnable example for visualization and testing.
