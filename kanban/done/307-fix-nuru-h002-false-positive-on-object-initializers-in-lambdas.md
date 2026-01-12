# Fix NURU_H002 false positive on object initializers in lambdas

## Results

**COMPLETE** - Fixed false positive by detecting object initializer property assignments.

### Solution

Added `IsObjectInitializerTarget()` helper method to `handler-validator.cs` that checks if
an identifier is on the left side of an assignment inside an `ObjectInitializerExpression`.
These are NOT closures - they're setting properties on the newly created object.

### Files Modified

- `source/timewarp-nuru-analyzers/validation/handler-validator.cs` - Added helper and skip check
- `samples/02-calculator/03-calc-mixed.cs` - Removed `#pragma warning disable NURU_H002`

### Verified

```bash
$ dotnet run 03-calc-mixed.cs -- compare 10 5
{"x":10,"y":5,"isEqual":false,"difference":5,"ratio":2}
```

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

In `source/timewarp-nuru-analyzers/validation/handler-validator.cs`:

```csharp
case IPropertySymbol prop when !prop.IsStatic:
  // Instance property access (via implicit 'this') - closure!
  capturedVariables.Add($"this.{name}");
  break;
```

The analyzer treats any non-static `IPropertySymbol` as a closure capture. But in an object initializer, `X = x` is *setting* a property on the **new object being created**, not *accessing* a property from the enclosing scope via `this`.

## Fix

The `DetectClosures` method now:
1. Detects when an identifier is on the LEFT side of an assignment in an `ObjectInitializerExpressionSyntax`
2. Skips those identifiers - they're property setters on the new object, not captured variables

## Checklist

- [x] Add detection for `ObjectInitializerExpressionSyntax` context
- [x] Skip property symbols that are targets of initialization assignments
- [x] Verify `03-calc-mixed.cs` compiles without suppression

## Notes

Discovered during Task #304 Phase 10 while converting `03-calc-mixed.cs` to use attributed routes.
