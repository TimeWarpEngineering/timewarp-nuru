# Endpoints

Demonstrates auto-registration of routes via `[NuruRoute]` attributes with zero `Map()` calls.

## Run It

```bash
dotnet run samples/03-endpoints/endpoints.cs -- greet Alice
dotnet run samples/03-endpoints/endpoints.cs -- deploy staging --dry-run
dotnet run samples/03-endpoints/endpoints.cs -- docker run nginx
```

## What's Demonstrated

- `[NuruRoute]` attribute for route discovery
- `[Parameter]` and `[Option]` attributes for binding
- `[NuruRouteGroup]` for grouped commands (e.g., `docker run`, `docker build`)
- `[NuruRouteAlias]` for command aliases
- Catch-all parameters with `{*args}`

## Related Documentation

- [Endpoints](../../documentation/user/features/endpoints.md)
