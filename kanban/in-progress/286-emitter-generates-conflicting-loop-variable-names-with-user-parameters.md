# Emitter generates conflicting loop variable names with user parameters

## Description

The code emitter generates option extraction loops using `i` as the loop index variable. When a user's route pattern has a parameter named `i` (e.g., `-i {i:int}`), this causes a CS0136 compiler error because the names conflict.

## Symptom

```csharp
// Generated code with conflict:
string? i = string.Empty;           // User's parameter
for (int i = 0; i < args.Length - 1; i++)  // CS0136 - 'i' already declared
```

Error:
```
CS0136: A local or parameter named 'i' cannot be declared in this scope because that name is used in an enclosing local scope
```

## Root Cause

The emitter uses `i` as the loop index variable without checking if it conflicts with user parameter names.

## Proposed Fix

Use unique loop index names that won't conflict with user parameters:
- Option 1: Use `__idx_0`, `__idx_1`, etc.
- Option 2: Use `__i_0`, `__i_1`, etc.
- Option 3: Prefix with double underscore: `__i`

## Checklist

- [ ] Locate the emitter code that generates option extraction loops
- [ ] Change loop variable from `i` to a unique name (e.g., `__idx`)
- [ ] Add test case with parameter named `i`
- [ ] Verify bench-nuru-full compiles after fix

## Discovered In

`benchmarks/aot-benchmarks/bench-nuru-full/Program.cs` with pattern `--str {str} -i {i:int} -b`

## Files to Investigate

- `source/timewarp-nuru-analyzers/generators/emitters/` - likely location of emitter code
