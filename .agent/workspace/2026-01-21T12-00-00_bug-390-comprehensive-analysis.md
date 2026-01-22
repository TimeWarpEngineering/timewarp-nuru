# Comprehensive Analysis: Bug #390 - Source Generator Cannot Resolve Services with Dependencies

**Date:** 2026-01-21
**Status:** Analysis Complete
**Severity:** BLOCKER
**Affected Version:** 3.0.0-beta.30

---

## Executive Summary

The TimeWarp.Nuru source generator has **two fundamental limitations** that prevent using the Endpoints API with real-world services:

| Issue | Root Cause | Impact |
|-------|------------|--------|
| **#1: Extension methods not followed** | Static analysis cannot cross assembly boundaries | Cannot use `services.AddMyServices()` pattern |
| **#2: Constructor dependencies not resolved** | Generator only emits `new T()` with no parameters | **BLOCKER**: Any service with constructor deps fails to compile |

**Critical Finding:** Issue #2 is the true blocker. Even when services ARE visible via inline registrations, the generator cannot instantiate them if they have constructor dependencies.

---

## Part 1: Architecture Deep Dive

### 1.1 Source Generator Pipeline

```
Source Code Analysis Pipeline:

┌─────────────────────────┐
│  User's Program.cs      │
│  .ConfigureServices()   │
└───────────┬─────────────┘
            │
            ▼
┌─────────────────────────┐
│  ServiceExtractor.cs    │ ◄── Extracts registrations from lambda/method body
│  Extract()              │     Only recognizes: AddTransient, AddScoped, AddSingleton
└───────────┬─────────────┘
            │ ImmutableArray<ServiceDefinition>
            ▼
┌─────────────────────────┐
│  EndpointExtractor.cs   │ ◄── Extracts handler constructor dependencies
│  ExtractConstructor     │     Creates ParameterBinding.FromService()
│  Dependencies()         │
└───────────┬─────────────┘
            │ HandlerDefinition with ServiceParameters
            ▼
┌─────────────────────────┐
│  InterceptorEmitter.cs  │ ◄── Generates Lazy<T> fields for singletons
│  EmitServiceFields()    │     PROBLEM: Only generates new T() with NO parameters
└───────────┬─────────────┘
            │
            ▼
┌─────────────────────────┐
│  ServiceResolverEmitter │ ◄── Generates service instantiation in handlers
│  Emit()                 │     Uses cached fields or new T() for transients
└───────────┬─────────────┘
            │
            ▼
┌─────────────────────────┐
│  NuruGenerated.g.cs     │ ◄── Final generated code
│  (fails to compile if   │
│   services have deps)   │
└─────────────────────────┘
```

### 1.2 Data Flow for Service Registration

**Input (User Code):**
```csharp
NuruApp.CreateBuilder()
  .ConfigureServices(services =>
  {
    services.AddSingleton<Ccc1StorageSettings>();
    services.AddSingleton<UserDatabaseManager>();  // Requires: Ccc1StorageSettings, ILogger<T>
    services.AddSingleton<ICredentialRepository, SqliteCredentialRepository>();  // Requires: UserDatabaseManager, ILogger<T>
  })
  .DiscoverEndpoints()
  .Build();
```

**Intermediate Model:**
```csharp
ServiceDefinition[]
{
  { ServiceType: "Ccc1StorageSettings", ImplementationType: "Ccc1StorageSettings", Lifetime: Singleton },
  { ServiceType: "UserDatabaseManager", ImplementationType: "UserDatabaseManager", Lifetime: Singleton },
  { ServiceType: "ICredentialRepository", ImplementationType: "SqliteCredentialRepository", Lifetime: Singleton }
}
```

**Generated Code (FAILS):**
```csharp
// InterceptorEmitter.cs line 321-322
private static readonly global::System.Lazy<Ccc1StorageSettings> __svc_Ccc1StorageSettings =
    new(() => new Ccc1StorageSettings());  // OK - parameterless

private static readonly global::System.Lazy<UserDatabaseManager> __svc_UserDatabaseManager =
    new(() => new UserDatabaseManager());  // CS7036: Missing required arguments!

private static readonly global::System.Lazy<SqliteCredentialRepository> __svc_SqliteCredentialRepository =
    new(() => new SqliteCredentialRepository());  // CS7036: Missing required arguments!
```

---

## Part 2: Root Cause Analysis

### 2.1 Issue #1: Extension Methods Not Followed

**Location:** `ServiceExtractor.cs` lines 84-86

```csharp
// Get the method's syntax declaration
SyntaxReference? syntaxRef = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault();
if (syntaxRef is null)
  return [];  // ◄── Returns empty for cross-assembly methods
```

**Why it fails:**
- `DeclaringSyntaxReferences` returns the syntax tree location of a method
- External assembly methods have no syntax references in the current compilation
- Even if they did, `internal` implementations cannot be instantiated from the consuming assembly

**Scope of Impact:**
- Any `services.AddMyLibraryServices()` pattern fails
- Common in library designs: EF Core's `AddDbContext`, Serilog's `AddSerilog`, etc.

### 2.2 Issue #2: Constructor Dependencies Not Resolved (CRITICAL)

**Location:** `InterceptorEmitter.cs` lines 318-323

```csharp
foreach (ServiceDefinition service in cachedServices)
{
  string fieldName = GetServiceFieldName(service.ImplementationTypeName);
  sb.AppendLine(
    $"  private static readonly global::System.Lazy<{service.ImplementationTypeName}> {fieldName} = new(() => new {service.ImplementationTypeName}());");
    //                                                                                                    ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲
    //                                                                                                    PROBLEM: Always parameterless new()
}
```

**Also in:** `ServiceResolverEmitter.cs` lines 85-86

```csharp
// Transient - new instance each time
sb.AppendLine(
  $"{indent}{typeName} {varName} = new {service.ImplementationTypeName}();");
  //                                  ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲
  //                                  PROBLEM: Always parameterless new()
```

**Acknowledgment in Code:**
Line 81 in `ServiceResolverEmitter.cs` contains:
```csharp
// Note: Phase 4 will add constructor dependency resolution for services with dependencies.
```

**Why it's critical:**
- This is not just about cross-assembly services
- Even **inline registrations with public implementations** fail if they have constructor deps
- There is **no workaround** - the fluent API has the same limitation
- Real-world services almost always have dependencies (loggers, settings, other services)

---

## Part 3: Current Capabilities vs. Gaps

### What Currently Works

| Feature | Status | Example |
|---------|--------|---------|
| Inline `AddSingleton<T>()` | ✓ Works | `services.AddSingleton<IFoo, Foo>()` where Foo has no-arg constructor |
| Inline `AddTransient<T>()` | ✓ Works | Same constraint |
| Method groups in same project | ✓ Works | `.ConfigureServices(ConfigureServices)` |
| Built-in services | ✓ Works | `ITerminal`, `IConfiguration`, `NuruApp` |
| `IOptions<T>` binding | ✓ Works | Binds from configuration section |
| `ILogger<T>` injection | ✓ Works | When `AddLogging()` is configured |

### What Doesn't Work

| Feature | Status | Impact |
|---------|--------|--------|
| Extension methods (cross-assembly) | ✗ Fails | Cannot use library registration helpers |
| Services with constructor dependencies | ✗ **BLOCKER** | Most real services fail |
| Nested dependency graphs | ✗ Fails | A → B → C chains impossible |
| Factory registrations | ✗ Fails | No `AddSingleton<IFoo>(() => new Foo(x, y))` |
| Internal implementations | ✗ Fails | Cannot instantiate `internal` types |

---

## Part 4: Real-World Impact Assessment

### Example: CCC1 Client Library

From the kanban task, these are actual services from `Crunchit.CCCOne.Client`:

```csharp
// Dependency Graph:
//
//  Ccc1StorageSettings ─────────────────────┬──► UserDatabaseManager
//  ILogger<UserDatabaseManager> ────────────┘         │
//                                                     │
//  UserDatabaseManager ─────────────────────┬──► SqliteCredentialRepository
//  ILogger<SqliteCredentialRepository> ─────┘         │
//                                                     │
//  UserDatabaseManager ─────────────────────┬──► SqliteAuthenticationRepository
//  ILogger<SqliteAuthenticationRepository> ─┘         │
//                                                     │
//  ... (more cascading dependencies)
```

**Current Generator Output:**
```csharp
// ALL of these fail to compile with CS7036

private static readonly Lazy<UserDatabaseManager> __svc =
    new(() => new UserDatabaseManager());
    // Actual: UserDatabaseManager(Ccc1StorageSettings, ILogger<UserDatabaseManager>)

private static readonly Lazy<SqliteCredentialRepository> __svc2 =
    new(() => new SqliteCredentialRepository());
    // Actual: SqliteCredentialRepository(UserDatabaseManager, ILogger<SqliteCredentialRepository>)
```

**Viability Assessment:**
- Only services with parameterless constructors work
- This excludes virtually all production services
- The `ScientificCalculator` example in samples works only because it has no dependencies

---

## Part 5: Solution Analysis

### Solution 1: Topological Dependency Resolution (Recommended for Issue #2)

**Approach:** Analyze constructor parameters of registered services, build a dependency graph, and emit instantiation code in topological order.

**Implementation Steps:**

1. **Enhance ServiceDefinition** to include constructor parameter info:
   ```csharp
   record ServiceDefinition(
     string ServiceTypeName,
     string ImplementationTypeName,
     ServiceLifetime Lifetime,
     ImmutableArray<ConstructorParameter> ConstructorParameters  // NEW
   );

   record ConstructorParameter(
     string ParameterName,
     string TypeName,
     bool IsBuiltIn  // ILogger<T>, IConfiguration, etc.
   );
   ```

2. **Extend ServiceExtractor** to analyze implementation constructors:
   ```csharp
   private static ImmutableArray<ConstructorParameter> ExtractConstructorParameters(
     INamedTypeSymbol implementationType)
   {
     IMethodSymbol? ctor = implementationType.InstanceConstructors
       .FirstOrDefault(c => c.DeclaredAccessibility == Accessibility.Public);

     if (ctor is null) return [];

     return [.. ctor.Parameters.Select(p => new ConstructorParameter(
       p.Name,
       p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
       IsBuiltInType(p.Type)))];
   }
   ```

3. **Add DependencyGraphBuilder** for topological sorting:
   ```csharp
   internal static class DependencyGraphBuilder
   {
     public static ImmutableArray<ServiceDefinition> TopologicalSort(
       ImmutableArray<ServiceDefinition> services)
     {
       // Build adjacency list
       // Detect cycles (report diagnostic)
       // Return services in instantiation order
     }
   }
   ```

4. **Modify InterceptorEmitter** to use sorted order:
   ```csharp
   private static void EmitServiceFields(StringBuilder sb, IEnumerable<ServiceDefinition> services)
   {
     var sorted = DependencyGraphBuilder.TopologicalSort(services.ToImmutableArray());

     foreach (var service in sorted)
     {
       EmitServiceWithDependencies(sb, service, sorted);
     }
   }

   private static void EmitServiceWithDependencies(StringBuilder sb, ServiceDefinition service, ...)
   {
     string args = string.Join(", ", service.ConstructorParameters.Select(p =>
       IsBuiltIn(p) ? GetBuiltInSource(p) : GetServiceFieldAccess(p, sorted)));

     sb.AppendLine($"  private static readonly Lazy<{service.ImplementationTypeName}> {fieldName} = ");
     sb.AppendLine($"    new(() => new {service.ImplementationTypeName}({args}));");
   }
   ```

**Generated Output Example:**
```csharp
// Services sorted in topological order
// Level 0: No dependencies
private static readonly Lazy<Ccc1StorageSettings> __svc_Ccc1StorageSettings =
    new(() => new Ccc1StorageSettings());

// Level 1: Depends on Level 0
private static readonly Lazy<UserDatabaseManager> __svc_UserDatabaseManager =
    new(() => new UserDatabaseManager(
        __svc_Ccc1StorageSettings.Value,
        __loggerFactory.CreateLogger<UserDatabaseManager>()));

// Level 2: Depends on Level 1
private static readonly Lazy<SqliteCredentialRepository> __svc_SqliteCredentialRepository =
    new(() => new SqliteCredentialRepository(
        __svc_UserDatabaseManager.Value,
        __loggerFactory.CreateLogger<SqliteCredentialRepository>()));
```

**Pros:**
- Preserves compile-time DI philosophy
- No runtime container overhead
- Works with any depth of dependency graph
- AOT-friendly

**Cons:**
- Requires all dependencies to be registered
- Cannot handle circular dependencies (but these are a design smell anyway)
- Implementation type must be visible (not `internal` in another assembly)

**Complexity:** Medium-High

---

### Solution 2: Attribute-Based Service Declaration (Recommended for Issue #1)

**Approach:** Declare required services via attributes, allowing cross-assembly registration discovery.

**For Library Authors:**
```csharp
// In MyLibrary.ServiceCollectionExtensions
[NuruServiceRegistration]
public static IServiceCollection AddMyServices(this IServiceCollection services)
{
    // Attribute tells generator what this method registers
    services.AddSingleton<IMyService, MyServiceImpl>();
    return services;
}

// Attribute definition
[AttributeUsage(AttributeTargets.Method)]
public sealed class NuruServiceRegistrationAttribute : Attribute
{
    public Type[]? Services { get; set; }  // Optional explicit declaration
}
```

**For Endpoint Authors:**
```csharp
[NuruRoute("test")]
[RequiresService(typeof(IMyService))]  // Explicit dependency declaration
public sealed class TestEndpoint : ICommand<int>
{
    public sealed class Handler(IMyService svc) : ICommandHandler<TestEndpoint, int>
    {
        // ...
    }
}
```

**Generator Enhancement:**
1. Scan all referenced assemblies for `[NuruServiceRegistration]` methods
2. Extract registrations from method bodies (if source available) or from attribute metadata
3. Validate that `[RequiresService]` dependencies have matching registrations

**Pros:**
- Works across assembly boundaries
- Self-documenting dependencies
- Enables better tooling (IDE warnings for missing dependencies)

**Cons:**
- Requires library authors to opt-in with attributes
- Still cannot instantiate `internal` types directly

**Complexity:** Medium

---

### Solution 3: Factory Method Pattern (For Internal Types)

**Approach:** Support factory methods that handle internal implementation instantiation.

```csharp
// Library exposes factory
public static class MyServiceFactory
{
    [NuruServiceFactory(typeof(IMyService), ServiceLifetime.Singleton)]
    public static IMyService Create(ILogger<IMyService> logger) => new MyServiceImpl(logger);
}

// Generator generates:
private static readonly Lazy<IMyService> __svc_IMyService =
    new(() => MyServiceFactory.Create(__loggerFactory.CreateLogger<IMyService>()));
```

**Pros:**
- Handles `internal` implementations
- Library controls instantiation
- Factory can include validation/initialization logic

**Cons:**
- Requires library changes
- More boilerplate

**Complexity:** Medium

---

### Solution 4: Hybrid Static/Runtime DI (Last Resort)

**Approach:** Use static DI for recognized patterns, fall back to MS.Extensions.DependencyInjection for unrecognized.

```csharp
// Generated code
private static ServiceProvider? _fallbackProvider;

private static T ResolveOrFallback<T>(Func<T>? staticFactory)
{
    if (staticFactory is not null) return staticFactory();
    _fallbackProvider ??= BuildFallbackProvider();
    return _fallbackProvider.GetRequiredService<T>();
}
```

**Pros:**
- Maximum compatibility
- Works with any registration pattern

**Cons:**
- **Defeats compile-time DI purpose**
- Runtime overhead for fallback cases
- AOT complications
- Increased binary size

**Complexity:** High

**Recommendation:** Only as escape hatch, not primary solution.

---

## Part 6: Implementation Roadmap

### Phase 1: Constructor Dependency Resolution (BLOCKER FIX)

**Priority:** Immediate
**Estimated Effort:** 3-5 days

1. Extend `ServiceDefinition` with constructor parameters
2. Add constructor analysis to `ServiceExtractor`
3. Implement `DependencyGraphBuilder` for topological sorting
4. Modify `InterceptorEmitter.EmitServiceFields()` to emit with dependencies
5. Modify `ServiceResolverEmitter` for transient services
6. Add diagnostics for:
   - Missing service registrations
   - Circular dependencies
   - Inaccessible types

### Phase 2: Attribute-Based Declaration

**Priority:** High
**Estimated Effort:** 2-3 days

1. Create `NuruServiceRegistrationAttribute` in annotations package
2. Create `RequiresServiceAttribute` for endpoints
3. Extend `ServiceExtractor` to scan for attributed methods
4. Add diagnostic validation for `[RequiresService]` dependencies
5. Update documentation

### Phase 3: Factory Method Support

**Priority:** Medium
**Estimated Effort:** 1-2 days

1. Create `NuruServiceFactoryAttribute`
2. Extend emitter to invoke factory methods
3. Support factory dependency injection

### Phase 4: Documentation & Migration Guide

**Priority:** High
**Estimated Effort:** 1 day

1. Document service registration patterns
2. Document workarounds for current limitations
3. Migration guide for upgrading from 2.x

---

## Part 7: Diagnostic Improvements

### New Diagnostics Needed

| Code | Severity | Message |
|------|----------|---------|
| `NURU030` | Error | Service `{TypeName}` is required by handler but not registered |
| `NURU031` | Error | Circular dependency detected: {ServiceA} → {ServiceB} → {ServiceA} |
| `NURU032` | Error | Cannot instantiate `{TypeName}` - type is not accessible |
| `NURU033` | Warning | Service `{TypeName}` has unresolvable dependency `{DepType}` - will be null |
| `NURU034` | Info | Consider using `[NuruServiceFactory]` for internal implementation `{TypeName}` |

---

## Part 8: Testing Strategy

### Unit Tests

```csharp
// Test: Constructor dependency resolution
[Fact]
public async Task Should_resolve_service_with_constructor_dependencies()
{
    // Arrange - service with deps
    NuruApp app = NuruApp.CreateBuilder()
        .ConfigureServices(services =>
        {
            services.AddSingleton<Settings>();
            services.AddSingleton<DatabaseManager>();  // Requires Settings, ILogger
        })
        .DiscoverEndpoints()
        .Build();

    // Act & Assert - should compile and run
    int result = await app.RunAsync(["my-command"]);
    result.ShouldBe(0);
}

// Test: Topological order
[Fact]
public void Should_sort_services_in_dependency_order()
{
    var services = ImmutableArray.Create(
        new ServiceDefinition("C", "C", Singleton, [new("B", ...)]),  // C depends on B
        new ServiceDefinition("A", "A", Singleton, []),               // A has no deps
        new ServiceDefinition("B", "B", Singleton, [new("A", ...)])   // B depends on A
    );

    var sorted = DependencyGraphBuilder.TopologicalSort(services);

    sorted[0].ServiceTypeName.ShouldBe("A");  // Level 0
    sorted[1].ServiceTypeName.ShouldBe("B");  // Level 1
    sorted[2].ServiceTypeName.ShouldBe("C");  // Level 2
}

// Test: Circular dependency detection
[Fact]
public void Should_report_circular_dependency()
{
    var services = ImmutableArray.Create(
        new ServiceDefinition("A", "A", Singleton, [new("B", ...)]),  // A → B
        new ServiceDefinition("B", "B", Singleton, [new("A", ...)])   // B → A (cycle!)
    );

    var diagnostics = DependencyGraphBuilder.Validate(services);

    diagnostics.ShouldContain(d => d.Id == "NURU031");
}
```

---

## Part 9: Workarounds (Current Version)

Until fixes are implemented, users have these options:

### Workaround 1: Parameterless Constructors with Property Injection

```csharp
// Instead of constructor injection
public class MyService
{
    public MyService() { }  // Parameterless

    public ILogger<MyService>? Logger { get; set; }  // Set after construction
    public Settings? Settings { get; set; }
}

// Configure post-construction (not ideal, but works)
```

**Downside:** Violates DI best practices, nullable properties, no compile-time safety.

### Workaround 2: Use Fluent API Instead of Endpoints (Limited)

The fluent API has the same limitation for services with dependencies.

**Verdict:** Not a workaround.

### Workaround 3: Avoid Services with Dependencies

Only use services that have parameterless constructors.

**Downside:** Unrealistic for any non-trivial application.

### Workaround 4: Wait for Fix

The most pragmatic option for production use cases.

---

## Part 10: References

### Source Files Analyzed

| File | Lines | Purpose |
|------|-------|---------|
| `service-extractor.cs` | 449 | Service registration detection |
| `service-resolver-emitter.cs` | 295 | Service instantiation code gen |
| `interceptor-emitter.cs` | 800+ | Main code generation |
| `endpoint-extractor.cs` | 760 | Handler dependency extraction |
| `nuru-generator.cs` | 350+ | Generator orchestration |

### Related Kanban Tasks

- **#390** - This bug (current analysis)
- **#292** - Emit static service injection (completed, but no constructor deps)
- **#294** - REPL scoping for SessionScoped/CommandScoped lifetimes
- **#308** - Support method group references (completed)

### External References

- Roslyn Source Generators documentation
- MS.Extensions.DependencyInjection patterns
- Topological sorting algorithms (Kahn's algorithm)

---

## Conclusion

**The #1 priority is fixing Issue #2 (constructor dependency resolution).** This is a true blocker that makes the Endpoints API unusable for any service with dependencies - which is virtually all production services.

Issue #1 (extension method following) is important but has workarounds (inline registrations). Issue #2 has **no workaround**.

The recommended solution is **topological dependency resolution** (Solution 1), which preserves the compile-time DI philosophy while enabling real-world service graphs. This should be followed by **attribute-based declaration** (Solution 2) to address cross-assembly scenarios.

**Estimated total effort:** 5-8 days for core fixes, plus documentation.
