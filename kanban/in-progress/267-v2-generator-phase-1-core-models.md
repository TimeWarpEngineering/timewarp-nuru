# V2 Generator Phase 1: Core Models

## Description

Create the new model types needed for V2 generator that don't exist in the current codebase. The existing models (`RouteDefinition`, `HandlerDefinition`, etc.) were moved in Phase 0 and are reusable. This phase adds the missing top-level `AppModel` and supporting types.

## Parent

#265 Epic: V2 Source Generator Implementation

## Checklist

### Commit 1.1: Create new model files
- [x] Create `generators/models/app-model.cs`
- [x] Create `generators/models/intercept-site-model.cs`
- [x] Create `generators/models/help-model.cs`
- [x] Create `generators/models/repl-model.cs`
- [x] Create `generators/models/behavior-definition.cs`
- [x] Create `generators/models/service-definition.cs`

### Commit 1.2: Create AppModelBuilder
- [ ] Create `generators/extractors/builders/app-model-builder.cs`
- [ ] Verify build succeeds

## Notes

### AppModel Structure
```csharp
internal sealed record AppModel(
    string? Name,
    string? Description,
    string? AiPrompt,
    bool HasHelp,
    HelpModel? HelpOptions,
    bool HasRepl,
    ReplModel? ReplOptions,
    bool HasConfiguration,
    ImmutableArray<RouteDefinition> Routes,
    ImmutableArray<BehaviorDefinition> Behaviors,
    ImmutableArray<ServiceDefinition> Services,
    InterceptSiteModel InterceptSite);
```

### InterceptSiteModel Structure
```csharp
internal sealed record InterceptSiteModel(
    string FilePath,
    int Line,
    int Column);
```

### Supporting Types
- `HelpModel` - Help configuration options
- `ReplModel` - REPL configuration options
- `BehaviorDefinition` - Pipeline behavior (type + order)
- `ServiceDefinition` - Service registration info

### Existing Models (from Phase 0)
- `RouteDefinition` - Complete route with segments, handler, pipeline
- `SegmentDefinition` - Abstract base + Literal/Parameter/Option
- `HandlerDefinition` - Handler info with parameters
- `ParameterBinding` - Parameter binding source
- `PipelineDefinition` - Middleware pipeline
