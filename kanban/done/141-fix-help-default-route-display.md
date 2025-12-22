# Fix Help Default Route Display

## Description

The `--help` output displays a blank entry followed by a comma before the `list` command for the default route:

```
Commands:
  , list                        Display the kanban board
```

This should display cleanly without the leading comma and blank space. The default route should either show just `list` or use a cleaner format like `list (default)`.

## Checklist

### Implementation
- [x] Identify where help text generation handles default routes
- [x] Fix the formatting to remove blank entry and comma
- [x] Verify fix with `--help` output

### Testing
- [x] Ensure other help displays are not affected
- [x] Add dedicated test file for default route display

## Solution

Modified `HelpProvider.AppendGroup()` in `source/timewarp-nuru-core/help/help-provider.cs` to:

1. Filter out empty patterns (default routes) from the display list
2. When a group contains only a default route, show `(default)` as the pattern
3. When a group contains a default route alongside other patterns, append `(default)` to the first pattern

### Before
```
Commands:
  , list                        Display the kanban board
  (default)                     Default welcome message
```

### After
```
Commands:
  list (default)                Display the kanban board
  (default)                     Default welcome message
```

### Tests Added
- `tests/timewarp-nuru-core-tests/help-provider-04-default-route.cs` - 5 tests covering:
  - Default route displays as `(default)` marker
  - No leading comma when default and alias share description
  - Multiple aliases with default handled correctly
  - Standalone default route displays correctly
  - Normal aliases without default unaffected
