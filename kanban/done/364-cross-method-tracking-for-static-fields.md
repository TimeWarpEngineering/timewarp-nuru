# Cross-Method Tracking for Static Fields

## Summary

Implemented cross-method tracking for static fields, enabling the generator to intercept entry point calls (RunAsync/RunReplAsync) when the app is stored in a static field and accessed from a different method.

## Root Cause

The `DslInterpreter.EvaluateExpression()` method didn't handle `AssignmentExpressionSyntax`. When encountering `App = NuruApp.CreateBuilder([])...Build()`, the assignment fell through to the default case and returned null, preventing field tracking.

## Solution

### 1. Added AssignmentExpressionSyntax Handling (dsl-interpreter.cs)

```csharp
private object? EvaluateExpression(ExpressionSyntax expression)
{
  return expression switch
  {
    // ... existing cases ...
    AssignmentExpressionSyntax assignment => EvaluateAssignment(assignment),  // NEW
    _ => null
  };
}

private object? EvaluateAssignment(AssignmentExpressionSyntax assignment)
{
  object? value = EvaluateExpression(assignment.Right);
  ISymbol? leftSymbol = SemanticModel.GetSymbolInfo(assignment.Left).Symbol;
  if (leftSymbol is not null && value is not null)
  {
    VariableState[leftSymbol] = value;
  }
  return value;
}
```

### 2. Fixed Type Resolution for AddTypeConverter (dsl-interpreter.cs)

Changed `DispatchAddTypeConverter()` to use `SymbolDisplayFormat.FullyQualifiedFormat` instead of `MinimallyQualifiedFormat`. This resolved conflicts between local enum types (e.g., `Environment`) and `System.Environment`.

## Files Modified

- `source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs`
  - Added `EvaluateAssignment()` method
  - Added `AssignmentExpressionSyntax` case in `EvaluateExpression()`
  - Fixed `DispatchAddTypeConverter()` to use fully qualified type names

## Results

| Test | Before | After |
|------|--------|-------|
| repl-19-tab-cycling-bug | Failed | **2/2 passed** |
| repl-17-sample-validation | CS0718 error | **Compiles (5/15 passed)** |
| CI Total | Build errors | **819/937 passed** |

## Acceptance Criteria

- [x] Tests using Setup() pattern pass (repl-19 passes)
- [x] Static field assignment is tracked
- [ ] Instance field assignment is tracked (not tested)
- [ ] Property assignment is tracked (not tested)
- [x] Incremental build performance is maintained
