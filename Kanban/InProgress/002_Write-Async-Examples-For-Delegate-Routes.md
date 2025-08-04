# Write Async Examples for Delegate Based Routes

## Problem
When trying to use optional parameters in async routes like:
```csharp
.AddRoute("wt {option?}", async (string? option) => await WindowsTerminalCommand.ExecuteAsync(option), "Windows Terminal integration demo")
```

The following error occurs:
```
Error executing handler: No value provided for required parameter 'option'
```

## Root Cause
The Nuru framework doesn't properly support the optional parameter syntax (`{param?}`) in route patterns:
1. The RoutePatternParser regex doesn't capture the `?` suffix
2. The ParameterSegment class doesn't have an IsOptional property
3. The parameter binding logic only checks `param.HasDefaultValue`, not nullable parameters

## Tasks
- [x] Fix optional parameter parsing in RoutePatternParser
- [x] Add IsOptional property to ParameterSegment
- [x] Update parameter binding to handle optional parameters
- [x] Create comprehensive async examples including:
  - Simple async routes
  - Async routes with parameters
  - Async routes with optional parameters
  - Error handling in async routes
  - Async routes returning Task<T>

## Acceptance Criteria
- Optional parameters work correctly with `{param?}` syntax
- Async routes work with all parameter types
- Test coverage for async routes with optional parameters
- Documentation updated to show async examples

## Implementation Notes

### What Was Fixed
1. **RoutePatternParser.cs** - Updated regex from `\{(\*)?([^}:|]+)(:([^}|]+))?(\|([^}]+))?\}` to `\{(\*)?([^}:|?]+)(\?)?(:([^}|]+))?(\|([^}]+))?\}` to capture the `?` suffix
2. **ParameterSegment.cs** - Added `IsOptional` property and updated constructor to accept it
3. **RouteBasedCommandResolver.cs** - Fixed `MatchPositionalSegments` to skip optional parameters when no value is provided
4. **NuruApp.cs** - Added `IsOptionalParameter` helper method and updated `BindParameters` to set optional parameters to null when no value is provided

### What Still Needs Work
- Typed optional parameters like `{seconds:int?}` don't work because TypeConverterRegistry doesn't handle nullable value types
- Created separate task 003 to track this work

### Test Results
- ✅ `deploy {env} {tag?}` works with both `deploy prod` and `deploy prod v2.0`
- ✅ `backup {source} {destination?}` works for async routes
- ❌ `sleep {seconds:int?}` fails with "Cannot convert '2' to type System.Nullable`1[System.Int32]"