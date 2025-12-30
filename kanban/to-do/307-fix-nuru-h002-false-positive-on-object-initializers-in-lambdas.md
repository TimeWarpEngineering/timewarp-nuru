# Fix NURU_H002 false positive on object initializers in lambdas

## Summary

The `NuruHandlerAnalyzer` incorrectly flags properties in object initializers as "captured variables", producing a false positive NURU_H002 error.

## Reproduction

```csharp
.WithHandler((double x, double y) => new ComparisonResult
{
  X = x,
  Y = y,
  IsEqual = x == y,
  Difference = x - y,
  Ratio = y != 0 ? x / y : double.NaN
})
```

**Error:**
```
error NURU_H002: Handler lambda captures external variable(s): this.X, this.Y, this.IsEqual, this.Difference, this.Ratio
```

## Root Cause

In `source/timewarp-nuru-analyzers/analyzers/nuru-handler-analyzer.cs` lines 244-247:

```csharp
case IPropertySymbol prop when !prop.IsStatic:
  // Instance property access (via implicit 'this') - closure!
  capturedVariables.Add($"this.{name}");
  break;
```

The analyzer treats any non-static `IPropertySymbol` as a closure capture. But in an object initializer, `X = x` is *setting* a property on the **new object being created**, not *accessing* a property from the enclosing scope via `this`.

## Fix

The `DetectClosures` method needs to:
1. Detect when an identifier is on the LEFT side of an assignment in an `ObjectInitializerExpressionSyntax`
2. Skip those identifiers - they're property setters on the new object, not captured variables

## Checklist

- [ ] Add detection for `ObjectInitializerExpressionSyntax` context
- [ ] Skip property symbols that are targets of initialization assignments
- [ ] Add unit test for object initializer in lambda handler
- [ ] Verify `03-calc-mixed.cs` compiles without suppression

## Notes

Discovered during Task #304 Phase 10 while converting `03-calc-mixed.cs` to use attributed routes.
