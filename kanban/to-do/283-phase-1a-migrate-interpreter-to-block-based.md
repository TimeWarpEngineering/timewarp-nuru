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
- [ ] Add `VariableName` field to `AppModel`
- [ ] Add `VariableName` property to `IrAppBuilder`, update `FinalizeModel()`

### Interpreter Signature Change
- [ ] Change `Interpret()` signature to take `BlockSyntax`, return `IReadOnlyList<AppModel>`
- [ ] Add `VariableState` dictionary (fresh per call)
- [ ] Add tracking list for built apps

### Statement Processing
- [ ] Implement `ProcessBlock()` - iterate statements
- [ ] Implement `ProcessStatement()` - switch on statement type
- [ ] Implement `ProcessLocalDeclaration()` - handle `var x = ...`
- [ ] Implement `ProcessExpressionStatement()` - handle standalone expressions

### Expression Evaluation
- [ ] Implement `EvaluateExpression()` - recursive with variable resolution
- [ ] Implement `ResolveIdentifier()` - lookup in VariableState

### Cleanup
- [ ] Remove `FindFluentChainRoot()`
- [ ] Remove `EvaluateFluentChain()`
- [ ] Remove `UnrollFluentChain()`

### Tests
- [ ] Update `dsl-interpreter-test.cs` to use `Interpret(BlockSyntax)`
- [ ] All 4 Phase 1 tests pass

## Files to Modify

| File | Change |
|------|--------|
| `generators/models/app-model.cs` | Add `VariableName` field |
| `generators/ir-builders/ir-app-builder.cs` | Add `VariableName` property |
| `generators/interpreter/dsl-interpreter.cs` | Complete refactor |
| `tests/.../interpreter/dsl-interpreter-test.cs` | Update API usage |

## Success Criteria

1. All 4 Phase 1 tests pass with new block-based API
2. `Interpret(BlockSyntax)` returns list of `AppModel`s
3. Variable tracking works for simple cases
4. Old methods removed (no dead code)
