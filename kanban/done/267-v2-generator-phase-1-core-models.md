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
- [x] Create `generators/extractors/builders/app-model-builder.cs`
- [x] Verify build succeeds

## Results

Phase 1 completed successfully with 2 commits:

1. **Commit 1.1:** Created 6 new model types:
   - `AppModel` - Top-level application model with routes, behaviors, services
   - `InterceptSiteModel` - File/line/column for [InterceptsLocation]
   - `HelpModel` - Help output configuration
   - `ReplModel` - REPL mode configuration
   - `BehaviorDefinition` - Pipeline behavior with ordering
   - `ServiceDefinition` - DI service registration

2. **Commit 1.2:** Created `AppModelBuilder` fluent builder with:
   - Support for all AppModel properties
   - Route, behavior, service collection methods
   - InterceptSite from Roslyn Location helper
   - Reset method for builder reuse

### Model Summary
```
generators/models/
├── app-model.cs            # Top-level IR
├── behavior-definition.cs  # Middleware/behavior
├── delegate-signature.cs   # (from Phase 0)
├── handler-definition.cs   # (from Phase 0)
├── help-model.cs           # Help config
├── intercept-site-model.cs # Interceptor location
├── parameter-binding.cs    # (from Phase 0)
├── pipeline-definition.cs  # (from Phase 0)
├── repl-model.cs           # REPL config
├── route-definition.cs     # (from Phase 0)
├── segment-definition.cs   # (from Phase 0)
└── service-definition.cs   # DI registration
```

### Build Status
- Analyzer project builds with 0 warnings, 0 errors

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
