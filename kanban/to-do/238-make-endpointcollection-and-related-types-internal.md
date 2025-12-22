# Make EndpointCollection and related types internal

## Description

Change `EndpointCollection` and related types from `public` to `internal` visibility. These types are implementation details not intended for external consumption. After `Build()`, the app is designed to be immutable - no new endpoints should be added. Making these internal enforces this design intent at the API level.

## Checklist

- [ ] Change `EndpointCollection` class to internal (`endpoint-collection.cs:8`)
- [ ] Change `IEndpointCollectionBuilder` interface to internal (`iendpoint-collection-builder.cs:6`)
- [ ] Change `DefaultEndpointCollectionBuilder` class to internal (`default-endpoint-collection-builder.cs:6`)
- [ ] Change `NuruCoreApp.Endpoints` property to internal (`nuru-core-app.cs:114`)
- [ ] Change `NuruCoreAppBuilder.EndpointCollection` property to internal (`nuru-core-app-builder.cs:24`)
- [ ] Run `dotnet build` to verify no compilation errors
- [ ] Run tests to verify no regressions

## Notes

### Why this is safe

- **Samples**: No usage of `EndpointCollection` or `.Endpoints`
- **Tests**: Heavy usage, but already have `InternalsVisibleTo` access via generated file
- **Sibling packages**: Already have `InternalsVisibleTo` in `timewarp-nuru-core.csproj`:
  - `TimeWarp.Nuru`
  - `TimeWarp.Nuru.Completion`
  - `TimeWarp.Nuru.Repl`

### Design context

From `endpoint-collection.cs` comment:
> "Thread-safety is not needed as routes are configured once at startup in CLI apps."

This change makes the "configured once at startup" design intent explicit in the API.
