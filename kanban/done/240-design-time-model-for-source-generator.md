# Design-time model for source generator

## Parent

#239 Epic: Compile-time endpoint generation

## Description

Create rich, debuggable data structures for use inside the source generator. These types exist only during compilation - they're never serialized or used at runtime. This enables using LINQ, complex algorithms, and easy debugging during code generation.

## Requirements

- Create `RouteDefinition` and related records in `timewarp-nuru-analyzers`
- Refactor `NuruAttributedRouteGenerator` to build model first, then emit
- Model must capture all intermediate data needed for code emission
- Support for all segment types: literals, parameters, options, catch-all

## Checklist

- [x] Create `source/timewarp-nuru-analyzers/models/` directory (used existing plural `models/`)
- [x] Implement `RouteDefinition` record
- [x] Implement `SegmentDefinition` hierarchy (Literal, Parameter, Option)
- [x] Implement `HandlerDefinition` record
- [x] Implement `ParameterBinding` record
- [x] Implement `PipelineDefinition` for middleware chain
- [x] ~~Refactor `NuruAttributedRouteGenerator` to use model~~ CANCELLED - per epic #239 dual-build strategy, we keep existing generator (AppA) and create NEW generator (AppB) that uses model
- [ ] Add tests for model construction (deferred to #241/#242)

## Notes

### Proposed Types

```csharp
namespace TimeWarp.Nuru.SourceGen.Model;

public record RouteDefinition(
    string OriginalPattern,
    ImmutableArray<SegmentDefinition> Segments,
    MessageType MessageType,
    string? Description,
    HandlerDefinition Handler,
    int ComputedSpecificity,
    int Order
);

public abstract record SegmentDefinition(int Position);

public record LiteralDefinition(int Position, string Value) 
    : SegmentDefinition(Position);

public record ParameterDefinition(
    int Position,
    string Name,
    string? TypeConstraint,
    string? Description,
    bool IsOptional,
    bool IsCatchAll,
    string? ResolvedClrTypeName
) : SegmentDefinition(Position);

public record OptionDefinition(
    int Position,
    string LongForm,
    string? ShortForm,
    string? ParameterName,
    string? TypeConstraint,
    string? Description,
    bool ExpectsValue,
    bool IsOptional,
    bool IsRepeated,
    bool ParameterIsOptional,
    string? ResolvedClrTypeName
) : SegmentDefinition(Position);
```

### Benefits

- LINQ-friendly for sorting, filtering, grouping
- Easy to debug - inspect model before emission
- Validation with rich error messages
- Decouples parsing from emission

## Results

**Completed 2024-12-23**

Created 5 model files in `source/timewarp-nuru-analyzers/models/`:

| File | Types |
|------|-------|
| `route-definition.cs` | `RouteDefinition` |
| `segment-definition.cs` | `SegmentDefinition`, `LiteralDefinition`, `ParameterDefinition`, `OptionDefinition` |
| `handler-definition.cs` | `HandlerDefinition`, `HandlerKind`, `HandlerReturnType` |
| `parameter-binding.cs` | `ParameterBinding`, `BindingSource` |
| `pipeline-definition.cs` | `PipelineDefinition`, `MiddlewareDefinition`, `MiddlewareKind`, `ExecutionPhase` |

All types are `internal sealed record` in namespace `TimeWarp.Nuru.SourceGen.Model`.

**Key decision**: Per epic #239 dual-build strategy, we do NOT refactor the existing `NuruAttributedRouteGenerator`. Instead, we keep it as AppA (reference) and create a NEW generator for AppB that uses these model types. This is handled in #241.
