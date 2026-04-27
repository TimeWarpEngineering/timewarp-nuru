# Issue #212: FreeSql Repository DI Pattern Analysis

**Date:** 2026-03-25  
**Issue:** [TimeWarpEngineering/timewarp-nuru#212](https://github.com/TimeWarpEngineering/timewarp-nuru/issues/212)  
**Status:** Analysis Complete

---

## Executive Summary

FreeSql's `AddFreeRepository()` extension method registers open generic repository types (`IBaseRepository<>`, `IBaseRepository<,>`) that Nuru's source-generated DI cannot analyze. The core problem is that extension methods are opaque to source generators, and FreeSql's implementation uses runtime reflection for assembly scanning and internal implementation types. This report analyzes the problem in depth and proposes multiple solution approaches with trade-offs.

---

## Scope

This analysis covers:

1. **Nuru's DI source generator architecture** - How it analyzes service registrations
2. **FreeSql's repository DI pattern** - What `AddFreeRepository()` does internally
3. **The compatibility gap** - Why source-gen DI cannot handle this pattern
4. **Proposed solutions** - Multiple approaches with trade-offs

---

## Methodology

- Read Nuru source generator code (`service-extractor.cs`, `service-validator.cs`, `interceptor-emitter.cs`)
- Read FreeSql repository implementation (`DependencyInjection.cs`, `IBaseRepository.cs`, `BaseRepository.cs`, `DefaultRepository.cs`)
- Analyzed the diagnostic descriptors (NURU050-NURU056)
- Explored the generated code emission patterns

---

## Findings

### 1. Nuru's DI Source Generator Architecture

#### Service Registration Detection

The `ServiceExtractor` class in `service-extractor.cs` analyzes `ConfigureServices()` calls:

```csharp
// Only these methods are recognized as analyzable
private static readonly HashSet<string> AnalyzableMethods =
  ["AddTransient", "AddScoped", "AddSingleton"];
```

**Key limitation:** Any method call NOT in this set is tracked as an `ExtensionMethodCall` and triggers NURU052 warning.

#### Current Whitelist

Only two extension methods are whitelisted (no NURU052 warning):

```csharp
private static readonly HashSet<string> WhitelistedExtensionMethods =
  new(StringComparer.Ordinal) { "AddLogging", "AddHttpClient" };
```

These have special handling:
- `AddLogging`: Lambda body extracted for `LoggerFactory.Create()`
- `AddHttpClient`: Type arguments and configuration lambda extracted

#### Generated Code Structure

For source-gen DI, the generator emits:

1. **Static service fields** for Singleton/Scoped services
2. **`EnsureServicesInitialized()`** method that instantiates services in topological order
3. **Inline instantiation** for Transient services

```csharp
// Example generated code
private static MyServiceImpl? __svc_MyServiceImpl;

private static void EnsureServicesInitialized(NuruApp app, IConfigurationRoot configuration)
{
  __svc_MyServiceImpl = new MyServiceImpl(__fw_ITerminal);
}
```

### 2. FreeSql's Repository DI Pattern

#### What `AddFreeRepository()` Does

From `FreeSql.DbContext/Repository/Extensions/DependencyInjection.cs`:

```csharp
public static IServiceCollection AddFreeRepository(this IServiceCollection services, params Assembly[] assemblies)
{
    // Register open generic repositories for single-entity (no key type)
    services.AddScoped(typeof(IBaseRepository<>), typeof(GuidRepository<>));
    services.AddScoped(typeof(BaseRepository<>), typeof(GuidRepository<>));

    // Register open generic repositories for entity + key type
    services.AddScoped(typeof(IBaseRepository<,>), typeof(DefaultRepository<,>));
    services.AddScoped(typeof(BaseRepository<,>), typeof(DefaultRepository<,>));

    // Scan assemblies for custom repository implementations
    if (assemblies?.Any() == true)
        foreach (var asse in assemblies)
            foreach (var repo in asse.GetTypes()
                .Where(a => a.IsAbstract == false && typeof(IBaseRepository).IsAssignableFrom(a)))
                services.AddScoped(repo);

    return services;
}
```

**Key observations:**

1. **Open generic registration** using `typeof(IBaseRepository<>)` syntax
2. **Internal implementation types** - `GuidRepository<>` is `internal`
3. **Runtime assembly scanning** for custom repositories
4. **Constructor dependency on `IFreeSql`**

#### Repository Interface Hierarchy

```
IBaseRepository (non-generic marker)
    └── IBaseRepository<TEntity> : IBaseRepository
            └── IBaseRepository<TEntity, TKey> : IBaseRepository<TEntity>
```

#### Repository Implementation Hierarchy

```
BaseRepository<TEntity> : IBaseRepository<TEntity>
    └── BaseRepository<TEntity, TKey> : BaseRepository<TEntity>, IBaseRepository<TEntity, TKey>
            └── DefaultRepository<TEntity, TKey> : BaseRepository<TEntity, TKey>
            └── GuidRepository<TEntity> : BaseRepository<TEntity, Guid> (internal)
```

#### Constructor Dependencies

```csharp
// DefaultRepository constructor
public DefaultRepository(IFreeSql fsql) : base(fsql) { }
public DefaultRepository(IFreeSql fsql, UnitOfWorkManager uowManger) : base(uowManger?.Orm ?? fsql)
{
    uowManger?.Binding(this);
}

// GuidRepository constructor (internal class)
public GuidRepository(IFreeSql fsql) : base(fsql) { }
public GuidRepository(IFreeSql fsql, UnitOfWorkManager uowManger) : base(uowManger?.Orm ?? fsql)
{
    uowManger?.Binding(this);
}
```

### 3. The Compatibility Gap

#### Problem 1: Extension Method Opacity

`AddFreeRepository` is an extension method. Nuru's source generator cannot see what services it registers internally.

**Current behavior:**
- NURU052 warning: "Cannot analyze registrations inside 'AddFreeRepository()'"
- NURU050 error: "Handler requires service 'IBaseRepository<RemoteRepo>' but it is not registered"

#### Problem 2: Open Generic Registration

FreeSql uses `typeof(IBaseRepository<>)` syntax:

```csharp
services.AddScoped(typeof(IBaseRepository<>), typeof(GuidRepository<>));
```

Nuru's source generator currently only handles:
- Generic method syntax: `AddScoped<IFoo, Foo>()`
- `typeof()` syntax for closed generics: `AddScoped(typeof(IFoo), typeof(Foo))`

**Open generics require runtime resolution** because the concrete type (`IBaseRepository<RemoteRepo>`) is not known at compile time.

#### Problem 3: Internal Implementation Types

`GuidRepository<>` is an `internal` class:

```csharp
class GuidRepository<TEntity> : BaseRepository<TEntity, Guid> where TEntity : class
```

This triggers NURU054: "Cannot instantiate internal type 'GuidRepository<RemoteRepo>' from generated code."

#### Problem 4: Constructor Dependencies

Repositories require `IFreeSql` in their constructor. The source generator would need to:
1. Know that `IFreeSql` is registered as Singleton
2. Generate code to pass `IFreeSql` to repository constructors

### 4. Why This Is Hard

| Challenge | Source-Gen DI | Runtime DI |
|-----------|---------------|------------|
| Extension method analysis | Cannot see inside | Executes at runtime |
| Open generic registration | Cannot enumerate all closed types | `MakeGenericType()` at runtime |
| Internal types | Cannot access from generated assembly | Reflection works |
| Assembly scanning | Cannot know all types at compile time | `Assembly.GetTypes()` at runtime |

---

## Proposed Solutions

### Solution A: Whitelist + Known Service Registration (Recommended)

**Approach:** Add `AddFreeRepository` to the whitelist and provide a mechanism to declare what services it registers.

#### Implementation

1. **Add to whitelist:**
```csharp
private static readonly HashSet<string> WhitelistedExtensionMethods =
  new(StringComparer.Ordinal) { "AddLogging", "AddHttpClient", "AddFreeRepository" };
```

2. **Create a registration hint attribute:**
```csharp
// In FreeSql (or a shim package)
[AttributeUsage(AttributeTargets.Method)]
public class RegistersServicesAttribute : Attribute
{
    public Type[] ServiceTypes { get; }
    public Type[] ImplementationTypes { get; }
    public ServiceLifetime Lifetime { get; }
}
```

3. **Or use a simpler convention-based approach:**
```csharp
// User explicitly declares what AddFreeRepository registers
.ConfigureServices(services =>
{
    services.AddSingleton<IFreeSql>(fsql);
    services.AddFreeRepository(typeof(ServiceRegistration).Assembly);
})
.KnownService<IBaseRepository<RemoteRepo>>() // Declare known service
```

**Pros:**
- Minimal changes to Nuru
- Works with existing FreeSql code
- User has explicit control

**Cons:**
- Requires user to manually declare services
- Doesn't solve the internal type problem

### Solution B: Runtime DI Fallback for Specific Services

**Approach:** Allow marking certain services as "runtime-resolved" while keeping others source-generated.

#### Implementation

```csharp
.ConfigureServices(services =>
{
    services.AddSingleton<IFreeSql>(fsql);
    services.AddFreeRepository(typeof(ServiceRegistration).Assembly);
})
.UseRuntimeDIFor<IBaseRepository<RemoteRepo>>() // This service uses runtime DI
// Other services still use source-gen DI
```

**Generated code:**
```csharp
// Source-gen services
private static MyService __svc_MyService;

// Runtime DI services resolved from container
IBaseRepository<RemoteRepo> remoteRepoRepository = 
  GetServiceProvider(app).GetRequiredService<IBaseRepository<RemoteRepo>>();
```

**Pros:**
- Granular control
- Keeps source-gen benefits for other services
- Works with any extension method

**Cons:**
- More complex generated code
- Requires maintaining both DI paths

### Solution C: Full Runtime DI for Apps Using Extension Methods

**Approach:** When `AddFreeRepository` (or any non-whitelisted extension method) is detected, automatically switch to runtime DI.

**Current behavior:** NURU052 warning suggests using `UseMicrosoftDependencyInjection()`.

**Enhanced behavior:**
```csharp
// Auto-detect and switch
if (extensionMethods.Any(e => !WhitelistedExtensionMethods.Contains(e.MethodName)))
{
    // Emit runtime DI code instead of source-gen DI
}
```

**Pros:**
- Zero user friction
- Works with any extension method
- Already partially implemented

**Cons:**
- Loses source-gen DI benefits for entire app
- Slower startup

### Solution D: FreeSql Source-Gen Friendly API (Requires FreeSql Changes)

**Approach:** Create a source-generator-friendly alternative to `AddFreeRepository`.

#### Implementation in FreeSql

```csharp
// New attribute for source-gen discovery
[Repository(ServiceLifetime = ServiceLifetime.Scoped)]
public class RemoteRepo { }

// Or explicit registration
public static IServiceCollection AddFreeRepositorySourceGen(
    this IServiceCollection services,
    params Type[] entityTypes)
{
    foreach (var entityType in entityTypes)
    {
        // Register closed generic types explicitly
        var repoInterface = typeof(IBaseRepository<>).MakeGenericType(entityType);
        var repoImpl = typeof(GuidRepository<>).MakeGenericType(entityType);
        services.AddScoped(repoInterface, repoImpl);
    }
    return services;
}
```

**Usage in Nuru app:**
```csharp
.ConfigureServices(services =>
{
    services.AddSingleton<IFreeSql>(fsql);
    services.AddFreeRepositorySourceGen<RemoteRepo, OtherEntity>();
})
```

**Pros:**
- Source-gen can analyze closed generics
- No internal type issues (if FreeSql makes types public)
- Clean separation

**Cons:**
- Requires FreeSql changes
- User must list all entity types explicitly
- Loses assembly scanning convenience

### Solution E: Hybrid Approach (Best of Both Worlds)

**Approach:** Combine whitelist + runtime DI fallback + explicit service declaration.

#### Implementation

1. **Whitelist `AddFreeRepository`** (suppress NURU052)
2. **Add `[RuntimeDI]` attribute** for services that must use runtime resolution
3. **Generate hybrid code:**
   - Source-gen DI for analyzable services
   - Runtime DI for marked services

```csharp
// User code
.ConfigureServices(services =>
{
    services.AddSingleton<IFreeSql>(fsql);
    services.AddFreeRepository(typeof(ServiceRegistration).Assembly);
})
// Mark repository as runtime-resolved
.RuntimeService<IBaseRepository<RemoteRepo>>()
```

**Generated code:**
```csharp
// Source-gen services (fast path)
private static IFreeSql __svc_IFreeSql;

// Runtime services (flexible path)
private IBaseRepository<RemoteRepo> GetRemoteRepoRepository()
{
    return GetServiceProvider(app).GetRequiredService<IBaseRepository<RemoteRepo>>();
}
```

**Pros:**
- Best performance for source-gen services
- Full compatibility for runtime services
- Explicit user control

**Cons:**
- Most complex implementation
- Requires careful lifetime management

---

## Recommendations

### Short-Term (Immediate Fix)

1. **Add `AddFreeRepository` to whitelist** to suppress NURU052 warning
2. **Improve NURU052 diagnostic message** to provide clear migration path:
   ```
   NURU052: Cannot analyze registrations inside 'AddFreeRepository()'.
   FreeSql repositories require runtime DI. Add .UseMicrosoftDependencyInjection() 
   to your app builder, or use .RuntimeService<IBaseRepository<T>>() for hybrid mode.
   ```

### Medium-Term (Enhanced Compatibility)

1. **Implement Solution E (Hybrid Approach)**:
   - Add `.RuntimeService<T>()` method to Nuru DSL
   - Generate hybrid code that uses runtime DI for marked services
   - Keep source-gen DI for all other services

2. **Create a FreeSql integration package**:
   - `TimeWarp.Nuru.FreeSql` package
   - Provides `.AddFreeSqlRepositories()` extension that works with Nuru's source-gen
   - Includes analyzers for FreeSql-specific patterns

### Long-Term (Ecosystem Solution)

1. **Propose source-gen friendly API to FreeSql**:
   - Open an issue/PR with FreeSql
   - Suggest `[Repository]` attribute for compile-time discovery
   - Suggest public repository implementations (or `InternalsVisibleTo`)

2. **Create a general "extension method metadata" mechanism**:
   - Allow library authors to declare what their extension methods register
   - Could be a Roslyn analyzer that reads attributes
   - Would benefit entire .NET ecosystem

---

## Implementation Priority

| Priority | Solution | Effort | Impact |
|----------|----------|--------|--------|
| 1 | Whitelist + improved diagnostic | Low | Medium |
| 2 | Hybrid approach (Solution E) | Medium | High |
| 3 | FreeSql integration package | Medium | High |
| 4 | Propose FreeSql changes | High | Very High |

---

## Code References

### Nuru Source Generator

| File | Purpose |
|------|---------|
| `source/timewarp-nuru-analyzers/generators/extractors/service-extractor.cs` | Extracts service registrations from `ConfigureServices()` |
| `source/timewarp-nuru-analyzers/validation/service-validator.cs` | Validates services and produces diagnostics |
| `source/timewarp-nuru-analyzers/diagnostics/diagnostic-descriptors.service.cs` | Defines NURU050-NURU056 diagnostics |
| `source/timewarp-nuru-analyzers/generators/emitters/service-resolver-emitter.cs` | Emits service resolution code |
| `source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs` | Main emitter for generated code |

### FreeSql Repository

| File | Purpose |
|------|---------|
| `FreeSql.DbContext/Repository/Extensions/DependencyInjection.cs` | `AddFreeRepository` extension method |
| `FreeSql.DbContext/Repository/Repository/IBaseRepository.cs` | Repository interfaces |
| `FreeSql.DbContext/Repository/Repository/BaseRepository.cs` | Abstract base implementation |
| `FreeSql.DbContext/Repository/Repository/DefaultRepository.cs` | Default and Guid repository implementations |

---

## References

- [GitHub Issue #212](https://github.com/TimeWarpEngineering/timewarp-nuru/issues/212)
- [FreeSql Repository Documentation](https://github.com/dotnetcore/FreeSql/wiki/Repository)
- [Nuru Source Generator Architecture](./2025-12-25T12-00-00_v2-fluent-dsl-design.md)
