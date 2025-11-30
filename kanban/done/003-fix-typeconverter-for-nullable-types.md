# Fix TypeConverter for Nullable Types

## Problem
When using typed optional parameters like `{seconds:int?}`, the framework throws an error:
```
Error executing handler: Cannot convert '2' to type System.Nullable`1[System.Int32] for parameter 'seconds'
```

## Root Cause
The TypeConverterRegistry doesn't know how to handle nullable value types like:
- `int?` (Nullable<int>)
- `double?` (Nullable<double>)
- `bool?` (Nullable<bool>)
- etc.

## Tasks
- [x] Update TypeConverterRegistry to detect nullable types
- [x] Add conversion logic that:
  - Converts the value using the underlying type converter
  - Wraps the result in a nullable
  - Returns null for empty/missing values
- [x] Add integration tests for nullable type conversions
- [x] Test with all supported nullable types

## Example Routes That Should Work
```csharp
.AddRoute("sleep {seconds:int?}", (int? seconds) => ...)
.AddRoute("discount {percent:double?}", (double? percent) => ...)
.AddRoute("verbose {enabled:bool?}", (bool? enabled) => ...)
.AddRoute("schedule {date:DateTime?}", (DateTime? date) => ...)
```

## Acceptance Criteria
- All nullable value types work correctly with type constraints
- Empty/missing values result in null
- Valid values are properly converted to nullable types
- Error messages are clear when conversion fails

## Implementation Notes

### What Was Fixed
1. **TypeConverterRegistry** - Added nullable type handling in both `TryConvert` overloads:
   - Detects `Nullable<T>` using `Nullable.GetUnderlyingType()`
   - Converts to underlying type, boxing automatically handles nullable wrapper
   - Also handles constraint names ending with `?` (e.g., `int?`)

2. **RoutePatternParser** - Updated to recognize nullable type syntax:
   - Checks if type constraint ends with `?` (e.g., `{seconds:int?}`)
   - Sets `IsOptional = true` for nullable typed parameters
   - Preserves the `?` in the constraint for TypeConverterRegistry to handle

### Test Results
- ✅ All 44 tests pass including the new nullable type tests
- ✅ `sleep {seconds:int?}` works with both `sleep 5` and `sleep` (defaults to 1)
- ✅ Both string optional (`{tag?}`) and nullable value type (`{seconds:int?}`) syntaxes work correctly