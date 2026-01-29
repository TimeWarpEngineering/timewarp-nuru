# Bug: Default route [NuruRoute("")] prevents --help from working

## Description

When an endpoint has `[NuruRoute("")]` (empty pattern for default/catch-all route) **with options**, it intercepts `--help` before the built-in help handler can run.

## Refined Root Cause (from test investigation)

The bug is more specific than originally thought:

| Route Pattern | Match Code Generated | Catches `--help`? |
|---------------|---------------------|-------------------|
| `Map("")` (no options) | `if (routeArgs is [])` via `EmitSimpleMatch` | **No** - Works correctly! |
| `Map("--verbose?")` (options only) | `if (routeArgs.Length >= 0)` via `EmitComplexMatch` | **YES - BUG!** |
| `[NuruRoute("")]` with `[Option]` | `if (routeArgs.Length >= 0)` via `EmitComplexMatch` | **YES - BUG!** |

**The issue is routes with options but no literal prefix**, not just default routes. The `EmitComplexMatch` path in `route-matcher-emitter.cs` generates a condition that's always true when `minPositionalArgs == 0`.

### Code Path Analysis

In `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs`:

```csharp
// Line 135-138: EmitComplexMatch calculates minPositionalArgs
int minPositionalArgs = route.PositionalMatchSegments.Count() + route.Parameters.Count(p => !p.IsOptional && !p.IsCatchAll);

// For routes with only options (no literals, no required params), minPositionalArgs = 0
sb.AppendLine($"    if (routeArgs.Length >= {minPositionalArgs})");  // Always true!
```

### Current Behavior

In `source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs` lines 602-604:

```csharp
// Built-in flags: --help, --version, --capabilities
// Emitted AFTER user routes so users can override default behavior
EmitBuiltInFlags(sb, app, methodSuffix);
```

The built-in flags are emitted AFTER user routes. A default route with options generates:

```csharp
if (routeArgs.Length >= 0)  // Always true!
{
  // ... handles the route, intercepting --help
}
```

### Expected Behavior

Running `app --help` should display the generated help (from `PrintHelp`), not trigger the default route handler.

## Test Results

Test file: `tests/timewarp-nuru-tests/help/help-02-default-route-help.cs`

| Test | Result | Notes |
|------|--------|-------|
| `Should_show_help_when_default_route_exists` | **PASSED** | `Map("")` alone works! |
| `Should_show_help_when_default_route_has_options` | **FAILED** | `Map("--verbose?")` breaks it! |
| `Should_execute_default_route_with_empty_args` | **PASSED** | Default route works with `[]` |
| `Should_show_help_with_short_form_when_default_route_exists` | **PASSED** | `-h` works |
| `Should_show_version_when_default_route_exists` | **PASSED** | `--version` works |
| `Should_show_capabilities_when_default_route_exists` | **PASSED** | `--capabilities` works |

**5 passed, 1 failed** - Bug confirmed for routes with options but no literals.

### Reproduction

In `samples/03-endpoints`:
- `DefaultQuery` has `[NuruRoute("")]` with `[Option("verbose", "v")]`
- Running `dotnet run --project samples/03-endpoints -- --help` shows the hardcoded help from `DefaultQuery.Handler`, NOT the generated `PrintHelp` output

## Possible Solutions

1. **Emit built-in flags BEFORE user routes** - but this prevents users from overriding help behavior
2. **Emit default/catch-all routes AFTER built-in flags** - sort routes so empty patterns come last, after built-ins
3. **Add special handling in EmitComplexMatch** - when `minPositionalArgs == 0`, explicitly exclude built-in flags first

### Recommended Solution: Option 3

Modify `EmitComplexMatch` in `route-matcher-emitter.cs` to add an explicit built-in flag exclusion check when `minPositionalArgs == 0`:

```csharp
// For routes that match everything (minPositionalArgs == 0),
// explicitly exclude built-in flags first
if (minPositionalArgs == 0)
{
  sb.AppendLine($"    // Default route: skip built-in flags first");
  sb.AppendLine($"    if (routeArgs is [\"--help\" or \"-h\"] or [\"--version\"] or [\"--capabilities\"])");
  sb.AppendLine($"      goto route_skip_{routeIndex};");
}
```

This approach:
- Fixes `--help` for routes with options but no literals
- Preserves catch-all/custom override capabilities
- Minimal code change - only affects routes that actually match everything
- Doesn't affect specificity ordering

## Related

- Discovered while working on task 402 (nested NuruRouteGroup)
- The generated `PrintHelp` correctly includes all routes (including nested groups)
- Task 370 (Help behavior for routes with same prefix) may be related

## Files

- `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs` - EmitComplexMatch method (line 138)
- `source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs` - EmitBuiltInFlags method
- `samples/03-endpoints/messages/queries/default-query.cs` - Example of affected default route
- `tests/timewarp-nuru-tests/help/help-02-default-route-help.cs` - Test cases (1 failing)

## Checklist

- [x] Verify the bug reproduces in `samples/03-endpoints`
- [x] Analyze the route emission order in `interceptor-emitter.cs`
- [x] Analyze `EmitComplexMatch` in `route-matcher-emitter.cs`
- [x] Create test cases to confirm bug
- [x] Determine the best solution approach (Option 3 recommended)
- [ ] Implement the fix in `route-matcher-emitter.cs`
- [ ] Test that `--help` now works correctly with default routes
- [ ] Verify user can still override help behavior if needed
- [ ] Run full CI tests to ensure no regressions

## Notes

Root cause refined: Routes with options but no literal prefix use `EmitComplexMatch` which generates `routeArgs.Length >= 0` (always true) when `minPositionalArgs == 0`. This catches `--help` before the built-in handler runs.
