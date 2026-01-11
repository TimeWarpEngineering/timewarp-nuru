# Add analyzer for ambiguous route patterns with overlapping type constraints

## Summary

Add a new analyzer diagnostic (NURU_R001) that detects route patterns with the same structure but different type constraints. This is part of a larger refactoring to unify analyzer architecture and move all validation to operate on the IR model.

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
**Diagnostic NURU_R001:** "Routes 'get {id:int}' and 'get {id:guid}' have the same structure with different type constraints. Type conversion failures produce errors, not fallback to other routes. Use explicit subcommands or flags instead."

### Pattern 2: Typed with untyped fallback (ERROR)
```csharp
.Map("delay {ms:int}").WithHandler((int ms) => ...)
.Map("delay {duration}").WithHandler((string duration) => ...)
```
**Diagnostic NURU_R001:** "Routes 'delay {ms:int}' and 'delay {duration}' have the same structure. The untyped route will never be reached when the typed route fails. Use explicit subcommands or flags instead."

### Pattern 3: Exact duplicates (ERROR)
```csharp
.Map("deploy {env}").WithHandler((string env) => ...)
.Map("deploy {environment}").WithHandler((string environment) => ...)
```
**Diagnostic NURU_R001:** "Duplicate route pattern. Both routes match identical input."

---

## Architecture Decision: Unified Model-Based Validation

### Current State (Problems)

1. **`NuruRouteAnalyzer`** (IIncrementalGenerator):
   - Finds individual `Map()` calls
   - Uses `PatternParser.TryParse()` directly (lightweight)
   - Reports NURU_P### and NURU_S### errors
   - Does NOT build full IR model
   - Cannot do cross-route validation

2. **`NuruHandlerAnalyzer`** (DiagnosticAnalyzer):
   - Validates handler expressions in `.WithHandler()` calls
   - Reports NURU_H### errors
   - Does NOT build full IR model

3. **`NuruGenerator`** (IIncrementalGenerator):
   - Builds full `AppModel` via `AppExtractor` and `DslInterpreter`
   - Does code generation
   - No diagnostic reporting - assumes valid input
   - Throws exceptions on errors (not ideal for agents)

### Target State (Solution)

1. **Unified `NuruAnalyzer`** (IIncrementalGenerator):
   - Builds full `AppModel` using same code as generator (`AppExtractor`)
   - ALL validation happens here on the model
   - Reports ALL diagnostics (P###, S###, H###, R###)
   - Single pass, more performant

2. **`NuruGenerator`** (IIncrementalGenerator):
   - Builds same `AppModel` (code is shared, but runs separately - analyzers can't pass data to generators)
   - Assumes valid model (analyzer already validated)
   - Pure code emission, no validation logic
   - Simpler and cleaner

### Why Unified Analyzer?

- **Agents benefit**: Structured diagnostics, all errors at once, fewer round trips
- **Humans benefit**: All errors in IDE Error List at once
- **Performance**: Single model build, single validation pass
- **Model validation**: Once we have `AppModel`, we can validate everything including cross-route concerns
- **Both DSLs covered**: Fluent and attributed routes both end up in `AppModel`

### Two Validation Layers

1. **Pre-model validation** (extraction phase):
   - Things that prevent building the model at all
   - Unknown DSL method, can't resolve symbols, malformed syntax
   - Must be collected as errors during extraction (not thrown as exceptions)

2. **Model validation** (post-extraction):
   - Parse errors (NURU_P###)
   - Semantic errors (NURU_S###)
   - Handler errors (NURU_H###)
   - Overlap errors (NURU_R###) ← NEW

---

## Implementation Plan

### Phase 1: Refactor Extraction to Collect Errors

**Goal**: Change `DslInterpreter` from throwing exceptions to collecting errors.

**Changes**:
- Add `ExtractionResult` record: `record ExtractionResult(AppModel? Model, ImmutableArray<Diagnostic> Diagnostics)`
- Modify `DslInterpreter` to collect errors instead of throwing
- Modify `AppExtractor` to return `ExtractionResult`
- Route-level parse errors should also be collected during extraction

**Files**:
- `source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs`
- `source/timewarp-nuru-analyzers/generators/extractors/app-extractor.cs`
- Create `source/timewarp-nuru-analyzers/generators/models/extraction-result.cs`

### Phase 2: Create Unified Analyzer

**Goal**: Merge `NuruRouteAnalyzer` and `NuruHandlerAnalyzer` into single `NuruAnalyzer`.

**Changes**:
- Create new `NuruAnalyzer` that:
  - Uses `AppExtractor` to build full model
  - Runs all validators on the model
  - Reports all diagnostics
- Create `validation/` folder with validators:
  - `ModelValidator.cs` - orchestrates all validation
  - `OverlapValidator.cs` - detects overlapping routes
- Deprecate/remove old analyzers

**Files**:
- Create `source/timewarp-nuru-analyzers/analyzers/nuru-analyzer.cs`
- Create `source/timewarp-nuru-analyzers/validation/model-validator.cs`
- Create `source/timewarp-nuru-analyzers/validation/overlap-validator.cs`
- Remove `source/timewarp-nuru-analyzers/analyzers/nuru-route-analyzer.cs`
- Remove `source/timewarp-nuru-analyzers/analyzers/nuru-handler-analyzer.cs`

### Phase 3: Implement Overlap Detection

**Goal**: Detect overlapping route patterns with NURU_R001.

**Structure Signature Algorithm**:
Normalize routes to structure signature (ignoring parameter names and types):
```
"get {id:int}"      → "get {P}"
"get {name:string}" → "get {P}"
"get {x}"           → "get {P}"
"get {id:int?}"     → "get {P?}"
"get {*args}"       → "get {*}"
```

Where:
- `{P}` = required parameter (any type)
- `{P?}` = optional parameter (any type)
- `{*}` = catch-all parameter
- Options normalized by name, not by parameter type

Two routes with same signature but different type constraints → NURU_R001

**Files**:
- `source/timewarp-nuru-analyzers/validation/overlap-validator.cs`
- `source/timewarp-nuru-analyzers/diagnostics/diagnostic-descriptors.overlap.cs` (already created)

### Phase 4: Simplify Generator

**Goal**: Generator assumes valid model, no validation logic.

**Changes**:
- Remove any defensive checks that are now redundant
- Generator focuses purely on code emission
- Trust that analyzer has validated the model

**Files**:
- `source/timewarp-nuru-analyzers/generators/nuru-generator.cs`
- Various emitter files if they have validation logic

---

## Checklist

### Phase 1: Extraction Error Collection
- [x] Create `ExtractionResult` record type
- [x] Modify `DslInterpreter` to collect errors instead of throwing
- [x] Modify `AppExtractor` to return `ExtractionResult`
- [ ] Ensure route parse errors are collected during extraction

### Phase 2: Unified Analyzer
- [x] Create `NuruAnalyzer` using `AppExtractor`
- [x] Create `validation/ModelValidator.cs` to orchestrate validation
- [ ] Move handler validation logic to model validator
- [ ] Move route pattern validation logic to model validator
- [ ] Remove `NuruRouteAnalyzer`
- [ ] Remove `NuruHandlerAnalyzer`
- [ ] Update any tests that reference old analyzers

### Phase 3: Overlap Detection
- [x] Implement route structure signature generation
- [x] Implement `OverlapValidator` to detect conflicts
- [x] Report NURU_R001 for overlapping routes
- [ ] Add unit tests for overlap detection
- [x] Update `routing-07-route-selection.cs` to not have conflicts

### Phase 4: Generator Cleanup
- [ ] Remove validation logic from generator
- [ ] Remove defensive checks that are now redundant
- [ ] Verify generator still works with valid input

### Final
- [ ] All existing tests pass
- [ ] New tests for overlap detection pass
- [ ] Documentation updated if needed

---

## Files Summary

**Create**:
- `source/timewarp-nuru-analyzers/analyzers/nuru-analyzer.cs`
- `source/timewarp-nuru-analyzers/validation/model-validator.cs`
- `source/timewarp-nuru-analyzers/validation/overlap-validator.cs`
- `source/timewarp-nuru-analyzers/generators/models/extraction-result.cs`
- `source/timewarp-nuru-analyzers/diagnostics/diagnostic-descriptors.overlap.cs` ✅ (already created)

**Modify**:
- `source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs`
- `source/timewarp-nuru-analyzers/generators/extractors/app-extractor.cs`
- `source/timewarp-nuru-analyzers/generators/nuru-generator.cs`
- `source/timewarp-nuru-analyzers/AnalyzerReleases.Unshipped.md` ✅ (already updated)
- `tests/timewarp-nuru-core-tests/routing/routing-07-route-selection.cs`

**Remove**:
- `source/timewarp-nuru-analyzers/analyzers/nuru-route-analyzer.cs`
- `source/timewarp-nuru-analyzers/analyzers/nuru-handler-analyzer.cs`

---

## Notes

- Related to V2 Generator epic (#265)
- Discovered during #335 discussion about type conversion failure behavior
- Runtime behavior: type conversion failure = clear error message + exit code 1
- No fallback to next route - the analyzer prevents patterns that would rely on this
- Both analyzer and generator build the model independently (can't pass data between them)
- Shared code for model building ensures consistency
