# Source generator for design-time model

## Parent

#239 Epic: Compile-time endpoint generation

## Description

Create a new source generator that automates Phase 1: parsing source code syntax into design-time model (`RouteDefinition`). This extracts the manual parsing work from step-1 into an actual source generator.

## Context

After manually proving:
1. step-1: We can parse patterns → design-time model
2. step-2: We can build runtime from design-time model

Now we automate step-1 with a source generator.

## Checklist

- [ ] Create new source generator project (name TBD - "timewarp-nuru-sourcegen-v2"?)
- [ ] Generator scans for `Map()` calls or `[NuruRoute]` attributes
- [ ] For each route found, build `RouteDefinition`
- [ ] Emit the design-time model as generated code (or use directly for phase 2)
- [ ] Unit tests using manual construction from step-1 as reference
- [ ] Integrate with dual-build AppB

## Notes

### Project Naming

Needs a good name. Options:
- `timewarp-nuru-sourcegen-v2`
- `timewarp-nuru-gen`
- `timewarp-nuru-compile`
- Other?

### Incremental Generator

Should use `IIncrementalGenerator` for performance.

### What It Produces

The generator builds `RouteDefinition` instances internally, then uses them to emit Phase 2 code. The design-time model itself doesn't need to be emitted - it's just internal to the generator.
