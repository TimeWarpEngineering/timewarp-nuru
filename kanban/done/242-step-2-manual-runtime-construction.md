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

- [x] Identify what runtime structures are needed:
  - [x] `CompiledRoute` - holds matchers, extractors, invoker
  - [x] `ISegmentMatcher` - LiteralMatcher, IntParameterMatcher
  - [x] `ParameterExtractor` - extracts and converts params
  - [x] `Router` - matches args against routes
- [x] Create runfile in `sandbox/` that:
  - [x] Takes a `RouteDefinition` (from step-1)
  - [x] Manually builds runtime structures
  - [x] Matches input args against the route
  - [x] Invokes the handler
- [x] Get `add 2 2` working end-to-end
- [x] Document what code would need to be generated

## Results

**Completed 2024-12-23**

Created `sandbox/manual-runtime-construction.cs` (900 lines) that:

1. **Builds RouteDefinition** for "add {x:int} {y:int}" (from step-1)

2. **Constructs minimal runtime structures**:
   - `CompiledRoute` - holds segment matchers, parameter extractors, and invoker
   - `ISegmentMatcher` with implementations:
     - `LiteralMatcher` - matches exact literal values ("add")
     - `IntParameterMatcher` - matches values parseable as int
   - `ParameterExtractor` - extracts and converts parameter values
   - `TypeConverter` - converts strings to typed values
   - `Router` - matches args against registered routes
   - `MatchResult` / `RouteMatchAttempt` - hold match results

3. **Successfully executes**:
   - `add 2 2` → `4` ✓
   - `add 10 20` → `30` ✓
   - `add -5 15` → `10` ✓
   - `add foo bar` → Error (non-integers rejected) ✓
   - `subtract 2 2` → Error (wrong command) ✓

### What the Source Generator Needs to Emit

For each route, the generator should emit:
1. A `CompiledRoute` instance with:
   - Segment matchers based on segment types (literal vs parameter)
   - Parameter extractors with correct type converters
   - An invoker delegate that extracts typed params and calls user's handler
2. A `Router` instance with all compiled routes
3. The matching/execution logic

## Notes

### Decision Made

Used **Option B**: Built minimal custom runtime structures inline rather than using existing Nuru types. This clearly shows what needs to be generated and keeps the experiment self-contained.
