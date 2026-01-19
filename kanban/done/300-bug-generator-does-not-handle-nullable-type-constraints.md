# Bug: Generator does not handle nullable type constraints

## Description

`EmitTypeConversions()` in `route-matcher-emitter.cs` does not handle nullable type constraints like `int?`, `double?`, etc. When a parameter has a type constraint like `{seconds:int?}`, the switch statement falls through to the `default` case and emits a TODO comment instead of actual parsing code.

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
if (args is ["sleep", var __seconds_0])
{
  // TODO: Type conversion for global::int?   // BUG: Should parse to int?
  void __handler_0()
  {
    int sleepTime = seconds ?? 1;  // CS0103: 'seconds' does not exist
  }
}
```

**Error:**
```
error CS0103: The name 'seconds' does not exist in the current context
```

## Root Cause

In `EmitTypeConversions()` (lines 127-188), the switch on `param.TypeConstraint?.ToLowerInvariant()` has cases for `"int"`, `"double"`, etc., but NOT for `"int?"`, `"double?"`, etc.

When `TypeConstraint` is `"int?"`, it falls through to `default` which only emits:
```csharp
$"{indentStr}// TODO: Type conversion for {param.ResolvedClrTypeName}");
```

## Solution

Modify `EmitTypeConversions()` to:
1. Strip the `?` suffix from the type constraint
2. Check `param.IsOptional` to determine if nullable type should be used
3. Generate appropriate parsing code for nullable types

**For nullable types:**
```csharp
int? seconds = __seconds_0 is { } __val ? int.Parse(__val, CultureInfo.InvariantCulture) : null;
```

Or since we're in a list pattern that only matches when the value exists:
```csharp
int? seconds = int.Parse(__seconds_0, CultureInfo.InvariantCulture);
```

Note: The nullable aspect matters more for optional positional matching (Bug #302), but we still need to declare the variable as `int?` to match the handler signature.

## Relevant Source File

`source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs`

## Test Case

**File:** `tests/timewarp-nuru-core-tests/generator/generator-07-nullable-type-conversion.cs`

```csharp
.Map("sleep {seconds:int?}")
  .WithHandler((int? seconds) => WriteLine($"Sleeping for {seconds ?? 1} seconds"))
```

Test with: `sleep 5` (value provided - this should work after fix)

## Checklist

- [x] Modify `EmitTypeConversions()` to handle nullable type constraints
- [x] Strip `?` suffix and use base type for parsing
- [x] Declare variable as nullable type (`int?`, `double?`, etc.)
- [x] Create test file `generator-07-nullable-type-conversion.cs`
- [x] Verify test passes with value provided

## Results

**Fixed in:** `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs`

**Changes:**
- Added `baseType` variable that strips `?` suffix from `TypeConstraint`
- Added `nullableSuffix` variable that adds `?` when `param.IsOptional` is true
- Updated all type cases to use `{baseType}{nullableSuffix}` pattern

**Generated code (after fix):**
```csharp
int? seconds = int.Parse(__seconds_0, System.Globalization.CultureInfo.InvariantCulture);
```

**Tests:**
- `generator-07-nullable-type-conversion.cs` - passes
- `sleep 5` - outputs "Sleeping for 5 seconds"
- `scale 3.5` - outputs "Scaled: 7"
- All existing generator tests still pass (17/17)

## Notes

This is blocked by Bug #302 for the case where the optional value is NOT provided (pattern matching issue). But fixing this allows optional typed params to work when value IS provided.
