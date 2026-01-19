# Bug: return await app.RunAsync(args) not intercepted by source generator

## Description

The source generator fails to intercept `RunAsync()` when it's used with `return`:

```csharp
// WORKS - generator intercepts this
await app.RunAsync(args);

// FAILS - generator does NOT intercept this (throws at runtime)
return await app.RunAsync(args);
```

The runtime throws:
```
System.InvalidOperationException: RunAsync was not intercepted. Ensure the source generator is enabled.
```

## Test Case

**File:** `tests/timewarp-nuru-core-tests/generator/generator-05-return-await.cs`

Run with:
```bash
./tests/timewarp-nuru-core-tests/generator/generator-05-return-await.cs
```

## Root Cause

The `DslInterpreter.ProcessStatement()` method only handled:
- `LocalDeclarationStatementSyntax` (variable declarations)
- `ExpressionStatementSyntax` (plain expression statements)

It explicitly ignored `ReturnStatementSyntax`, so `return await app.RunAsync(args)` was never processed.

## Fix

Added handling for `ReturnStatementSyntax` in `ProcessStatement()` to evaluate the return expression.

**File changed:** `source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs`

```csharp
case ReturnStatementSyntax returnStmt:
  ProcessReturnStatement(returnStmt);
  break;
```

## Checklist

- [x] Add test case for `return await app.RunAsync(args)` pattern
- [x] Debug why `GetInterceptableLocation()` returns null for return statements
- [x] Fix the extractor/locator to handle both patterns
- [x] Verify test passes after fix

## Results

- All generator tests pass (01-05)
- `return await app.RunAsync(args)` pattern now works correctly
