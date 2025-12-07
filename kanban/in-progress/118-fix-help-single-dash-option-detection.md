# Fix HelpProvider Single-Dash Option Detection

## Description

`HelpProvider` incorrectly classifies single-dash options (e.g., `-i`) as commands instead of options. This causes routes like `-i` to appear in the "Commands:" section of help output rather than the "Options:" section.

## Root Cause

In `source/timewarp-nuru-core/help/help-provider.cs` lines 38-39:

```csharp
List<Endpoint> commands = [.. routes.Where(r => !r.RoutePattern.StartsWith("--", StringComparison.Ordinal))];
List<Endpoint> options = [.. routes.Where(r => r.RoutePattern.StartsWith("--", StringComparison.Ordinal))];
```

Only `--` prefix is recognized as an option. Single-dash `-x` patterns are treated as commands.

## Fix

Change the check to recognize both single-dash and double-dash prefixes:

```csharp
List<Endpoint> commands = [.. routes.Where(r => !r.RoutePattern.StartsWith("-", StringComparison.Ordinal))];
List<Endpoint> options = [.. routes.Where(r => r.RoutePattern.StartsWith("-", StringComparison.Ordinal))];
```

## Checklist

### Implementation
- [ ] Update `HelpProvider.cs` to use `-` prefix check
- [ ] Add test verifying `-i` appears in Options section
- [ ] Verify existing tests pass

## Notes

- This is a standalone bug fix, separate from the larger `HelpOptions` configuration work
- Related: `-i` and `--interactive` should ideally be consolidated as aliases (separate task)
