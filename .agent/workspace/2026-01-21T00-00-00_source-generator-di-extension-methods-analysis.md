# Analysis: Source Generator Service Resolution for Extension Methods

## Executive Summary

The Nuru source generator's DI resolution fails for services registered via extension methods in external assemblies because it uses compile-time static analysis with a fixed whitelist of method names (`AddTransient`, `AddScoped`, `AddSingleton`). This is a fundamental design constraint, not a bug. **A runtime DI container is NOT required** - the solution is to extend the generator's static analysis capabilities to recognize additional registration patterns and support explicit declaration of required services.

## Scope

This analysis covers:
- Current `ServiceExtractor.cs` implementation and its limitations
- The `ServiceResolverEmitter.cs` code generation pattern
- Cross-assembly analysis capabilities and constraints
- Solution options and their trade-offs

## Methodology

- Analyzed source code in `/source/timewarp-nuru-analyzers/generators/`
- Reviewed `ServiceExtractor.cs` for method recognition logic
- Reviewed `ServiceResolverEmitter.cs` for service instantiation patterns
- Examined test files for current DI behavior
- Evaluated Roslyn source generator constraints for cross-assembly analysis

## Findings

### 1. Current Architecture

The source generator uses **compile-time static DI** to avoid runtime container overhead. The pipeline is:

```
Source Code → Syntax Analysis → ServiceExtractor → AppModel.Services → ServiceResolverEmitter → Generated Code
```

**Key Components:**
- `ServiceExtractor.cs` - Extracts registrations from `ConfigureServices()` calls
- `ServiceDefinition.cs` - Data model holding `(ServiceType, ImplementationType, Lifetime)`
- `ServiceResolverEmitter.cs` - Generates `new T()` instantiation code for handlers

### 2. Root Cause Analysis

The limitation is in `ServiceExtractor.cs:183-189`:

```csharp
ServiceLifetime? lifetime = methodName switch
{
  "AddTransient" => ServiceLifetime.Transient,
  "AddScoped" => ServiceLifetime.Scoped,
  "AddSingleton" => ServiceLifetime.Singleton,
  _ => null  // <-- All other methods return null, ignored silently
};
```

**What the generator CAN do:**
- Analyze inline lambdas: `.ConfigureServices(s => { s.AddSingleton<IFoo, Foo>(); })`
- Follow method groups within the same project: `.ConfigureServices(ConfigureServices)`
- Extract types from generic arguments or `typeof()` expressions
- Generate thread-safe `Lazy<T>` fields for singleton services

**What the generator CANNOT do:**
- Recognize custom extension methods (e.g., `AddMyServices()`)
- Follow extension methods into external assemblies (no source access)
- Instantiate `internal` types from referenced assemblies (visibility constraint)

### 3. Cross-Assembly Constraint

When `ServiceExtractor.cs` attempts to follow a method group:

```csharp
SyntaxReference? syntaxRef = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault();
if (syntaxRef is null)
  return [];  // No source available - method defined in external assembly
```

External assemblies don't expose their source code to the generator. Even if they did, `internal` types cannot be instantiated from the consuming assembly.

### 4. Generated Code Pattern

The generator produces static instantiation code:

```csharp
// Generated for singleton
private static readonly global::System.Lazy<MyService> __svc_MyNamespace_MyService = 
    new(() => new MyService());

// Handler instantiation
var __handler = new Handler(__svc_MyNamespace_MyService.Value);
```

This pattern requires:
1. Knowing the implementation type name
2. Having access to instantiate it (not `internal` in another assembly)

## Solution Options

### Option A: Extend Method Name Recognition (Simplest)

**Approach:** Add more recognized method names to the whitelist.

**Pros:**
- Minimal code change
- Works for in-project extension methods

**Cons:**
- Still fails for external assemblies
- Requires library authors to use specific method names
- Doesn't solve the `internal` type instantiation problem

**Implementation:**
```csharp
ServiceLifetime? lifetime = methodName switch
{
  "AddTransient" or "AddMyServices" => ServiceLifetime.Transient,  // Configurable
  "AddScoped" or "AddMyScopedServices" => ServiceLifetime.Scoped,
  "AddSingleton" or "AddMySingletonServices" => ServiceLifetime.Singleton,
  _ => null
};
```

### Option B: Attribute-Based Service Declaration (Recommended)

**Approach:** Declare required services directly on endpoints/handlers using attributes. The generator doesn't need to find the registration - it just validates the service exists.

**Pros:**
- Works across assemblies
- Explicit intent, self-documenting
- No changes to library registration patterns
- Works with `internal` implementations

**Cons:**
- Requires adding attributes to endpoints
- Still needs a way to get the implementation instance

**Implementation:**
```csharp
[NuruRoute("test")]
[RequiresService(typeof(IMyService))]  // Generator validates and emits
public sealed class TestEndpoint : ICommand<int> { }
```

**Combined with factory pattern:**
```csharp
// In the library
public static class ServiceCollectionExtensions
{
  [NuruServiceRegistration]  // Marker for generators
  public static IServiceCollection AddMyServices(this IServiceCollection services)
  {
    services.AddSingleton<IMyService, MyServiceImpl>();
    return services;
  }
}

// Generator finds [NuruServiceRegistration] and extracts registrations
```

### Option C: Service Provider Factory Pattern

**Approach:** Support factory delegate registrations that the generator can invoke at compile time.

**Pros:**
- Works with any registration pattern
- Supports `internal` types via factory methods

**Cons:**
- Requires library authors to use specific patterns
- More complex to implement

**Implementation:**
```csharp
// In the library
public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddMyServices(this IServiceCollection services)
  {
    // Standard registration
    services.AddSingleton<IMyService, MyServiceImpl>();
    
    // Factory method for the generator
    services.AddSingletonFactory<IMyService>(() => MyServiceImpl.Instance);
    return services;
  }
}
```

### Option D: Hybrid - Static DI + Runtime Fallback

**Approach:** Use static DI for known patterns, fall back to MS DI `ServiceProvider` for unrecognized registrations.

**Pros:**
- Maximum flexibility
- Works with any registration pattern

**Cons:**
- Defeats the compile-time DI purpose
- Adds runtime overhead for fallback cases
- Requires generator to track which services use static vs. dynamic resolution

**Implementation:**
```csharp
// Generated code
partial class NuruGenerated
{
  private static ServiceProvider? _serviceProvider;
  
  private static object ResolveService(Type serviceType)
  {
    _serviceProvider ??= BuildServiceProvider();
    return _serviceProvider.GetRequiredService(serviceType);
  }
}
```

## Recommendation

**Do NOT implement a runtime DI container.** The compile-time DI is a core design principle that provides performance benefits.

**Recommended solution: Option B (Attribute-Based) with Service Factory Support**

1. **Add `[NuruServiceRegistration]` attribute** for library authors:
   ```csharp
   [AttributeUsage(AttributeTargets.Method)]
   public sealed class NuruServiceRegistrationAttribute : Attribute
   {
     public Type? ServiceType { get; set; }
     public Type? ImplementationType { get; set; }
     public ServiceLifetime Lifetime { get; set; }
   }
   ```

2. **Add `[RequiresService]` attribute** for endpoint declarations:
   ```csharp
   [NuruRoute("test")]
   [RequiresService(typeof(IMyService))]
   public sealed class TestEndpoint : ICommand<int> { }
   ```

3. **Extend `ServiceExtractor`** to:
   - Scan assemblies for `[NuruServiceRegistration]` methods
   - Extract registrations from attributed methods
   - Support factory methods that return service instances

4. **Update `ServiceResolverEmitter`** to:
   - Generate factory invocation code for attributed services
   - Handle `internal` implementations via factory delegates

**Why this works:**
- Libraries declare their registrations explicitly via attributes
- Generators can discover these without source access
- `internal` implementations are accessed via factory methods
- Compile-time DI is preserved
- No runtime container overhead

## Implementation Checklist

1. **Create `NuruServiceRegistrationAttribute`** in `TimeWarp.Nuru.Annotations`
2. **Update `ServiceExtractor.cs`** to:
   - Scan for attributed extension methods
   - Extract service/impl types and lifetime from attributes
3. **Create `ServiceFactoryEmitter`** for factory-based instantiation
4. **Update `ServiceResolverEmitter.cs`** to use factories when available
5. **Add diagnostic** when service is required but not registered
6. **Update documentation** with library author guidelines

## References

- `ServiceExtractor.cs:183-189` - Current method name whitelist
- `ServiceExtractor.cs:67-107` - Method group resolution (can follow across syntax trees)
- `ServiceResolverEmitter.cs:76-104` - Service instantiation logic
- `ServiceDefinition.cs:10-13` - Service registration model
