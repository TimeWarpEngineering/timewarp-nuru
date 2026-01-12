# feat: Add DiscoverEndpoints() and Map<T>() for endpoint isolation

## Description

Implemented endpoint discovery API to solve test isolation issues where `[NuruRoute]` endpoint classes from one test file would contaminate other test apps during CI compilation.

## Solution

Endpoints (attributed routes) are no longer automatically included in all apps. New DSL methods control inclusion:

- `.DiscoverEndpoints()` - includes all `[NuruRoute]` endpoint classes from assembly
- `.Map<T>()` - includes a specific endpoint class explicitly

Default behavior: **no endpoints** (test isolation by default).

## API Usage

```csharp
// Real apps - include all [NuruRoute] endpoints
NuruApp.CreateBuilder(args)
  .DiscoverEndpoints()
  .Build();

// Tests - explicit control (isolated by default)
NuruApp.CreateBuilder(args)
  .Map<DeployCommand>()  // Only this endpoint
  .Build();

// Fluent routes unchanged
NuruApp.CreateBuilder(args)
  .Map("deploy {env}").WithHandler(...)
  .Build();
```

## Checklist

- [x] Add DSL stub methods to runtime builder
- [x] Update AppModel with DiscoverEndpoints and ExplicitEndpointTypes properties
- [x] Update IR builder to track discovery state
- [x] Update DSL interpreter to detect and dispatch new methods
- [x] Update generator to filter endpoints based on app's discovery mode
- [x] Update emitter to filter endpoints per-app
- [x] Update generator-11-attributed-routes.cs to use DiscoverEndpoints()
- [x] Verify CI tests pass (no more NURU_R002/R003 cross-contamination)

## Files Modified

- `source/timewarp-nuru-core/builders/nuru-core-app-builder/nuru-core-app-builder.routes.cs`
- `source/timewarp-nuru-analyzers/generators/models/app-model.cs`
- `source/timewarp-nuru-analyzers/generators/ir-builders/ir-app-builder.cs`
- `source/timewarp-nuru-analyzers/generators/ir-builders/abstractions/iir-app-builder.cs`
- `source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs`
- `source/timewarp-nuru-analyzers/generators/nuru-generator.cs`
- `source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs`
- `tests/timewarp-nuru-core-tests/generator/generator-11-attributed-routes.cs`

## Results

- CI tests now run: 456 passed, 9 failed (pre-existing), 2 skipped
- No more NURU_R002/R003 cross-contamination errors
- Test apps are isolated by default

## Related

- Task #351: Merge NuruAnalyzer into NuruGenerator (discovered this need)
- Task #349: Bug - typed repeated options (original investigation)

## Notes

This was implemented as part of task #351 work but represents a significant standalone feature. Task retroactively documented and marked complete.
