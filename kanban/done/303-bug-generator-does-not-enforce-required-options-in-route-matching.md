# Bug: Generator does not enforce required options in route matching

## Description

The source generator does not enforce **required options** when matching routes. Routes with required options (no `?` suffix) incorrectly match even when those options are missing from the input.

**User code:**
```csharp
// Route 1: Required --mode option (higher specificity: 180 pts)
.Map("round {value:double} --mode {mode}")
  .WithHandler((double value, string mode) => ...)

// Route 2: No options (lower specificity: 120 pts)
.Map("round {value:double}")
  .WithHandler((double value) => ...)
```

**Expected behavior:**
- `round 2.5` → matches Route 2 (Route 1 requires `--mode`, which is missing)
- `round 2.5 --mode up` → matches Route 1

**Actual behavior:**
- `round 2.5` → matches Route 1 with `mode = ""` (empty string)
- Route 2 is never reached

**Generated code (broken):**
```csharp
// Route: round {value:double} --mode {mode}
if (args.Length >= 2)
{
  if (args[0] != "round") goto route_skip_4;
  string __value_4 = args[1];
  double value = double.Parse(__value_4, ...);
  string? mode = string.Empty;  // Default for "required" option
  for (int __idx = 0; __idx < args.Length - 1; __idx++)
  {
    if (args[__idx] == "--mode")
    {
      mode = args[__idx + 1];
      break;
    }
  }
  // BUG: No check that --mode was actually found!
  // Proceeds with mode = "" even though --mode is required
  __handler_4();
  return 0;
}
```

## Root Cause

In `EmitValueOptionParsing()` (route-matcher-emitter.cs, line 299):

```csharp
string defaultValue = option.IsOptional ? "null" : "string.Empty";
```

For required options (`IsOptional = false`), the code:
1. Defaults to `string.Empty`
2. Searches for the option in args
3. **Never checks** if the option was actually found
4. Proceeds with the route regardless

According to the specificity algorithm documentation, required options should **only match when present**.

## Solution

After the option search loop, add a check for required options:

**Current code:**
```csharp
sb.AppendLine("      }");  // End of for loop
```

**Fixed code:**
```csharp
sb.AppendLine("      }");  // End of for loop

// For required options, skip route if not found
if (!option.IsOptional)
{
  sb.AppendLine(CultureInfo.InvariantCulture,
    $"      if ({varName} == string.Empty) goto route_skip_{routeIndex};");
}
```

This requires passing `routeIndex` to `EmitValueOptionParsing()`.

## Relevant Source File

`source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs`

## Test Case

**File:** `tests/timewarp-nuru-core-tests/generator/generator-10-required-options.cs`

Test both:
- `round 2.5` → should match `round {value:double}` (no --mode), NOT the route with required --mode
- `round 2.5 --mode up` → should match `round {value:double} --mode {mode}`

## Checklist

- [ ] Update `EmitValueOptionParsing()` signature to accept `routeIndex`
- [ ] Update call site in `EmitOptionParsing()` to pass `routeIndex`
- [ ] Add check after option search loop to skip route if required option not found
- [ ] Create test file `generator-10-required-options.cs`
- [ ] Verify `samples/02-calculator/01-calc-delegate.cs` works correctly:
  - `round 2.5` → "Round(2.5) = 2" (default rounding)
  - `round 2.5 --mode up` → "Round(2.5, up) = 3"

## Blocking

This blocks proper behavior of `samples/02-calculator/01-calc-delegate.cs` for the `round` command without `--mode`.

## References

- Specificity algorithm: `documentation/developer/design/resolver/specificity-algorithm.md`
- Lines 10-11: "Required options (`--flag`) = 50 points each" / "Optional options (`--flag?`) = 25 points each"
- Line 216: "Specificity Wins: More specific routes always match before general ones"
