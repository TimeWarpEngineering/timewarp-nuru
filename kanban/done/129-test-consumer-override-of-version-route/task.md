# Test consumer override of --version route

## Summary

Add a test to verify behavior when a consumer of Nuru maps their own `--version` route, overriding the built-in one registered by `CreateBuilder()`.

## Todo List

- [x] Add test verifying consumer can override `--version` route
- [x] Verify warning message is displayed for duplicate route
- [x] Verify consumer's handler is used (not the built-in one)
- [x] Document expected behavior in user docs (completed in task 135 - built-in-routes.md)

## Notes

Current behavior in `EndpointCollection.Add()`:
- Duplicate routes print a warning to stderr
- The new handler overrides the previous one (consumer wins)

Example scenario:
```csharp
NuruApp.CreateBuilder(args)  // registers --version,-v via UseAllExtensions
  .Map("--version,-v", () => Console.WriteLine("My custom version"))  // overrides it
```

**Important:** To override the built-in route, consumer must use the SAME pattern `--version,-v` (including alias). Using just `--version` creates a separate route that doesn't override.

## Results

Tests added in `routing-20-version-route-override.cs`:
1. `Consumer_can_override_version_route` - verifies override with same pattern works
2. `Consumer_can_override_version_route_with_alias` - verifies -v short form works after override
3. `DisableVersionRoute_then_map_custom` - verifies clean registration without warning
4. `Endpoint_count_after_override` - verifies override replaces (doesn't duplicate)

All 4 tests pass. Warning is displayed for duplicate routes as expected.
