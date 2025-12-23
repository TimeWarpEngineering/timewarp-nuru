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
- [ ] Refactor `NuruAttributedRouteGenerator` to use model
- [ ] Add tests for model construction

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
