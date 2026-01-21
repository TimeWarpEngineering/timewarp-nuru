# Bug: Source Generator Cannot Resolve Services Registered via Extension Methods

## Summary

The Nuru source generator cannot resolve services registered via extension methods in external assemblies. It only performs static analysis of the `ConfigureServices` method body in the consuming project, missing registrations inside called methods.

## Root Cause

The source generator does compile-time static analysis to avoid DI container overhead at runtime. However:
1. It cannot follow method calls (especially extension methods) into other assemblies
2. Even if it could, implementations marked as `internal` cannot be instantiated from the consuming assembly

## Reproduction

```csharp
// External library (MyLibrary)
internal sealed class MyServiceImpl : IMyService { }

public static class ServiceCollectionExtensions
{
  public static void AddMyServices(this IServiceCollection services)
  {
    services.AddSingleton<IMyService, MyServiceImpl>();
  }
}

// Program.cs (consuming project)
NuruApp app = NuruApp.CreateBuilder()
  .ConfigureServices(services => services.AddMyServices())  // Generator can't see inside!
  .DiscoverEndpoints()
  .Build();

// Endpoint
[NuruRoute("test")]
public sealed class TestEndpoint : ICommand<int>
{
  public sealed class Handler(IMyService svc) : ICommandHandler<TestEndpoint, int>
  {
    public ValueTask<int> Handle(...) => ...;  // NullReferenceException!
  }
}
```

## Generated Code (Problematic)

```csharp
// NuruGenerated.g.cs
global::IMyService __svc = default! /* ERROR: Service not registered */;
var __handler = new Handler(__svc);
```

## Checklist

- [ ] Investigate static analysis depth for extension method following
- [ ] Design solution for cross-assembly internal implementations
- [ ] Consider attribute-based service declaration pattern
- [ ] Update documentation with workarounds

## Suggested Solutions

1. **Attribute-based declaration**: Allow endpoints to declare required services:
   ```csharp
   [NuruRoute("test")]
   [RequiresService(typeof(IMyService))]
   public sealed class TestEndpoint : ICommand<int> { }
   ```

2. **Factory delegate pattern**: Support factory registrations the generator can invoke

3. **Assembly metadata**: Ship generators that emit metadata for consuming generators

## Impact

- **Severity**: Blocker for endpoints using services from extension methods
- **Workaround**: Use fluent API (`.Map().WithHandler()`) or inline all registrations in `ConfigureServices`
- **Affected Version**: 3.0.0-beta.30

## Context

Discovered during migration of ccc1-cli from Nuru 2.x fluent DSL to Nuru 3.x Endpoints API.
