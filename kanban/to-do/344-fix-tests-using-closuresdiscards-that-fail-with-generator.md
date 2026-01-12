# Fix tests using closures/discards that fail with generator

## Description

Several test files use patterns that are incompatible with the Nuru source generator:
1. **Closures** - Lambdas that capture external variables (NURU_H002)
2. **Discards** - Parameters named `_` (NURU_H006)

These tests were written before the generator was in place and need to be refactored
to use the `TestTerminal` pattern instead.

## Errors

```
error NURU_H006: Handler lambda uses discard parameter '_'. Discards are not supported 
because the lambda body is inlined into generated code. Use named parameters instead.

error CS0103: The name 'capturedEnv' does not exist in the current context
```

## Affected Files

- `tests/timewarp-nuru-core-tests/options/options-01-mixed-required-optional.cs`
  - Uses `(string _, string? _, bool _)` discard pattern
  - Uses `capturedEnv`, `capturedVer`, `capturedDryRun` closures

- `tests/timewarp-nuru-core-tests/routing/routing-23-multiple-map-same-handler.cs`
  - Uses `executionCount++` closure
  - Uses `capturedName` closure

- Other test files likely affected (need full audit)

## Checklist

- [ ] Audit all test files for closure/discard usage
- [ ] Refactor to use `TestTerminal` pattern (see #332 for examples)
- [ ] Replace discards with named parameters
- [ ] Verify all tests compile and pass

## Notes

### Pattern to avoid (closure)
```csharp
string? captured = null;
.WithHandler((string name) => { captured = name; })
```

### Pattern to use (TestTerminal)
```csharp
using TestTerminal terminal = new();
.UseTerminal(terminal)
.WithHandler((string name) => name)  // Return value goes to terminal
// Assert: terminal.OutputContains("expected")
```

### Related
- #332 - Promote NURU_H002 to Error (completed, has refactoring examples)
- #307 - NURU_H002 false positive on object initializers (separate issue)
