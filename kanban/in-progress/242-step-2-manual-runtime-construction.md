# Manual runtime construction from design-time model

## Parent

#239 Epic: Compile-time endpoint generation

## Description

Given a `RouteDefinition` (design-time model), manually construct the runtime structures needed to match and execute a route. This is Phase 2 of the two-phase approach, done manually first before automating with a source generator.

**Input:** `RouteDefinition` instance
**Output:** Working runtime that can execute `add 2 2` → `4`

## Context

This manual construction:
1. Proves the runtime structures work correctly
2. Shows exactly what the source generator needs to emit
3. Becomes a test case for the eventual source generator

## Checklist

- [ ] Identify what runtime structures are needed:
  - [ ] `CompiledRoute`?
  - [ ] `Endpoint`?
  - [ ] Matchers (`LiteralMatcher`, `ParameterMatcher`)?
  - [ ] Other?
- [ ] Create runfile in `sandbox/` that:
  - [ ] Takes a `RouteDefinition` (from step-1)
  - [ ] Manually builds runtime structures
  - [ ] Matches input args against the route
  - [ ] Invokes the handler
- [ ] Get `add 2 2` working end-to-end
- [ ] Document what code would need to be generated

## Notes

### What We're Building

A minimal working example that bypasses `NuruCoreApp.CreateSlimBuilder()` and manually constructs everything the source generator would emit.

### Open Questions

- Do we use existing runtime types (`CompiledRoute`, `Endpoint`) or create new ones?
- What's the minimal set of types needed for route matching?
- How do we wire up the handler invocation?

### Success Criteria

```bash
dotnet sandbox/manual-runtime.cs -- add 2 2
# Output: 4
```

With the runtime structures built manually from a `RouteDefinition`, not from builder APIs.
