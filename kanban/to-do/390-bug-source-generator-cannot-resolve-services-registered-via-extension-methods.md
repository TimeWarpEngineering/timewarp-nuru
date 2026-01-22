# Bug: Source Generator Cannot Instantiate Services with Constructor Dependencies

## Blocked By

- **#392**: Add UseMicrosoftDependencyInjection() DSL Method (immediate workaround)
- **#391**: Epic - Full DI Support (complete solution)

## Summary

The Nuru source generator has **two fundamental limitations** that prevent using the Endpoints API with real-world services:

1. **Cannot follow extension methods**: It only performs static analysis of the `ConfigureServices` method body, missing registrations inside called methods
2. **Cannot instantiate services with constructor dependencies**: Even when services ARE visible (via inline registrations with public implementations), the generator only emits `new ServiceType()` without parameters

## Root Cause

The source generator does compile-time static analysis to avoid DI container overhead at runtime. However:
1. It cannot follow method calls (especially extension methods) into other assemblies
2. Even if it could, implementations marked as `internal` cannot be instantiated from the consuming assembly
3. **CRITICAL**: The generator has no mechanism to resolve constructor dependencies - it only generates parameterless `new` calls

## Reproduction

### Issue 1: Extension Methods Not Followed

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

### Issue 2: Constructor Dependencies Not Resolved

Even after making implementations `public` and adding inline registrations:

```csharp
// Program.cs - Inline registrations visible to generator
.ConfigureServices(services =>
{
  services.AddSingleton<Ccc1StorageSettings>();
  services.AddSingleton<UserDatabaseManager>();
  services.AddSingleton<ICredentialRepository, SqliteCredentialRepository>();
  // ... more services
})
```

The generator emits:

```csharp
// NuruGenerated.g.cs - FAILS TO COMPILE
private static readonly Lazy<UserDatabaseManager> __svc_UserDatabaseManager =
    new(() => new UserDatabaseManager());  // CS7036: Missing required arguments!

// UserDatabaseManager constructor requires:
// public UserDatabaseManager(Ccc1StorageSettings settings, ILogger<UserDatabaseManager> logger)
```

## Generated Code (Problematic)

### When service not registered:
```csharp
global::IMyService __svc = default! /* ERROR: Service not registered */;
var __handler = new Handler(__svc);
```

### When service IS registered but has constructor dependencies:
```csharp
// Generator assumes parameterless constructor
private static readonly Lazy<SqliteCredentialRepository> __svc =
    new(() => new SqliteCredentialRepository());  // CS7036!

// But actual constructor:
// public SqliteCredentialRepository(UserDatabaseManager dbManager, ILogger<SqliteCredentialRepository> logger)
```

## Checklist

- [ ] Investigate static analysis depth for extension method following
- [ ] Design solution for cross-assembly internal implementations
- [ ] **NEW**: Design constructor dependency resolution mechanism
- [ ] Consider attribute-based service declaration pattern
- [ ] Update documentation with workarounds

## Suggested Solutions

1. **Attribute-based declaration**: Allow endpoints to declare required services:
   ```csharp
   [NuruRoute("test")]
   [RequiresService(typeof(IMyService))]
   public sealed class TestEndpoint : ICommand<int> { }
   ```

2. **Factory delegate pattern**: Support factory registrations the generator can invoke:
   ```csharp
   services.AddSingleton<IMyService>(() => new MyServiceImpl(dep1, dep2));
   ```

3. **Assembly metadata**: Ship generators that emit metadata for consuming generators

4. **Topological instantiation order**: Analyze constructor dependencies and emit `new` calls in dependency order:
   ```csharp
   // Generated - resolve dependency graph
   var settings = new Ccc1StorageSettings();
   var logger = NullLogger<UserDatabaseManager>.Instance; // or configured logger
   var dbManager = new UserDatabaseManager(settings, logger);
   var credRepo = new SqliteCredentialRepository(dbManager, ...);
   ```

5. **Optional DI bridge**: For complex dependency graphs, allow opt-in to a lightweight DI resolver at runtime

## Impact

- **Severity**: **BLOCKER** for any endpoint using services with constructor dependencies
- **Workaround**: ~~Use fluent API~~ **No workaround** - fluent API also cannot inject services with dependencies
- **Only viable pattern**: Services with parameterless constructors (like the `ScientificCalculator` example)
- **Affected Version**: 3.0.0-beta.30

## Context

Discovered during migration of ccc1-cli from Nuru 2.x fluent DSL to Nuru 3.x Endpoints API.

### Real-World Service Dependencies Encountered

The following services from `Crunchit.CCCOne.Client` all have constructor dependencies:

| Service | Constructor Dependencies |
|---------|-------------------------|
| `UserDatabaseManager` | `Ccc1StorageSettings`, `ILogger<T>` |
| `SqliteCredentialRepository` | `UserDatabaseManager`, `ILogger<T>` |
| `SqliteAuthenticationRepository` | `UserDatabaseManager`, `ILogger<T>` |
| `SqliteSessionDataRepository` | `UserDatabaseManager`, `ILogger<T>` |
| `CurrentUserService` | `Ccc1StorageSettings`, `ILogger<T>` |
| `SessionService` | `ServiceHttpClient`, `ILogger<T>`, `ITokenService`, `ISessionDataRepository` |
| `ServiceHttpClient` | `HttpClient`, `ILogger<T>`, `IOptions<ClientOptions>` |

This is a typical pattern for any non-trivial service library.
