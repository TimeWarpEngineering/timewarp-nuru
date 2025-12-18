# Add Route Matching Tests for Attributed Routes

## Description

Create tests that verify routes generated from `[NuruRoute]` attributes correctly match input arguments. These tests use the `CompiledRoute.Match()` method to verify matching behavior.

## Parent

150-implement-attributed-routes-phase-1

## Dependencies

- Task 186: Test utilities must be created first

## Checklist

### Test File
- [ ] Create `tests/timewarp-nuru-analyzers-tests/auto/attributed-route-generator-03-matching.cs`

### Literal Matching Tests
- [ ] Test simple literal matches: `["status"]` matches `[NuruRoute("status")]`
- [ ] Test wrong literal does not match: `["help"]` does not match `[NuruRoute("status")]`
- [ ] Test empty default route matches empty args
- [ ] Test multi-word literal matches: `["docker", "compose", "up"]`

### Parameter Matching Tests
- [ ] Test required parameter present: `["greet", "Alice"]` matches
- [ ] Test required parameter missing: `["greet"]` does not match
- [ ] Test optional parameter present: `["greet", "Alice", "formal"]` matches
- [ ] Test optional parameter absent: `["greet", "Alice"]` still matches

### Option Matching Tests
- [ ] Test bool flag with long form: `["deploy", "--force"]`
- [ ] Test bool flag with short form: `["deploy", "-f"]`
- [ ] Test valued option: `["deploy", "--config", "app.json"]`
- [ ] Test valued option with equals: `["deploy", "--config=app.json"]`

### Special Matching Tests
- [ ] Test catch-all captures remaining: `["exec", "echo", "hello", "world"]`
- [ ] Test alias matches: `["bye"]` matches `[NuruRouteAlias("bye")]`
- [ ] Test grouped route: `["docker", "run", "nginx"]` matches group+route
- [ ] Test group option on grouped route: `["docker", "run", "--debug"]`

## Notes

### Implementation Pattern

```csharp
// Use RegisteredRoute from NuruRouteRegistry
RegisteredRoute? route = NuruRouteRegistry.RegisteredRoutes
  .FirstOrDefault(r => r.RequestType == typeof(StatusTestRequest));

CompiledRoute compiled = route.Route;
MatchResult match = compiled.Match(["status"]);
match.IsMatch.ShouldBeTrue();
```

### Test Request Classes

Test request classes with `[NuruRoute]` attributes are defined in the test file. The source generator runs at compile time, so routes are automatically registered when the test assembly loads.
