# Fix Help Route Priority Over Optional Flags

## Description

GitHub Issue #98: Auto-generated `--help` routes incorrectly match before user-defined routes with optional flags when the help flag is not explicitly provided.

When defining a route like `recent --verbose?` and invoking just `recent`, the auto-generated `recent --help` route matches first and displays usage instead of executing the user's handler with `verbose=false`.

## Requirements

- User-defined routes with optional flags must execute when invoked without the flag
- Auto-generated help routes should only display help when `--help` is explicitly provided
- Existing specificity algorithm should remain intact for non-help route matching
- All existing tests must continue to pass

## Checklist

### Analysis
- [x] Reproduce the issue
- [x] Identify root cause in specificity scoring
- [x] Document route matching behavior

### Implementation
- [x] Implement fix (new approach - see Implemented Solution below)
- [x] Add regression tests for the fix
- [x] Verify fix doesn't break existing help functionality

### Verification
- [x] Test `command` matches user route, not help route
- [x] Test `command --help` still shows help
- [x] Test `command --flag` executes user route correctly
- [x] Run full test suite (all routing tests pass)

## Notes

### Reproduction

```csharp
NuruCoreAppBuilder builder = NuruApp.CreateSlimBuilder(args);
builder.Map("recent --verbose?", (bool verbose) =>
{
    Console.WriteLine($"Executing with verbose={verbose}");
    return 0;
}, "Show recent items");
```

Running `recent` shows help instead of executing the handler.

### Root Cause Analysis

The specificity scoring gives higher points to the help route:

| Route | Calculation | Score |
|-------|-------------|-------|
| `recent --help` | 100 (literal) + 50 (boolean option) | **150** |
| `recent --verbose?` | 100 (literal) + 25 (optional option) | **125** |

Both routes match input `recent`:
- Both have the literal "recent" which matches
- Both have optional boolean flags defaulting to `false`
- Both are valid matches with `defaultsUsed: 1`
- Resolver picks higher specificity → `recent --help` wins incorrectly

### Key Files

- `source/timewarp-nuru-core/resolution/endpoint-resolver.cs` - Route matching logic
- `source/timewarp-nuru-core/help/help-route-generator.cs` - Help route generation
- `source/timewarp-nuru-parsing/parsing/compiler/compiler.cs` - Specificity scoring

### Implementation Insight

The scoring logic should use the `IsOptional` property on segments rather than checking for `?` in the pattern string. This provides a more robust and semantic approach to determining optionality.

### Suggested Fix Options

#### Option 1: Specificity Adjustment for Help Routes

Lower the specificity score for auto-generated help routes so user-defined routes take precedence.

Approach:
- Add a flag to identify auto-generated help routes
- Apply a specificity penalty (e.g., -200) to help routes
- Or use a separate "priority tier" where user routes are checked before help routes

Pros:
- Simple to implement
- Clear separation between user and system routes

Cons:
- Requires tracking which routes are auto-generated

#### Option 3: Route Ordering - User Routes Before Help Routes

Ensure user-defined routes are checked before auto-generated help routes regardless of specificity.

Approach:
- Add an `Order` property to endpoints (already exists in `Endpoint.cs`)
- Set user-defined routes to `Order = 0` (or lower number = higher priority)
- Set auto-generated help routes to `Order = 1000` (or higher number = lower priority)
- Modify `EndpointCollection.Sort()` to sort by Order first, then Specificity

Pros:
- Uses existing `Order` property
- Clear semantic meaning: user routes always checked first
- No changes to specificity algorithm

Cons:
- Need to ensure help routes are still found when explicitly requested

### Test File

Regression tests added at:
`tests/timewarp-nuru-tests/routing/routing-15-help-route-priority.cs`

### Implemented Solution

The fix uses two changes:

1. **Help routes use `--help?` (optional)** instead of `--help`
   - Auto-generated help routes now have same specificity as user optional flags
   - File: `source/timewarp-nuru-core/help/help-route-generator.cs`

2. **Route resolver prioritizes exact matches, then more defaults**
   - Exact matches (0 defaults) always win
   - Among partial matches, prefer routes with MORE defaults
   - This ensures user routes with multiple optional flags beat help routes
   - Help only wins when `--help` is explicitly provided (0 defaults for help route)
   - File: `source/timewarp-nuru-core/resolution/endpoint-resolver.cs`

This approach treats help routes as normal routes after generation, maintaining consistency.

### Related

- GitHub Issue: #98
- Specificity Algorithm: `documentation/developer/design/resolver/specificity-algorithm.md`
