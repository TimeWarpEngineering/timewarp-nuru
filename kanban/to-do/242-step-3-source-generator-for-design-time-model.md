# Source generator for design-time model

## Parent

#239 Epic: Compile-time endpoint generation

## Description

Build the source generator incrementally, one function at a time. Start in `sandbox/` with testable helper methods that:
1. Extract syntax (route pattern, handler info) from `Map()` calls
2. Parse pattern string into `RouteDefinition` (design-time model)
3. (Step-4 will handle runtime code emission)

All helper methods are public and unit-testable - they're in the sourcegen, not the library API.

## Approach

Work backwards from "we need this":

```
Map("add {x:int} {y:int}").WithHandler(...) 
  → Extract pattern string + handler info (syntax extraction)
  → Parse into RouteDefinition (pattern parsing)
  → Generate runtime code (step-4)
```

## Checklist

### Setup
- [ ] Reorganize sandbox: existing experiments in `sandbox/experiments/`
- [ ] Create `sandbox/sourcegen/` for new generator code
- [ ] Create `sandbox/tests/` for unit tests

### Syntax Extraction
- [ ] Helper method: Given `Map()` invocation syntax, extract pattern string
- [ ] Helper method: Given `WithHandler()` call, extract delegate info
- [ ] Tests: Verify extraction works for `Map("add {x:int} {y:int}")`

### Pattern Parsing
- [ ] Evaluate existing parser in `source/timewarp-nuru-parsing/`
- [ ] Helper method: Given pattern string, return `RouteDefinition`
- [ ] Tests: Verify `"add {x:int} {y:int}"` produces correct model (compare to step-1)

### Integration
- [ ] Wire syntax extraction → pattern parsing → RouteDefinition
- [ ] Test: Given source code with `Map()` call, get valid `RouteDefinition`

## Notes

### What We Already Have
- `sandbox/build-design-time-model.cs` - manually built RouteDefinition (step-1)
- `sandbox/manual-runtime-construction.cs` - manually built runtime (step-2)
- Existing lexer/parser in `source/timewarp-nuru-parsing/` (may reuse)

### Testing Strategy
- All sourcegen helpers are public for testability
- Compare outputs to manual construction from step-1
- Unit tests in `sandbox/tests/`

### Scope
- Start with delegate-based `Map()` calls only
- Pattern: `"add {x:int} {y:int}"`
- Attributed routes and Fluent Builder are future scope

### Agent Context
- Agent name: Amina
- Working on Epic #239: Compile-time endpoint generation
- Step-1 and Step-2 complete (manual design-time and runtime construction)
