# Bug: Generator complex routes skip type conversion

## Description

Routes with options (or catch-all) use `EmitComplexMatch()` which does NOT call `EmitTypeConversions()`. This means typed parameters are declared as `string` instead of being parsed to their correct type.

**User code:**
```csharp
.Map("round {value:double} --mode {mode}")
  .WithHandler((double value, string mode) =>
  {
    double result = mode.ToLower() switch
    {
      "up" => Math.Ceiling(value),
      "down" => Math.Floor(value),
      _ => Math.Round(value)
    };
  })
```

**Generated code (broken):**
```csharp
// Route: round {value:double} --mode {mode}
if (args.Length >= 2)
{
  if (args[0] != "round") goto route_skip_4;
  string value = args[1];  // BUG: Should be double!
  string? mode = string.Empty;
  // option parsing...
  void __handler_4()
  {
    double result = mode.ToLower() switch
    {
      "up" => Math.Ceiling(value),    // CS1503: cannot convert 'string' to 'double'
      // ...
    };
  }
}
```

**Error:**
```
error CS1503: Argument 1: cannot convert from 'string' to 'double'
```

## Root Cause

In `route-matcher-emitter.cs`:

- `EmitSimpleMatch()` (line 44) calls `EmitTypeConversions()` ✓
- `EmitComplexMatch()` (line 68) does NOT call `EmitTypeConversions()` ✗

Additionally, `EmitParameterExtraction()` (called by `EmitComplexMatch`) always extracts parameters as `string`, regardless of type constraint.

## Solution

1. Modify `EmitParameterExtraction()` signature to accept `routeIndex`
2. For typed parameters, extract to unique variable name (`__value_0`) instead of final name
3. Add call to `EmitTypeConversions()` in `EmitComplexMatch()` after parameter extraction

**Current `EmitComplexMatch` flow:**
```csharp
EmitParameterExtraction(sb, route, literalIndex);  // string value = args[1];
EmitOptionParsing(sb, route);
HandlerInvokerEmitter.Emit(...);
```

**Fixed flow:**
```csharp
EmitParameterExtraction(sb, route, routeIndex, literalIndex);  // string __value_0 = args[1];
EmitTypeConversions(sb, route, routeIndex, indent: 6);         // double value = double.Parse(__value_0, ...);
EmitOptionParsing(sb, route);
HandlerInvokerEmitter.Emit(...);
```

## Relevant Source File

`source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs`

## Test Case

**File:** `tests/timewarp-nuru-core-tests/generator/generator-08-typed-params-with-options.cs`

**Also verify:** `samples/02-calculator/01-calc-delegate.cs` compiles and runs

## Checklist

- [x] Update `EmitParameterExtraction()` signature to include `routeIndex`
- [x] Update call site in `EmitComplexMatch()` to pass `routeIndex`
- [x] Modify `EmitParameterExtraction()` to use unique var names for typed params
- [x] Add `EmitTypeConversions()` call in `EmitComplexMatch()` after parameter extraction
- [x] Create test file `generator-08-typed-params-with-options.cs`
- [x] Verify `samples/02-calculator/01-calc-delegate.cs` works

## Results

**Fixed in:** `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs`

**Changes:**
1. Updated `EmitParameterExtraction()` signature to accept `routeIndex`
2. For typed params, extract to unique var name (`__value_{routeIndex}`)
3. Added `EmitTypeConversions()` call in `EmitComplexMatch()` after parameter extraction

**Generated code (after fix):**
```csharp
// Route: round {value:double} --mode {mode}
if (args.Length >= 2)
{
  if (args[0] != "round") goto route_skip_4;
  string __value_4 = args[1];
  double value = double.Parse(__value_4, System.Globalization.CultureInfo.InvariantCulture);
  string? mode = string.Empty;
  // option parsing...
}
```

**Tests:**
- `generator-08-typed-params-with-options.cs` - passes
- `samples/02-calculator/01-calc-delegate.cs` - works
  - `round 3.7 --mode up` → "Round(3.7, up) = 4"
  - `add 2.5 3.5` → "2.5 + 3.5 = 6"
- All existing generator tests still pass (17/17)

## Notes

This was blocking the calculator sample which uses `round {value:double} --mode {mode}`.

Note: Typed OPTIONS (e.g., `--factor {factor:double}`) are not yet supported - they still need to be parsed manually in the handler. This could be a separate enhancement.
