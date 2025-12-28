# Phase 5: Integration and Cleanup (Interpreter)

## Description

Integrate the new DSL interpreter into the main generator pipeline, replacing the old `FluentChainExtractor`. Clean up obsolete code and ensure all existing tests pass.

## Parent

#277 Epic: Semantic DSL Interpreter with Mirrored IR Builders

## Depends On

- #278 Phase 1: POC - Minimal Fluent Case ✅
- #279 Phase 2: Add Group Support ✅
- #280 Phase 3: Handle Fragmented Styles ✅
- #281 Phase 4: Additional DSL Methods ✅

## Checklist

### 5.1 Update `AppExtractor`

- [x] Replace `FluentChainExtractor.ExtractToBuilder()` call with `DslInterpreter.Interpret()`
- [x] Update `Extract()` method signature if needed
- [x] Find `CreateBuilder()` call using `CreateBuilderLocator`
- [x] Create `DslInterpreter` instance
- [x] Call `Interpret()` and return the `AppModel`

```csharp
public static AppModel? Extract(
  InvocationExpressionSyntax buildCall,
  SemanticModel semanticModel,
  CancellationToken cancellationToken)
{
  // Find the CreateBuilder() call for this Build()
  InvocationExpressionSyntax? createBuilderCall = 
    CreateBuilderLocator.FindFromBuild(buildCall, semanticModel, cancellationToken);
  
  if (createBuilderCall is null)
    return null;
  
  // Interpret the DSL
  DslInterpreter interpreter = new(semanticModel, cancellationToken);
  return interpreter.Interpret(createBuilderCall);
}
```

### 5.2 Remove Old Extraction Code

Delete obsolete files:

- [x] `generators/extractors/fluent-chain-extractor.cs`
- [x] `generators/extractors/builders/app-model-builder.cs`
- [x] `generators/extractors/builders/route-definition-builder.cs` (kept - still used by IrRouteBuilder)
- [x] `generators/extractors/builders/handler-definition-builder.cs`

Moved old code to `reference-only/` folder and excluded from compilation.

### 5.3 Keep Required Extractors

Ensure these are still used by the interpreter:

- [x] `generators/extractors/handler-extractor.cs` - used for `WithHandler()`
- [x] `generators/extractors/pattern-string-extractor.cs` - used for `Map()`
- [x] `generators/extractors/intercept-site-extractor.cs` - used for `RunAsync()`
- [x] `generators/extractors/service-extractor.cs` - used for `ConfigureServices()`

### 5.4 Update Locators if Needed

- [x] Ensure `CreateBuilderLocator` can find `CreateBuilder()` from a `Build()` call
- [x] Or update `AppExtractor` to find `CreateBuilder()` directly

### 5.5 Run Existing V2 Generator Tests

- [x] Run `tests/timewarp-nuru-core-tests/routing/temp-minimal-intercept-test.cs`
- [x] All 17 tests should pass (now 20 with group tests)
- [x] Specifically verify nested group tests now pass:
  - `Should_match_grouped_route`
  - `Should_match_nested_group_route`
  - `Should_match_outer_group_route_after_nested_group`

### 5.6 Run Full Test Suite

- [ ] `dotnet test` on entire solution (REPL project currently excluded - known issue)
- [x] No regressions in analyzer tests (23 interpreter tests pass)
- [x] No regressions in core tests (20 minimal intercept tests pass)
- [ ] No regressions in REPL tests (REPL not building - separate issue)

### 5.7 Clean Up Temporary Test Files

- [ ] Rename `temp-interpreter-*.cs` test files to permanent names
- [ ] Or merge into existing test files
- [ ] Remove `temp-` prefix once stable

### 5.8 Update Documentation

- [ ] Update any architecture docs that reference `FluentChainExtractor`
- [ ] Add documentation for the interpreter approach
- [ ] Update `.agent/workspace/` design docs if applicable

### 5.9 Final Verification

- [ ] Build entire solution without warnings
- [ ] Run `ganda runfile cache --clear` and test runfiles
- [ ] Verify generated code is correct for:
  - Simple routes
  - Routes with parameters
  - Routes with options
  - Grouped routes
  - Nested grouped routes

## Files to Delete

| File | Reason |
|------|--------|
| `extractors/fluent-chain-extractor.cs` | Replaced by `DslInterpreter` |
| `extractors/builders/app-model-builder.cs` | Replaced by `IrAppBuilder` |
| `extractors/builders/route-definition-builder.cs` | Replaced by `IrRouteBuilder` |
| `extractors/builders/handler-definition-builder.cs` | Check if still needed |

## Files to Modify

| File | Change |
|------|--------|
| `generators/extractors/app-extractor.cs` | Use `DslInterpreter` instead of `FluentChainExtractor` |
| `generators/locators/create-builder-locator.cs` | Possibly add `FindFromBuild()` helper |

## Final File Structure

```
source/timewarp-nuru-analyzers/generators/
├── ir-builders/
│   ├── abstractions/
│   │   ├── iir-app-builder.cs
│   │   ├── iir-group-builder.cs
│   │   ├── iir-route-builder.cs
│   │   └── iir-route-source.cs
│   ├── ir-app-builder.cs
│   ├── ir-route-builder.cs           # Unified - works for both app-level and group-level routes
│   └── ir-group-builder.cs
├── interpreter/
│   └── dsl-interpreter.cs
├── extractors/
│   ├── app-extractor.cs              # Updated
│   ├── handler-extractor.cs          # Keep
│   ├── pattern-string-extractor.cs   # Keep
│   ├── intercept-site-extractor.cs   # Keep
│   └── service-extractor.cs          # Keep
├── locators/                          # Keep all
├── emitters/                          # Unchanged
└── models/                            # Unchanged
```

**Note:** `ir-group-route-builder.cs` no longer exists - Phase 2 unified route builders into `IrRouteBuilder<TParent>` which works for both top-level and group-level routes.

## Success Criteria

1. `AppExtractor` uses `DslInterpreter`
2. Old extraction code is removed
3. All 17+ existing V2 generator tests pass
4. Nested group tests pass (the original problem is solved)
5. Full solution builds without warnings
6. No test regressions
7. Generated code is correct for all scenarios

## Post-Integration Tasks

After this phase is complete:

- [ ] Mark #276 (Implement WithGroupPrefix) as obsolete/superseded
- [ ] Mark #272 nested group checklist items as complete
- [ ] Update #277 epic as complete
- [ ] Consider removing temporary test files

---

## Results (In Progress)

### Commits

| Commit | Description |
|--------|-------------|
| `8faec61e` | feat(generator): Integrate DslInterpreter into AppExtractor (#282) |
| `31af45c7` | feat(runtime): Add WithGroupPrefix for nested route groups (#276) |

### Work Completed

**5.1 AppExtractor Integration (Done)**
- Replaced `FluentChainExtractor.ExtractToBuilder()` with `DslInterpreter.Interpret()`
- Simplified `AppExtractor` from 168 lines to ~98 lines
- Net reduction of ~950 lines of code

**5.2 Old Code Removed (Done)**
- Deleted `fluent-chain-extractor.cs` (305 lines)
- Deleted `app-model-builder.cs` (241 lines)
- Deleted `handler-definition-builder.cs` (325 lines)
- Kept `route-definition-builder.cs` (still used by IrRouteBuilder)
- Excluded `reference-only/` folder from compilation

**5.5 Tests Verified (Done)**
- All 23 interpreter tests pass
- All 20 minimal intercept tests pass (17 original + 3 group tests)
- Nested group tests pass:
  - `Should_match_grouped_route` ✅
  - `Should_match_nested_group_route` ✅
  - `Should_match_outer_group_route_after_nested_group` ✅

**Related: Runtime WithGroupPrefix (#276)**
- Added `GroupBuilder<TParent>` for fluent nested group configuration
- Added `GroupEndpointBuilder<TParent>` for routes within groups
- Added `WithGroupPrefix()` entry point on `NuruCoreAppBuilder`

### Remaining Work

- [ ] 5.6 Full test suite (REPL not building - known separate issue)
- [ ] 5.7 Clean up temporary test files
- [ ] 5.8 Update documentation
- [ ] 5.9 Final verification
- [ ] Post-integration tasks (mark related tasks complete)

---

## Lessons Learned from Previous Phases

### Current Interpreter Architecture (from Phases 1-4)

The interpreter uses block-based processing:

```csharp
public IReadOnlyList<AppModel> Interpret(BlockSyntax block)
```

Key components:
- **`VariableState`**: Dictionary tracking all variable assignments (uses `SymbolEqualityComparer.Default`)
- **`BuiltApps`**: List of `IrAppBuilder` instances that have been built
- **`ProcessBlock()`** → `ProcessStatement()` → `EvaluateExpression()` flow
- **`DispatchMethodCall()`**: Central dispatch switch for all DSL methods

### HandleNonDslMethod Pattern (from Phase 3)

Unknown methods are handled by `HandleNonDslMethod()`:
- If receiver is a builder type (`IIrRouteSource`, `IIrRouteBuilder`, `IIrGroupBuilder`, `IIrAppBuilder`): **throw error** (unknown DSL method - fail fast)
- If receiver is non-builder (e.g., `Console`): **return null** (ignore - not our DSL)

This means new DSL methods **must** be added to the dispatch switch, or they will throw.

### Marker Interfaces for Polymorphic Dispatch

Use the marker interfaces from Phase 2 for type checking:
- `IIrRouteSource` - can create routes/groups
- `IIrAppBuilder` - app-level builder
- `IIrGroupBuilder` - group builder
- `IIrRouteBuilder` - route builder

### IrRouteBuilder Unification (from Phase 2)

There is only `IrRouteBuilder<TParent>` which works for both:
- `IrRouteBuilder<IrAppBuilder>` - top-level routes
- `IrRouteBuilder<IrGroupBuilder<...>>` - routes inside groups

`IrGroupRouteBuilder` no longer exists.

### Test File Naming Convention

Use `dsl-interpreter-*.cs` pattern:
- `dsl-interpreter-test.cs` - Phase 1 tests (4 tests)
- `dsl-interpreter-group-test.cs` - Phase 2 tests (6 tests)
- `dsl-interpreter-fragmented-test.cs` - Phase 3 tests (5 tests)
- `dsl-interpreter-methods-test.cs` - Phase 4 tests (8 tests)

### Current Test Count

| Phase | Tests | Description |
|-------|-------|-------------|
| Phase 1 | 4 | Basic fluent chains |
| Phase 2 | 6 | Groups and nested groups |
| Phase 3 | 5 | Fragmented code styles |
| Phase 4 | 8 | Additional DSL methods |
| **Total** | **23** | All passing |

### Code Locations

| Component | Location |
|-----------|----------|
| IR Builder Interfaces | `generators/ir-builders/abstractions/` |
| IR Builder Classes | `generators/ir-builders/` |
| Interpreter | `generators/interpreter/dsl-interpreter.cs` |
| Tests | `tests/timewarp-nuru-analyzers-tests/interpreter/` |

### Deferred Items from Phase 4

The following items were intentionally deferred to Phase 5+:
- **Options lambda extraction**: `AddHelp(options => ...)` and `AddRepl(options => ...)` currently use defaults only
- **Service extraction**: `ConfigureServices(services => ...)` dispatch added but `ServiceExtractor` not yet integrated

When implementing these, use the existing extractors:
- `ServiceExtractor.ExtractFromLambda(lambda, semanticModel, cancellationToken)` for services
- For options, analyze the lambda body for property assignments

### Key Integration Points for Phase 5

When integrating the interpreter into `AppExtractor`:

1. **Finding the block**: The interpreter needs a `BlockSyntax` containing the DSL code
2. **Entry point**: Use `CreateBuilderLocator` to find the `CreateBuilder()` call
3. **Signature change**: `Interpret(BlockSyntax)` returns `IReadOnlyList<AppModel>` - typically one app per file

### CA1716 Code Analysis

If adding new methods with common names (like `alias`), may need to rename parameters to avoid code analysis warnings. Example: `alias` → `aliasPattern` to fix CA1716.
