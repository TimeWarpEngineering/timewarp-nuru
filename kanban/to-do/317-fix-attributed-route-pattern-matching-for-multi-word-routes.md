# Fix attributed route pattern matching for multi-word routes

## Summary

Attributed routes with multi-word patterns (e.g., `docker ps` from `[NuruRouteGroup("docker")]` + `[NuruRoute("ps")]`) only check the first literal segment, causing routes to not match.

## Reproduction

```csharp
[NuruRouteGroup("docker")]
public abstract class DockerGroupBase;

[NuruRoute("ps", Description = "List containers")]
public sealed class DockerPsQuery : DockerGroupBase, IQuery<Unit>
{
  // ...
}
```

**Expected:** `docker ps` should match  
**Actual:** Falls through to help/unknown command

## Root Cause

Generated code checks only the last segment:

```csharp
// Route: docker ps
if (args.Length >= 1)
{
  if (args[0] != "ps") goto route_skip_7;  // BUG: Should check args[0] == "docker" && args[1] == "ps"
  // ...
}
```

The route pattern is `docker ps` but `route-matcher-emitter.cs` only emits a check for the last literal token.

## Solution

In `route-matcher-emitter.cs`, when emitting literal token checks for attributed routes with group prefixes, emit checks for ALL literal segments:

```csharp
// Route: docker ps
if (args.Length >= 2)
{
  if (args[0] != "docker") goto route_skip_7;
  if (args[1] != "ps") goto route_skip_7;
  // ...
}
```

## Files to Modify

| File | Changes |
|------|---------|
| `generators/emitters/route-matcher-emitter.cs` | Fix literal token matching for multi-word patterns |

## Checklist

- [ ] Identify how group prefix is combined with route pattern
- [ ] Fix `EmitLiteralChecks()` to handle all literal segments
- [ ] Test `docker ps`, `docker build`, `docker run`, `docker tag`
- [ ] Test `config get`, `config set`
- [ ] Ensure single-word routes still work (`greet`, `ping`, `deploy`, `exec`)

## Test Commands

```bash
dotnet run --project samples/03-attributed-routes/attributed-routes.csproj -- docker ps
dotnet run --project samples/03-attributed-routes/attributed-routes.csproj -- docker build /path
dotnet run --project samples/03-attributed-routes/attributed-routes.csproj -- config get mykey
```

## Related

- #304 - Attributed Routes Generator (parent task)
- Blocks completion of #304 Phase 14 (test everything)
