# Fix AddInteractiveRoute to Use Alias Syntax

## Description

`AddInteractiveRoute` uses `MapMultiple` to register `--interactive` and `-i` as separate endpoints. This causes them to appear as separate lines in help output. Should use the built-in option alias syntax instead.

## Root Cause

In `source/timewarp-nuru-repl/nuru-app-extensions.cs` line 118:

```csharp
builder.MapMultiple(patternArray, StartInteractiveModeAsync, "Enter interactive REPL mode");
// where patternArray = ["--interactive", "-i"]
```

This creates two separate `Endpoint` instances.

## Fix

Use single route pattern with alias syntax:

```csharp
builder.Map("--interactive,-i", StartInteractiveModeAsync, "Enter interactive REPL mode");
```

The comma syntax is already supported for options and correctly populates `OptionMatcher.AlternateForm`, which `HelpRouteGenerator` already consolidates in display.

## Checklist

### Implementation
- [x] Update `AddInteractiveRoute` to use `"--interactive,-i"` pattern
- [x] Handle custom patterns parameter (parse and convert to alias syntax if both are options)
- [x] Add test verifying single endpoint is created
- [x] Verify help output shows `-i, --interactive` on one line

## Notes

- The `patterns` parameter allows custom values like `"--interactive,-i,--repl"` - need to handle conversion
- Option alias syntax only works for exactly 2 options (long + short form), not multiple
- More than 2 options fall back to `MapMultiple`
- `MapMultiple` remains appropriate for literal command aliases like `["exit", "quit", "q"]`

## Implementation Details

The fix uses conditional logic:
1. If exactly 2 patterns are provided AND both are options (start with `-`), use `Map` with alias syntax
2. Otherwise, use `MapMultiple` for literals or more than 2 option aliases

Tests added in `tests/timewarp-nuru-repl-tests/repl-34-interactive-route-alias.cs`:
- `Should_create_single_endpoint_for_option_aliases` - verifies default case creates single endpoint
- `Should_use_alias_syntax_in_route_pattern` - verifies custom 2-option aliases work
- `Should_use_map_multiple_for_literal_commands` - verifies literals use MapMultiple
- `Should_handle_two_option_aliases` - verifies custom long+short form works
- `Should_fallback_to_map_multiple_for_more_than_two_options` - verifies >2 options fallback
- `Should_show_aliases_on_single_help_line` - verifies help consolidates aliases
