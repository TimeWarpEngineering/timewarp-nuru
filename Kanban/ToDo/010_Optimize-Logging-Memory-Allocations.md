# Optimize Logging Memory Allocations

## Problem
The current logging implementation causes unnecessary memory allocations even when logging is disabled:

1. **String interpolation always allocates** - Strings are built before checking if logging is enabled
2. **Property access creates new structs** - ComponentLogger properties create new instances on each access
3. **Impact on benchmarks** - Adds ~50-200 bytes per log call to the framework's minimal allocation goal (3,992 B)

## Current Impact
```csharp
// This allocates a string even when Debug logging is OFF:
NuruLogger.Parser.Debug($"Setting boolean option parameter name to: '{valueParameterName}'");
```

## Proposed Solution

### 1. Add Guard Clauses
```csharp
if (NuruLogger.Parser.IsDebugEnabled)
{
    NuruLogger.Parser.Debug($"Setting boolean option parameter name to: '{valueParameterName}'");
}
```

### 2. Make ComponentLogger Properties Static Readonly Fields
```csharp
// Instead of:
public static ComponentLogger Parser => new(LogComponent.Parser);

// Use:
public static readonly ComponentLogger Parser = new(LogComponent.Parser);
```

### 3. Consider Zero-Allocation Logging (Future)
- Investigate source generators like .NET's LoggerMessage.Define
- Consider compile-time string building for common patterns

## Acceptance Criteria
- [ ] No allocations when logging is disabled
- [ ] Maintain current logging functionality
- [ ] Update all existing log calls to use guard clauses where appropriate
- [ ] Benchmark shows no regression in the 3,992 B allocation target

## Files to Update
- `Source/TimeWarp.Nuru.Parsing/Logging/NuruLogger.cs` - Change properties to static readonly fields
- `Source/TimeWarp.Nuru.Parsing/Logging/ComponentLogger.cs` - Potentially add helper methods
- All files with logging calls - Add IsEnabled checks before string interpolation

## Notes
- Getting functionality working first was the right call
- This optimization is important for maintaining the framework's performance goals
- Consider adding analyzer rules to enforce guard clauses for Debug/Trace logging