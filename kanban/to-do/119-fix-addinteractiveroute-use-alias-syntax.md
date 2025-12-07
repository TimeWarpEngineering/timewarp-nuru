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
- [ ] Update `AddInteractiveRoute` to use `"--interactive,-i"` pattern
- [ ] Handle custom patterns parameter (parse and convert to alias syntax if both are options)
- [ ] Add test verifying single endpoint is created
- [ ] Verify help output shows `-i, --interactive` on one line

## Notes

- The `patterns` parameter allows custom values like `"--interactive,-i,--repl"` - need to handle conversion
- Option alias syntax only works for options (dash-prefixed), not literal commands
- `MapMultiple` remains appropriate for literal command aliases like `["exit", "quit", "q"]`
