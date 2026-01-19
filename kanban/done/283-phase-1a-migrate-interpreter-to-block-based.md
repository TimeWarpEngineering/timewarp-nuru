# Phase 1a: Migrate Interpreter to Block-Based Processing

## Description

Refactor DslInterpreter to use `Interpret(BlockSyntax)` instead of `Interpret(InvocationExpressionSyntax)`. This is a prerequisite for supporting fragmented code styles and multiple apps per block.

## Parent

#277 Epic: Semantic DSL Interpreter with Mirrored IR Builders

## Depends On

- #278 Phase 1: POC - Minimal Fluent Case ✅ (completed)
- #279 Phase 2: Add Group Support with CRTP ✅ (completed)

## Scope

- Change signature from `Interpret(InvocationExpressionSyntax)` to `Interpret(BlockSyntax)`
- Return `IReadOnlyList<AppModel>` (each app identifiable by InterceptSites locations)
- Add `VariableName` field to `AppModel` for debugging/identification
- Add `VariableState` dictionary with `SymbolEqualityComparer.Default`
- Implement statement-by-statement processing
- Remove `FindFluentChainRoot()`, `EvaluateFluentChain()`, `UnrollFluentChain()`
- Update all 4 Phase 1 tests in `dsl-interpreter-test.cs` to use new API

## Checklist

### Model Changes
- [x] Add `VariableName` field to `AppModel`
- [x] Add `VariableName` property to `IrAppBuilder`, update `FinalizeModel()`

### Interpreter Signature Change
- [x] Change `Interpret()` signature to take `BlockSyntax`, return `IReadOnlyList<AppModel>`
- [x] Add `VariableState` dictionary (fresh per call)
- [x] Add tracking list for built apps

### Statement Processing
- [x] Implement `ProcessBlock()` - iterate statements
- [x] Implement `ProcessStatement()` - switch on statement type
- [x] Implement `ProcessLocalDeclaration()` - handle `var x = ...`
- [x] Implement `ProcessExpressionStatement()` - handle standalone expressions

### Expression Evaluation
- [x] Implement `EvaluateExpression()` - recursive with variable resolution
- [x] Implement `ResolveIdentifier()` - lookup in VariableState

### Cleanup
- [x] Remove `FindFluentChainRoot()`
- [x] Remove `EvaluateFluentChain()`
- [x] Remove `UnrollFluentChain()`

### Tests
- [x] Update `dsl-interpreter-test.cs` to use `Interpret(BlockSyntax)`
- [x] All 4 Phase 1 tests pass
- [x] Update `dsl-interpreter-group-test.cs` to use `Interpret(BlockSyntax)`
- [x] All 6 group tests pass

## Files to Modify

| File | Change |
|------|--------|
| `generators/models/app-model.cs` | Add `VariableName` field |
| `generators/ir-builders/ir-app-builder.cs` | Add `VariableName` property |
| `generators/ir-builders/abstractions/iir-app-builder.cs` | Add `SetVariableName` interface method |
| `generators/interpreter/dsl-interpreter.cs` | Complete refactor |
| `generators/extractors/builders/app-model-builder.cs` | Add `VariableName: null` |
| `generators/nuru-generator.cs` | Add `VariableName: null` |
| `tests/.../interpreter/dsl-interpreter-test.cs` | Update API usage |
| `tests/.../interpreter/dsl-interpreter-group-test.cs` | Update API usage |

## Success Criteria

1. All 4 Phase 1 tests pass with new block-based API ✅
2. All 6 Phase 2 group tests pass with new block-based API ✅
3. `Interpret(BlockSyntax)` returns list of `AppModel`s ✅
4. Variable tracking works for simple cases ✅
5. Old methods removed (no dead code) ✅

## Results

### Implementation Complete

The DslInterpreter has been successfully migrated from invocation-based to block-based processing:

**Key Changes:**
- `Interpret(BlockSyntax)` now takes a method body and returns `IReadOnlyList<AppModel>`
- Added `VariableState` dictionary using `SymbolEqualityComparer.Default` for symbol-based variable tracking
- Processes statements one by one via `ProcessStatement()` switch dispatch
- Local declarations (`var app = ...`) are evaluated and stored in `VariableState`
- Expression statements (`await app.RunAsync(...)`) are evaluated for side effects
- Variable names are captured for debugging/identification purposes

**Test Results:**
- All 4 Phase 1 basic tests pass
- All 6 Phase 2 group tests pass
- Total: 10/10 tests passing

**Files Modified:**
- `source/timewarp-nuru-analyzers/generators/models/app-model.cs`
- `source/timewarp-nuru-analyzers/generators/ir-builders/ir-app-builder.cs`
- `source/timewarp-nuru-analyzers/generators/ir-builders/abstractions/iir-app-builder.cs`
- `source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs`
- `source/timewarp-nuru-analyzers/generators/extractors/builders/app-model-builder.cs`
- `source/timewarp-nuru-analyzers/generators/nuru-generator.cs`
- `tests/timewarp-nuru-analyzers-tests/interpreter/dsl-interpreter-test.cs`
- `tests/timewarp-nuru-analyzers-tests/interpreter/dsl-interpreter-group-test.cs`
