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

The `RouteDefinition.Literals` property only returns segments from the `OriginalPattern`, ignoring the `GroupPrefix`:

```csharp
// In route-definition.cs
public IEnumerable<LiteralDefinition> Literals =>
  Segments.OfType<LiteralDefinition>();  // Only returns ["ps"], not ["docker", "ps"]
```

The `GroupPrefix` is stored separately and only used for display in `FullPattern`:

```csharp
public string FullPattern => string.IsNullOrEmpty(GroupPrefix)
  ? OriginalPattern
  : $"{GroupPrefix} {OriginalPattern}";  // Shows "docker ps" correctly
```

When `route-matcher-emitter.cs` iterates through `route.Literals`, it only gets `["ps"]`:

```csharp
// Generated code (broken)
// Route: docker ps
if (args.Length >= 1)
{
  if (args[0] != "ps") goto route_skip_7;  // BUG: Should check args[0] == "docker" && args[1] == "ps"
  // ...
}
```

## Solution

**Option A: Fix at RouteDefinition level (Preferred)**

Modify `RouteDefinition.Literals` to prepend group prefix literals:

```csharp
public IEnumerable<LiteralDefinition> Literals
{
  get
  {
    // Prepend group prefix as literal(s) if present
    if (!string.IsNullOrEmpty(GroupPrefix))
    {
      foreach (string word in GroupPrefix.Split(' ', StringSplitOptions.RemoveEmptyEntries))
      {
        yield return new LiteralDefinition(word);
      }
    }
    
    // Then yield original literals
    foreach (LiteralDefinition literal in Segments.OfType<LiteralDefinition>())
    {
      yield return literal;
    }
  }
}
```

This will generate:

```csharp
// Route: docker ps
if (args.Length >= 2)
{
  if (args[0] != "docker") goto route_skip_7;
  if (args[1] != "ps") goto route_skip_7;
  // ...
}
```

**Why Option A is preferred:**
1. Single point of fix in `RouteDefinition`
2. `FullPattern` and `Literals` stay consistent
3. No changes needed to extractor or emitter

**Option B: Fix at extractor level**

Modify `attributed-route-extractor.cs` to insert group prefix as literal segments before calling `RouteDefinition.Create()`. More invasive, requires modifying segment merging logic.

## Files to Modify

| File | Changes |
|------|---------|
| `source/timewarp-nuru-analyzers/generators/models/route-definition.cs` | Update `Literals` property to prepend group prefix |

## Checklist

- [ ] Update `RouteDefinition.Literals` to include group prefix literals
- [ ] Rebuild analyzers: `dotnet build source/timewarp-nuru-analyzers/`
- [ ] Test `docker ps`, `docker build`, `docker run`, `docker tag`
- [ ] Test `config get`, `config set`
- [ ] Ensure single-word routes still work (`greet`, `ping`, `deploy`, `exec`)
- [ ] Verify help output still shows correct patterns

## Test Commands

```bash
# Clear cache and rebuild
ganda runfile cache --clear
dotnet build source/timewarp-nuru-analyzers/timewarp-nuru-analyzers.csproj

# Test multi-word routes (currently failing)
dotnet run --project samples/03-attributed-routes/attributed-routes.csproj -- docker ps
dotnet run --project samples/03-attributed-routes/attributed-routes.csproj -- docker build /path --no-cache
dotnet run --project samples/03-attributed-routes/attributed-routes.csproj -- config get mykey
dotnet run --project samples/03-attributed-routes/attributed-routes.csproj -- config set mykey myvalue

# Test single-word routes (should still work)
dotnet run --project samples/03-attributed-routes/attributed-routes.csproj -- greet World
dotnet run --project samples/03-attributed-routes/attributed-routes.csproj -- ping
dotnet run --project samples/03-attributed-routes/attributed-routes.csproj -- deploy prod --replicas 3
```

## Related

- #304 - Attributed Routes Generator (parent task)
- Blocks completion of #304 Phase 14 (test everything)
