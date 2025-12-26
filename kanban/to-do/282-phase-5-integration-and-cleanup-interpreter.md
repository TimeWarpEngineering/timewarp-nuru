# Phase 5: Integration and Cleanup (Interpreter)

## Description

Integrate the new DSL interpreter into the main generator pipeline, replacing the old `FluentChainExtractor`. Clean up obsolete code and ensure all existing tests pass.

## Parent

#277 Epic: Semantic DSL Interpreter with Mirrored IR Builders

## Depends On

- #278 Phase 1: POC - Minimal Fluent Case
- #279 Phase 2: Add Group Support
- #280 Phase 3: Handle Fragmented Styles
- #281 Phase 4: Additional DSL Methods

## Checklist

### 5.1 Update `AppExtractor`

- [ ] Replace `FluentChainExtractor.ExtractToBuilder()` call with `DslInterpreter.Interpret()`
- [ ] Update `Extract()` method signature if needed
- [ ] Find `CreateBuilder()` call using `CreateBuilderLocator`
- [ ] Create `DslInterpreter` instance
- [ ] Call `Interpret()` and return the `AppModel`

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

- [ ] `generators/extractors/fluent-chain-extractor.cs`
- [ ] `generators/extractors/builders/app-model-builder.cs`
- [ ] `generators/extractors/builders/route-definition-builder.cs`
- [ ] `generators/extractors/builders/handler-definition-builder.cs`

Or move to `_obsolete/` folder for reference if preferred.

### 5.3 Keep Required Extractors

Ensure these are still used by the interpreter:

- [ ] `generators/extractors/handler-extractor.cs` - used for `WithHandler()`
- [ ] `generators/extractors/pattern-string-extractor.cs` - used for `Map()`
- [ ] `generators/extractors/intercept-site-extractor.cs` - used for `RunAsync()`
- [ ] `generators/extractors/service-extractor.cs` - used for `ConfigureServices()`

### 5.4 Update Locators if Needed

- [ ] Ensure `CreateBuilderLocator` can find `CreateBuilder()` from a `Build()` call
- [ ] Or update `AppExtractor` to find `CreateBuilder()` directly

### 5.5 Run Existing V2 Generator Tests

- [ ] Run `tests/timewarp-nuru-core-tests/routing/temp-minimal-intercept-test.cs`
- [ ] All 17 tests should pass
- [ ] Specifically verify nested group tests now pass:
  - `Should_match_grouped_route`
  - `Should_match_nested_group_route`
  - `Should_match_outer_group_route_after_nested_group`

### 5.6 Run Full Test Suite

- [ ] `dotnet test` on entire solution
- [ ] No regressions in analyzer tests
- [ ] No regressions in core tests
- [ ] No regressions in REPL tests

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
│   ├── ir-app-builder.cs
│   ├── ir-route-builder.cs
│   ├── ir-group-builder.cs
│   └── ir-group-route-builder.cs
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
