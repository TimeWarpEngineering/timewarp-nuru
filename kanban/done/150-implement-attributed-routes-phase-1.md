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
- [x] 187: Add Route Matching Tests for Attributed Routes
- [x] 188: Update Existing Basic Tests with Missing Coverage
- [x] 189: Add MessageType Detection to Attributed Route Generator

## Checklist

### NuruRouteRegistry Infrastructure
- [x] Create `NuruRouteRegistry` static class for route registration
- [x] Implement `Register<TRequest>(CompiledRoute route, string pattern) where TRequest : IMessage` method
- [x] Store registered routes for lookup by `NuruApp`
- [x] Integrate with `IEndpointCollectionBuilder` so registered routes are included
- [x] Ensure thread-safe registration (module initializers run early)

### Attribute Design
- [x] Create `[NuruRoute]` attribute - route pattern on request class
- [x] Create `[NuruRouteAlias]` attribute - additional patterns for same request
- [x] Create `[NuruRouteGroup]` attribute - shared prefix and options (applied to base class)
- [x] Create `[Parameter]` attribute - positional parameter on property/parameter
- [x] Create `[Option]` attribute - flag or valued option on property/parameter (no dashes, two params)
- [x] Create `[GroupOption]` attribute - marks parameter from group's shared options
- [x] Make `CompiledRouteBuilder` public (currently internal)

### Source Generator
- [x] Create or extend generator to find classes with `[NuruRoute]` attribute
- [x] Read attributes from request classes
- [x] Look up inheritance chain for `[NuruRouteGroup]` on base classes
- [x] Emit `CompiledRouteBuilder` calls for each attributed request
- [x] Generate route pattern string for help display
- [x] Emit `NuruRouteRegistry.Register<T>()` calls via `[ModuleInitializer]`
- [x] Read `IQuery<T>`, `ICommand<T>`, `IIdempotent` from request class → emit `WithMessageType()` call

### Attribute Features
- [x] Support empty route `[NuruRoute("")]` for default route
- [x] Support nested literals `[NuruRoute("docker compose up")]`
- [x] Infer optional from nullability (`string?` = optional parameter)
- [x] Infer option type from property type (`bool` = flag, else valued)
- [x] Support `IsCatchAll` on `[Parameter]` attribute

### Sample Application
- [x] Create `samples/attributed-routes/` directory
- [x] Implement example requests demonstrating all attribute features
- [x] Include grouped requests example (inheritance-based)
- [x] Include default route example
- [x] Include aliases example

### Testing
- [x] Test simple request with `[NuruRoute]`
- [x] Test request with `[Parameter]` attributes
- [x] Test request with `[Option]` attributes (flags and valued)
- [x] Test `[NuruRouteAlias]` multiple patterns
- [x] Test `[NuruRouteGroup]` with shared options (inheritance-based)
- [x] Test default route `[NuruRoute("")]`
- [x] Test catch-all parameter
- [x] Verify auto-registration works without `Map()` calls

## Results

Phase 1 of Attributed Routes is **complete and releasable**. Users can now:

1. Decorate request classes with `[NuruRoute]` and related attributes
2. Have routes auto-register via `[ModuleInitializer]` - no `Map()` calls needed
3. Use `[NuruRouteGroup]` on base classes for shared prefixes and options
4. Use `[NuruRouteAlias]` for command aliases
5. Get automatic MessageType inference from `IQuery<T>`, `ICommand<T>`, `IIdempotent`

### Key Files

**Attributes:**
- `source/timewarp-nuru-core/attributes/nuru-route-attribute.cs`
- `source/timewarp-nuru-core/attributes/nuru-route-alias-attribute.cs`
- `source/timewarp-nuru-core/attributes/nuru-route-group-attribute.cs`
- `source/timewarp-nuru-core/attributes/parameter-attribute.cs`
- `source/timewarp-nuru-core/attributes/option-attribute.cs`
- `source/timewarp-nuru-core/attributes/group-option-attribute.cs`

**Infrastructure:**
- `source/timewarp-nuru-core/nuru-route-registry.cs`
- `source/timewarp-nuru-analyzers/analyzers/nuru-attributed-route-generator.cs`

**Sample:**
- `samples/attributed-routes/` - comprehensive example application

**Tests:**
- `tests/timewarp-nuru-analyzers-tests/auto/attributed-route-generator-01-basic.cs`
- `tests/timewarp-nuru-analyzers-tests/auto/attributed-route-generator-02-source.cs`
- `tests/timewarp-nuru-analyzers-tests/auto/attributed-route-generator-03-matching.cs`
- `tests/timewarp-nuru-analyzers-tests/auto/attributed-route-generator-04-messagetype.cs`

## Notes

### Reference

- **Design doc:** `kanban/in-progress/148-nuru-3-unified-route-pipeline/fluent-route-builder-design.md` (lines 610-833)

### Example Usage

```csharp
// User writes - no Map() call needed!
[NuruRoute("deploy", Description = "Deploy to an environment")]
public sealed class DeployRequest : ICommand<Unit>
{
    [Parameter(Description = "Target environment")]
    public string Env { get; set; } = string.Empty;
    
    [Option("force", "f", Description = "Skip confirmation")]
    public bool Force { get; set; }
    
    [Option("config", "c")]
    public string? ConfigFile { get; set; }
}

public sealed class DeployRequestHandler : ICommandHandler<DeployRequest, Unit>
{
    public ValueTask<Unit> Handle(DeployRequest request, CancellationToken ct) 
    {
        // Do deployment...
        return default;
    }
}

// Generated route: "deploy {env} --force,-f --config,-c {configFile?}"
// Auto-registered via [ModuleInitializer]
// MessageType.Command inferred from ICommand<T>
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
public sealed class DockerRunRequest : DockerRequestBase, ICommand<Unit>
{
    [Parameter]
    public string Image { get; set; } = string.Empty;
}

[NuruRoute("ps")]
public sealed class DockerPsQuery : DockerRequestBase, IQuery<Unit>
{
    [Option("all", "a")]
    public bool All { get; set; }
}

// Generated routes:
// "docker run {image} --debug,-D" (C)
// "docker ps --all,-a --debug,-D" (Q)
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

The builder needs to be public so generated code can access it.

### 2. Attribute Dash Convention

**Decision: No dashes in attribute - use two separate parameters**

```csharp
[Option("force", "f")]  // Generator adds -- and - when building route
public bool Force { get; set; }
```

### 3. IMessage Constraint

**Decision: Use `where TRequest : IMessage`**

`IMessage` is the base interface for all message types. If something doesn't implement `IMessage`, it's not a valid message to register as a route.

### 4. Group Inheritance Mechanism

**Decision: Inheritance-based**

The `[NuruRouteGroup]` attribute is placed on a base class. Request classes inherit group membership by extending that base class.

### 5. MessageType Inference

**Decision: Automatic from interfaces**

The source generator reads `IQuery<T>`, `ICommand<T>`, and `IIdempotent` interfaces from the request class and emits the appropriate `WithMessageType()` call:

- `IQuery<T>` → `MessageType.Query`
- `ICommand<T>` + `IIdempotent` → `MessageType.IdempotentCommand`
- `ICommand<T>` → `MessageType.Command`
- `IRequest<T>` or none → `MessageType.Unspecified`
