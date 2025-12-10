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
- [ ] Identify where help text generation handles default routes
- [ ] Fix the formatting to remove blank entry and comma
- [ ] Verify fix with `--help` output

### Testing
- [ ] Ensure other help displays are not affected
