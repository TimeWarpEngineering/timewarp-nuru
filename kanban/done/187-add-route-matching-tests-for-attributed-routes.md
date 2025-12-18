# Add Route Matching Tests for Attributed Routes

## Description

Create tests that verify routes generated from `[NuruRoute]` attributes correctly match input arguments. These tests use `RouteMatchEngine.Instance.Match()` to verify matching behavior against `EndpointCollection` built from `NuruRouteRegistry`.

## Parent

150-implement-attributed-routes-phase-1

## Dependencies

- Task 186: Test utilities must be created first

## Checklist

### Test File
- [x] Create `tests/timewarp-nuru-analyzers-tests/auto/attributed-route-generator-03-matching.cs`

### Literal Matching Tests
- [x] Test simple literal matches: `["status"]` matches `[NuruRoute("status")]`
- [x] Test wrong literal does not match: `["help"]` does not match `[NuruRoute("status")]`
- [x] Test empty default route matches empty args
- [x] Test multi-word literal matches: `["docker", "compose", "up"]`

### Parameter Matching Tests
- [x] Test required parameter present: `["greet", "Alice"]` matches with IsExactMatch=true
- [x] Test required parameter missing: `["greet"]` is IsViable=true but IsExactMatch=false
- [x] Test optional parameter present: `["greet", "Alice", "formal"]` matches
- [x] Test optional parameter absent: `["greet", "Alice"]` still matches with IsExactMatch=true

### Option Matching Tests
- [x] Test bool flag with long form: `["deploy", "--force"]`
- [x] Test bool flag with short form: `["deploy", "-f"]`
- [x] Test valued option: `["deploy", "--config", "app.json"]`

### Special Matching Tests
- [x] Test catch-all captures remaining: `["exec", "echo", "hello", "world"]`
- [x] Test alias matches: `["bye"]` matches `[NuruRouteAlias("bye")]`
- [x] Test grouped route: `["docker", "run", "nginx"]` matches group+route
- [x] Test group option on grouped route: `["docker", "run", "nginx", "--debug"]`

## Results

All 15 tests pass. The test file verifies that routes generated from `[NuruRoute]` attributes correctly match input arguments using `RouteMatchEngine`.

### Additional Changes

- Renamed `RegisteredRoute.Route` to `RegisteredRoute.CompiledRoute` to match the type name (property names should match their type names)

## Notes

### Implementation Pattern

```csharp
// Create EndpointCollection from registered routes
RegisteredRoute? registeredRoute = NuruRouteRegistry.RegisteredRoutes
  .FirstOrDefault(r => r.RequestType == typeof(StatusTestRequest));

EndpointCollection endpoints = new();
endpoints.Add(new Endpoint
{
  RoutePattern = registeredRoute.Pattern,
  CompiledRoute = registeredRoute.CompiledRoute,
  CommandType = registeredRoute.RequestType,
  Description = registeredRoute.Description
});

// Use RouteMatchEngine to test matching
ParsedInput input = new(["status"], null, true);
IReadOnlyList<RouteMatchState> states = RouteMatchEngine.Instance.Match(input, endpoints);

RouteMatchState? matchState = states.FirstOrDefault();
matchState.ShouldNotBeNull();
matchState.IsViable.ShouldBeTrue();
matchState.IsExactMatch.ShouldBeTrue();
```

### Key Types

- `RouteMatchEngine` - Singleton that performs matching (`RouteMatchEngine.Instance.Match()`)
- `ParsedInput` - Represents tokenized input: `new(completedWords[], partialWord, hasTrailingSpace)`
- `RouteMatchState` - Match result with `IsViable`, `IsExactMatch`, `SegmentsMatched`, `OptionsUsed`
- `EndpointCollection` - Collection of endpoints to match against

### Test Request Classes

Test request classes with `[NuruRoute]` attributes are defined in the test file. The source generator runs at compile time, so routes are automatically registered when the test assembly loads.
