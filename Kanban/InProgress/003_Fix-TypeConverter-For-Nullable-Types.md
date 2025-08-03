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
- [ ] Update TypeConverterRegistry to detect nullable types
- [ ] Add conversion logic that:
  - Converts the value using the underlying type converter
  - Wraps the result in a nullable
  - Returns null for empty/missing values
- [ ] Add unit tests for nullable type conversions
- [ ] Test with all supported nullable types

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