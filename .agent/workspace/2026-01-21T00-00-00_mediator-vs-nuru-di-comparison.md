# Mediator vs Nuru: Source Generator DI Approaches

## Executive Summary

The Mediator library takes a **hybrid approach**: it generates DI registrations at compile-time but resolves handlers at runtime via MS DI ServiceProvider. Nuru attempts **pure compile-time static DI** with direct `new T()` instantiation. The key difference explains why Mediator handles external assemblies effortlessly while Nuru struggles.

## Mediator's Approach (Hybrid)

### How It Works

1. **Generator discovers handlers** by scanning assemblies for types implementing handler interfaces (`IRequestHandler<,>`, etc.)

2. **Generates `AddMediator` extension method** that registers handlers with MS DI:
   ```csharp
   // Generated code in Mediator.g.cs
   public static IServiceCollection AddMediator(this IServiceCollection services, ...)
   {
       services.Add(new ServiceDescriptor(
           typeof(IRequestHandler<Ping, Pong>), 
           typeof(PingHandler), 
           ServiceLifetime.Singleton));
       
       services.Add(new ServiceDescriptor(
           typeof(Mediator), 
           sp => new Mediator(...),  // Factory delegate
           ServiceLifetime.Singleton));
       
       return services;
   }
   ```

3. **At runtime**, handlers come from DI container:
   ```csharp
   // Handler instantiation at runtime
   var handler = serviceProvider.GetRequiredService<IRequestHandler<Ping, Pong>>();
   ```

### Why External Assemblies Work

- Generator discovers handlers via **assembly reference graph traversal** (BFS up to depth 3)
- MS DI handles instantiation at runtime with proper visibility
- `internal` types are accessible to DI because DI runs within the application's context

### Trade-offs

| Benefit | Cost |
|---------|------|
| Simple generator logic | Runtime DI overhead (minimal for Singleton) |
| Works with any registration pattern | Slightly more allocations |
| Handles internal types automatically | Requires DI container at runtime |
| Composable with other MS DI services | Less "pure" AOT optimization |

---

## Nuru's Current Approach (Pure Static DI)

### How It Works

1. **Generator parses `ConfigureServices`** looking for `AddSingleton<T, Impl>()` calls

2. **Extracts service definitions** with implementation type names

3. **Generates direct instantiation**:
   ```csharp
   // Generated code in NuruGenerated.g.cs
   private static readonly Lazy<MyService> __svc_MyService = 
       new(() => new MyService());  // Direct 'new' - needs implementation type!
   
   var handler = new Handler(__svc_MyService.Value);
   ```

### Why External Assemblies Fail

1. **Cannot follow extension methods** into external assemblies (no source access)
2. **Cannot instantiate `internal` types** from consuming assembly
3. **Needs implementation type name** at compile-time for `new T()` generation

### The Problem in Code

In `ServiceExtractor.cs:183-189`:
```csharp
ServiceLifetime? lifetime = methodName switch
{
    "AddTransient" => ServiceLifetime.Transient,
    "AddScoped" => ServiceLifetime.Scoped,
    "AddSingleton" => ServiceLifetime.Singleton,
    _ => null  // <-- Custom extension methods: null, silently ignored
};
```

If extension method is in another assembly:
```csharp
// Consuming project
.ConfigureServices(s => s.AddMyServices())  // Generator can't see this!
```

Generator tries `syntaxRef.GetSyntax()` but gets null for external assemblies.

---

## Comparison Table

| Aspect | Mediator (Hybrid) | Nuru (Static) |
|--------|-------------------|---------------|
| **Generator complexity** | Low | High |
| **Handler discovery** | Interface scanning | Parse ConfigureServices |
| **External assemblies** | Works | Fails |
| **Internal types** | Works | Fails |
| **Runtime resolution** | MS DI ServiceProvider | Direct `new T()` |
| **AOT friendliness** | High | Higher (no DI) |
| **Flexibility** | High | Limited |
| **Runtime overhead** | Minimal | None |

---

## Recommended Path Forward for Nuru

### Option 1: Adopt Mediator's Hybrid Approach (Recommended)

**Change architecture to use MS DI at runtime:**

1. **Keep generator for discovery** - find handlers/endpoints at compile-time
2. **Generate DI registrations** instead of direct instantiation
3. **Let MS DI handle service resolution** at runtime

**Benefits:**
- Fixes external assembly issue immediately
- Simpler generator code
- Works with any registration pattern
- `internal` types work automatically

**Changes needed:**
```csharp
// Instead of generating:
private static readonly Lazy<MyService> __svc = new(() => new MyService());

// Generate:
services.AddSingleton<IMyService, MyServiceImpl>();  // Or use ServiceDescriptor
```

**Runtime resolution in handlers:**
```csharp
// Generated interceptor uses DI to resolve services
var service = serviceProvider.GetRequiredService<IMyService>();
var handler = new Handler(service);
```

### Option 2: Keep Static DI, Add Attribute-Based Declaration

**If you want to preserve pure static DI:**

1. **Add library-level attribute** for service registration:
   ```csharp
   [assembly: NuruServiceRegistration(
       Service = typeof(IMyService),
       Implementation = typeof(MyServiceImpl),
       Lifetime = ServiceLifetime.Singleton)]
   ```

2. **Add endpoint attribute** for service requirements:
   ```csharp
   [NuruRoute("test")]
   [RequiresService(typeof(IMyService))]
   public sealed class TestEndpoint : ICommand<int> { }
   ```

3. **Generator scans assemblies** for these attributes

**Benefits:**
- Preserves pure static DI
- Explicit declaration
- Works with internal types via factory methods

**Drawbacks:**
- More boilerplate for library authors
- Generator must scan assemblies for attributes

### Option 3: Hybrid - Static DI + DI Fallback

**Best of both worlds:**

1. **Static DI for in-project registrations**
2. **Fall back to MS DI for external registrations**

```csharp
// Generated code
private static readonly Lazy<MyService> __svc_Internal = 
    new(() => new InternalService());

// For external services, use DI
private static IExternalService? __ResolveExternalService(IServiceProvider sp)
    => sp.GetRequiredService<IExternalService>();
```

---

## Detailed Implementation: Mediator-Style for Nuru

### 1. Generate AddNuru Extension Method

```csharp
// Generated: NuruExtensions.g.cs
public static class NuruExtensions
{
    public static IServiceCollection AddNuru(
        this IServiceCollection services,
        Action<NuruOptions>? options = null)
    {
        var nuruOptions = new NuruOptions();
        options?.Invoke(nuruOptions);

        // Register discovered endpoints
        services.AddSingleton<EndpointRegistry>(sp => 
            new EndpointRegistry(...));

        // Register handlers (if using handler classes)
        services.AddSingleton(typeof(ICommandHandler<,>), typeof(CommandHandlerImpl<>));
        
        return services;
    }
}
```

### 2. Runtime Service Resolution

```csharp
// In the NuruApp builder
public NuruApp Build()
{
    // Resolve services from DI at runtime
    var registry = _serviceProvider.GetRequiredService<EndpointRegistry>();
    var terminal = _serviceProvider.GetRequiredService<ITerminal>();
    
    return new NuruApp(registry, terminal, ...);
}
```

### 3. Handler Parameter Injection via DI

```csharp
// Generated handler invocation
var handler = new TestHandler(
    _serviceProvider.GetRequiredService<IMyService>()  // DI resolution!
);

var result = handler.Handle(request, cancellationToken);
```

### 4. Cross-Assembly Handler Discovery

```csharp
// In the generator - scan for [NuruRoute] attributed types
foreach (var assembly in GetReferencedAssemblies(compilation))
{
    var types = assembly.GetTypes()
        .Where(t => t.GetCustomAttribute<NuruRouteAttribute>() != null);
    
    foreach (var type in types)
    {
        // Register with DI
        EmitServiceRegistration(type);
    }
}
```

---

## Conclusion

**Mediator's approach is superior for this use case** because:

1. It works with external assemblies out of the box
2. The generator is simpler (no need to parse ConfigureServices)
3. `internal` type instantiation is handled by MS DI at runtime
4. Registration is explicit and discoverable
5. Trades negligible runtime overhead for massive architectural simplification

**Nuru should consider migrating to Mediator-style DI registration:**

1. Generate `AddNuru(services, options => {})` extension method
2. Register endpoints/handlers with MS DI
3. Resolve services via `IServiceProvider` at runtime
4. Keep the excellent route matching and parameter binding logic
5. Remove `ConfigureServices` parsing complexity

This would fix issue #390 immediately while maintaining Nuru's performance benefits (minimal overhead with Singleton lifetime, fast route matching, compile-time endpoint discovery).

---

## References

- Mediator Source: `/src/Mediator.SourceGenerator/`
- Mediator Template: `Mediator.sbn-cs` (ServiceDescriptor registration)
- Nuru ServiceExtractor: `ServiceExtractor.cs:183-189` (hardcoded method names)
- Nuru ServiceResolverEmitter: `ServiceResolverEmitter.cs:76-104` (direct instantiation)
