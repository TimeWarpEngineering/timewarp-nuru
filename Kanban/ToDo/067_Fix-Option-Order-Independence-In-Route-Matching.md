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
- [ ] Identify the route matching logic that enforces option order
- [ ] Modify matcher to collect options position-independently
- [ ] Ensure required positional arguments are still validated correctly
- [ ] Add/Update Tests for option order independence

### Documentation
- [ ] Update documentation if option ordering behavior was previously documented

## Notes

- Tab completion already handles this correctly, so the completion logic can serve as a reference
- This is a fundamental UX issue - users expect options to work in any order (standard CLI behavior)
