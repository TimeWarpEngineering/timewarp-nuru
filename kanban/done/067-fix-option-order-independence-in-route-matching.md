# Fix Option Order Independence in Route Matching

## Description

The route matcher fails to match commands when options are provided in a different order than defined in the route pattern. Options should be position-independent - they can appear anywhere after the command and required arguments.

### Bug Reproduction

```
demo> backup "something" --output "mydest" --compress
No matching command found

demo> backup "something" --compress --output "mydest"
Backing up 'something' to 'mydest'
  Compression: enabled
Backup complete.
```

Tab completion correctly handles both orderings, but the route matcher only accepts options in the exact order they were defined in the route pattern.

## Requirements

- Options must be matchable regardless of their position in the command line
- The order of options should not affect route matching
- Tab completion and route matching should have consistent behavior regarding option ordering

## Checklist

### Implementation
- [x] Identify the route matching logic that enforces option order
- [x] Modify matcher to collect options position-independently
- [x] Ensure required positional arguments are still validated correctly
- [x] Add/Update Tests for option order independence

### Documentation
- [x] Update documentation if option ordering behavior was previously documented (none needed - no documentation explicitly required specific ordering)

## Notes

- Tab completion already handles this correctly, so the completion logic can serve as a reference
- This is a fundamental UX issue - users expect options to work in any order (standard CLI behavior)

## Implementation Notes

### Root Cause
The `MatchOptionSegment` method in `EndpointResolver.cs` was checking if options matched only at the **current position** (`args[consumedArgs]`). If options were provided in a different order than the route pattern, they wouldn't be at the expected position.

### Fix Applied
Modified `MatchOptionSegment` to search through ALL unconsumed arguments (using `consumedIndices` HashSet) rather than just checking the current sequential position. This allows options to appear in any order after positional arguments.

### Key Changes
- `Source/TimeWarp.Nuru/Resolution/EndpointResolver.cs`: Changed option matching from sequential position check to full search through unconsumed args
- Removed unused `ref int consumedArgs` parameter from `MatchOptionSegment` method
- Added comprehensive test file: `Tests/TimeWarp.Nuru.Tests/Routing/routing-14-option-order-independence.cs`

### Test Coverage (9 tests)
- Options in different order than pattern
- Options in same order as pattern (regression test)
- Three options in various orderings (abc, cba, bac)
- Optional options in any order
- Options interleaved between positional args (correctly rejected)
- Short form aliases in different order
- Required option validation still works
