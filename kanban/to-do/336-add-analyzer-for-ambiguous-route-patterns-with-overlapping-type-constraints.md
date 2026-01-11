# Add analyzer for ambiguous route patterns with overlapping type constraints

## Summary

Add a new analyzer diagnostic (NURU_R001 or similar) that detects route patterns with the same structure but different type constraints. These patterns create ambiguous dispatch behavior and should be flagged as errors at compile time.

## Background

During task #335, we discovered a design question: what should happen when a typed route's conversion fails? Options were:
1. Skip to next route (allows typed→untyped fallback)
2. Emit error (clear feedback)
3. Complex hybrid (track best failed match)

After analyzing real CLIs (git, docker, kubectl, npm), we found that **no real CLI uses type-based dispatch fallback**. Real CLIs use:
- Explicit subcommands (`get-by-id` vs `get-by-name`)
- Single route with smart parsing in handler
- Flags for disambiguation (`--id` vs `--name`)

**Decision:** The analyzer should prevent ambiguous patterns at compile time, making the runtime behavior simple (type conversion failure = error).

## Patterns to Detect

### Pattern 1: Same structure, different types (ERROR)
```csharp
.Map("get {id:int}").WithHandler((int id) => ...)
.Map("get {id:guid}").WithHandler((Guid id) => ...)
```
**Diagnostic NURU_R001:** "Routes 'get {id:int}' and 'get {id:guid}' have the same structure with different type constraints. The second route will never be reached on type conversion failure. Use explicit subcommands or flags instead."

### Pattern 2: Typed with untyped fallback (ERROR)
```csharp
.Map("delay {ms:int}").WithHandler((int ms) => ...)
.Map("delay {duration}").WithHandler((string duration) => ...)
```
**Diagnostic NURU_R001:** "Routes 'delay {ms:int}' and 'delay {duration}' have the same structure. The untyped route will never be reached when the typed route fails. Use explicit subcommands or flags instead."

### Pattern 3: Exact duplicates (ERROR - may already exist)
```csharp
.Map("deploy {env}").WithHandler((string env) => ...)
.Map("deploy {environment}").WithHandler((string environment) => ...)
```
**Diagnostic:** "Duplicate route pattern. Both routes match identical input."

## Checklist

- [ ] Define NURU_R001 diagnostic descriptor in `diagnostic-descriptors.*.cs`
- [ ] Add to `AnalyzerReleases.Unshipped.md`
- [ ] Implement detection logic (compare route structures ignoring parameter names/types)
- [ ] Emit error when same-structure routes have different type constraints
- [ ] Emit error when typed route has untyped "fallback" with same structure
- [ ] Add unit tests for the analyzer
- [ ] Update `routing-07-route-selection.cs` test that uses typed+untyped pattern
  - Test should trigger the analyzer
  - Validate analyzer catches it
  - Then remove or modify the conflicting routes

## Notes

- Related to V2 Generator epic (#265)
- Discovered during #335 discussion about type conversion failure behavior
- Runtime behavior will be: type conversion failure = clear error message + exit code 1
- No fallback to next route - the analyzer prevents patterns that would rely on this

## Files to Modify

**Analyzer:**
- `source/timewarp-nuru-analyzers/diagnostics/diagnostic-descriptors.route.cs` (new file or add to existing)
- `source/timewarp-nuru-analyzers/analyzers/` (new analyzer or extend existing)
- `source/timewarp-nuru-analyzers/AnalyzerReleases.Unshipped.md`

**Tests:**
- `tests/timewarp-nuru-analyzers-tests/` (new analyzer tests)
- `tests/timewarp-nuru-core-tests/routing/routing-07-route-selection.cs` (update after analyzer works)
