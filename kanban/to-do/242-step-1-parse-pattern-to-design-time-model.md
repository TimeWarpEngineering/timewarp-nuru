# Parse route pattern to design-time model

## Parent

#239 Epic: Compile-time endpoint generation

## Description

Create parsing logic that takes a route pattern string and produces a `RouteDefinition` (design-time model). This is Phase 1 of the two-phase source generation approach.

**Input:** Route pattern string like `"add {x:int} {y:int}"`
**Output:** `RouteDefinition` with correct segments (`LiteralDefinition`, `ParameterDefinition`, etc.)

## Context

The source generator needs to:
1. Parse source syntax → build design-time model (this task)
2. From design-time model → emit runtime code (separate task)

Keeping these phases separate allows independent testing and easier reasoning.

## Checklist

- [ ] Create `sandbox/` folder for experimentation
- [ ] Create runfile that parses route pattern strings
- [ ] Evaluate existing parsing in `timewarp-nuru-parsing/` - can we reuse/adapt?
- [ ] Build `RouteDefinition` from parsed pattern
- [ ] Build `SegmentDefinition` hierarchy (Literal, Parameter, Option)
- [ ] Test with various patterns:
  - [ ] `"add {x:int} {y:int}"` - literals + typed params
  - [ ] `"greet {name}"` - untyped param
  - [ ] `"deploy {env} --force"` - with option/flag
  - [ ] `"exec {*args}"` - catch-all
- [ ] Verify design-time model is correct for each case

## Notes

### Existing Parsing Code

Located in `source/timewarp-nuru-parsing/`. Current flow goes directly to runtime structures. We need to evaluate:
- What can be reused?
- What needs to be adapted to target design-time model?
- Should we write fresh parsing?

### Design-Time Model Types (from #240)

```
source/timewarp-nuru-analyzers/models/
  route-definition.cs      → RouteDefinition
  segment-definition.cs    → SegmentDefinition, LiteralDefinition, ParameterDefinition, OptionDefinition
  handler-definition.cs    → HandlerDefinition
  parameter-binding.cs     → ParameterBinding
  pipeline-definition.cs   → PipelineDefinition
```

### Success Criteria

Given pattern `"add {x:int} {y:int}"`, we get:
```csharp
RouteDefinition {
  OriginalPattern = "add {x:int} {y:int}",
  Segments = [
    LiteralDefinition(Position: 0, Value: "add"),
    ParameterDefinition(Position: 1, Name: "x", TypeConstraint: "int", ...),
    ParameterDefinition(Position: 2, Name: "y", TypeConstraint: "int", ...),
  ],
  ...
}
```
