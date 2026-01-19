# Bug: Generator optional positional params generate wrong pattern

## Description

Routes with optional positional parameters (e.g., `{seconds:int?}` or `{name?}`) generate list patterns that require the optional parameter to be present. The pattern only matches when ALL parameters are provided, not when optional ones are omitted.

**User code:**
```csharp
.Map("sleep {seconds:int?}")
  .WithHandler((int? seconds) =>
  {
    int sleepTime = seconds ?? 1;
    WriteLine($"Sleeping for {sleepTime} seconds");
  })
```

**Generated code (broken):**
```csharp
// Route: sleep {seconds:int?}
if (args is ["sleep", var __seconds_0])  // BUG: Requires exactly 2 args!
{
  int? seconds = int.Parse(__seconds_0, ...);
  // handler...
}
```

**Expected behavior:**
- `sleep` → matches, `seconds = null`
- `sleep 5` → matches, `seconds = 5`

**Actual behavior:**
- `sleep` → does NOT match (pattern requires 2 args)
- `sleep 5` → matches (if #300 is fixed)

## Root Cause

In `BuildListPattern()` (line 194), optional parameters are treated the same as required:

```csharp
case ParameterDefinition param when param.IsOptional:
  string optVarName = $"__{param.CamelCaseName}_{routeIndex}";
  parts.Add($"var {optVarName}");  // Same as required param!
  break;
```

This generates `["sleep", var __seconds_0]` which only matches 2-element arrays.

## Solution

For routes with optional positional parameters, we need to:
1. Generate multiple patterns OR use length-based matching
2. Handle the case where optional param is missing

**Option A: Multiple patterns**
```csharp
if (args is ["sleep", var __seconds_0])
{
  int? seconds = int.Parse(__seconds_0, ...);
  // handler
}
else if (args is ["sleep"])
{
  int? seconds = null;
  // handler
}
```

**Option B: Use length-based matching (like complex routes)**
```csharp
if (args.Length >= 1 && args[0] == "sleep")
{
  int? seconds = args.Length > 1 ? int.Parse(args[1], ...) : null;
  // handler
}
```

Option B is simpler and consistent with `EmitComplexMatch`. Could extend `HasOptions || HasCatchAll` condition to include `HasOptionalPositionalParams`.

## Relevant Source File

`source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs`

## Test Case

**File:** `tests/timewarp-nuru-core-tests/generator/generator-09-optional-positional-params.cs`

Test both:
- `sleep` (no value) → should work, seconds = null
- `sleep 5` (with value) → should work, seconds = 5

## Checklist

- [x] Decide on approach (multiple patterns vs length-based) → **Option B: length-based**
- [x] Modify route emission to handle optional positional params
- [x] Create test file `generator-09-optional-positional-params.cs`
- [x] Verify test passes for both cases (with and without optional param)

## Results

**Approach chosen:** Option B - length-based matching (consistent with `EmitComplexMatch`)

**Files modified:**
1. `route-definition.cs` - Added `HasOptionalPositionalParams` property
2. `route-matcher-emitter.cs`:
   - Updated `Emit()` condition to include `HasOptionalPositionalParams`
   - Updated `EmitParameterExtraction()` to use `string?` and `null` for optional params
   - Refactored `EmitTypeConversions()` to use switch expression and handle null check for optional params

**Generated code (after fix):**
```csharp
// Route: sleep {seconds:int?}
if (args.Length >= 1)
{
  if (args[0] != "sleep") goto route_skip_0;
  string? __seconds_0 = args.Length > 1 ? args[1] : null;
  int? seconds = __seconds_0 is not null ? int.Parse(__seconds_0, ...) : null;
  // handler
}
```

**Tests:**
- `sleep` (no value) → "Sleeping for 1 seconds" ✓
- `sleep 5` → "Sleeping for 5 seconds" ✓
- `greet` (untyped optional) → "Hello, World!" ✓
- `greet Alice` → "Hello, Alice!" ✓
- All generator tests (01-09) pass ✓

## Notes

This is orthogonal to #300 (nullable type conversion) and #301 (complex routes). Those fix type parsing, this fixes pattern matching.

After #300 and #301 are fixed, optional typed params with provided values will work. This bug (#302) is needed for the case where the optional value is omitted.
