# Attributed Routes

Demonstrates auto-registration of routes via `[NuruRoute]` attributes with zero `Map()` calls.

## Run It

```bash
dotnet run samples/03-attributed-routes/attributed-routes.cs -- greet Alice
dotnet run samples/03-attributed-routes/attributed-routes.cs -- deploy staging --dry-run
dotnet run samples/03-attributed-routes/attributed-routes.cs -- docker run nginx
```

## What's Demonstrated

- `[NuruRoute]` attribute for route discovery
- `[Parameter]` and `[Option]` attributes for binding
- `[NuruRouteGroup]` for grouped commands (e.g., `docker run`, `docker build`)
- `[NuruRouteAlias]` for command aliases
- Catch-all parameters with `{*args}`

## Related Documentation

- [Attributed Routes](../../documentation/user/features/attributed-routes.md)
