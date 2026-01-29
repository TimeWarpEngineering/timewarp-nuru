# Bug: Default route [NuruRoute("")] prevents --help from working

## Description

When an endpoint has `[NuruRoute("")]` (empty pattern for default/catch-all route), it intercepts `--help` before the built-in help handler can run.

### Current Behavior

In `source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs` lines 602-604:

```csharp
// Built-in flags: --help, --version, --capabilities
// Emitted AFTER user routes so users can override default behavior
EmitBuiltInFlags(sb, app, methodSuffix);
```

The built-in flags are emitted AFTER user routes. A default route with `[NuruRoute("")]` generates:

```csharp
if (routeArgs.Length >= 0)  // Always true!
{
  // ... handles the route
}
```

This catches `--help` before the built-in handler at line 806:
```csharp
if (routeArgs is ["--help" or "-h"])
{
  PrintHelp(app.Terminal);
  return 0;
}
```

### Expected Behavior

Running `app --help` should display the generated help (from `PrintHelp`), not trigger the default route handler.

### Reproduction

In `samples/03-endpoints`:
- `DefaultQuery` has `[NuruRoute("")]`
- Running `dotnet run --project samples/03-endpoints -- --help` shows the hardcoded help from `DefaultQuery.Handler`, NOT the generated `PrintHelp` output

### Possible Solutions

1. **Emit built-in flags BEFORE user routes** - but this prevents users from overriding help behavior
2. **Emit default/catch-all routes AFTER built-in flags** - sort routes so empty patterns come last, after built-ins
3. **Add special handling** - default routes should explicitly check for `--help` and skip

## Related

- Discovered while working on task 402 (nested NuruRouteGroup)
- The generated `PrintHelp` correctly includes all routes (including nested groups)
- Task 370 (Help behavior for routes with same prefix) may be related

## Files

- `source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs` - EmitBuiltInFlags method
- `samples/03-endpoints/messages/queries/default-query.cs` - Example of affected default route

## Checklist

- [ ] Verify the bug reproduces in `samples/03-endpoints`
- [ ] Analyze the route emission order in `interceptor-emitter.cs`
- [ ] Determine the best solution approach (1, 2, or 3 above)
- [ ] Implement the fix
- [ ] Test that `--help` now works correctly with default routes
- [ ] Verify user can still override help behavior if needed

## Notes

Root cause: Default route with empty pattern `[NuruRoute("")]` generates a condition that is always true (`routeArgs.Length >= 0`), which catches `--help` before the built-in handler runs.
