# Add UseMicrosoftDependencyInjection() DSL Method

## Parent

Epic #391: Full DI Support - Source-Gen and Runtime Options

## Description

Add `UseMicrosoftDependencyInjection()` method to the NuruAppBuilder DSL that enables full MS DI container at runtime. This is an opt-in escape hatch for users who need:
- Factory delegate registrations
- Services with constructor dependencies
- Extension method registrations (AddDbContext, AddSerilog, etc.)
- Internal type implementations

## API Design

```csharp
// Default - source-gen DI (current behavior)
NuruApp.CreateBuilder()
    .ConfigureServices(services => { ... })
    .Build();

// Opt-in - full MS DI runtime container
NuruApp.CreateBuilder()
    .UseMicrosoftDependencyInjection()  // NEW
    .ConfigureServices(services => { ... })
    .Build();
```

## Requirements

### Model Changes
- [x] Add `UseMicrosoftDependencyInjection` boolean to `AppModel`
- [x] Default value: `false`

### DSL Changes
- [x] Add `UseMicrosoftDependencyInjection()` extension method to builder
- [x] Method is recognized by DSL interpreter

### Generator Changes
- [x] Update `DslInterpreter` to recognize `UseMicrosoftDependencyInjection` call
- [x] Add branch in `InterceptorEmitter`:
  - If `true`: emit runtime DI code path
  - If `false`: emit current source-gen DI code path

### Runtime DI Code Generation
- [x] Emit `IServiceProvider` field
- [x] Emit `GetServiceProvider()` method that:
  - Creates `ServiceCollection`
  - Registers extracted services (MS DI resolves constructor deps automatically)
  - Calls `BuildServiceProvider()`
  - Caches result
- [x] Emit handler invocation using `GetRequiredService<T>()`

### Testing
- [ ] Test extension methods work (e.g., mock `AddMyServices()`) - Requires Phase 4
- [ ] Test factory delegates work - Requires Phase 4
- [x] Test constructor dependencies resolve correctly - MS DI handles automatically
- [ ] Test services with ILogger<T> injection
- [x] Test singleton/transient lifetimes - generator-15-runtime-di.cs
- [x] Verify AOT still works for default (source-gen) path - All 1023 tests pass

## Implementation Summary

### Files Modified

1. **app-model.cs** - Added `UseMicrosoftDependencyInjection` parameter
2. **nuru-app-builder.configuration.cs** - Added DSL method
3. **dsl-interpreter.cs** - Added dispatch for the method call
4. **iir-app-builder.cs** - Added interface method
5. **ir-app-builder.cs** - Added field, method, and interface implementation
6. **generator-model.cs** - Added `UsesMicrosoftDependencyInjection` helper property
7. **interceptor-emitter.cs** - Added runtime DI infrastructure emission
8. **service-resolver-emitter.cs** - Added runtime DI path with `GetRequiredService<T>()`
9. **handler-invoker-emitter.cs** - Threaded runtime DI flag through invocation
10. **route-matcher-emitter.cs** - Threaded runtime DI flag to handlers

### Generated Code Example

When `UseMicrosoftDependencyInjection()` is called:

```csharp
public static partial class NuruGenerated
{
    private static global::System.IServiceProvider? __serviceProvider;

    private static global::System.IServiceProvider GetServiceProvider()
    {
        if (__serviceProvider is not null) return __serviceProvider;

        var services = new global::Microsoft.Extensions.DependencyInjection.ServiceCollection();

        // Services extracted from ConfigureServices at compile time
        // MS DI handles constructor dependency resolution automatically
        global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<IMyService, MyService>(services);

        __serviceProvider = global::Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions.BuildServiceProvider(services);
        return __serviceProvider;
    }

    // In handler invocation:
    IMyService myService = global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<IMyService>(GetServiceProvider());
}
```

### Test File

**tests/timewarp-nuru-tests/generator/generator-15-runtime-di.cs** - 5 tests:
- `Should_resolve_service_with_constructor_dependency`
- `Should_resolve_two_level_dependency_chain`
- `Should_respect_transient_lifetime`
- `Should_respect_singleton_lifetime`
- `Should_inject_multiple_services_with_dependencies`

## Lessons Learned

### Mixed DI Strategies in Same Assembly

When apps in the same assembly use different DI strategies (some source-gen, some runtime), the code generator must emit infrastructure for BOTH:
- Static `Lazy<T>` fields for source-gen DI apps
- `GetServiceProvider()` runtime infrastructure for runtime DI apps

**Fix applied:** `interceptor-emitter.cs` now filters services by app's DI strategy and emits appropriate infrastructure for each.

### Shared Static ServiceProvider Limitation

The runtime DI uses a single static `__serviceProvider` field shared across all apps that use `UseMicrosoftDependencyInjection()`. This means:
- The ServiceProvider is built once with ALL services from ALL runtime DI apps
- Services registered with different lifetimes in different apps may conflict
- Tests must use unique types to avoid cross-test interference

**Future consideration:** For Phase 5 or beyond, consider per-app ServiceProvider isolation.

## Notes

- This is Phase 1 of Epic #391
- Goal is to unblock users ASAP
- Performance tradeoff is explicit and documented
- Users understand they're opting out of AOT optimization
- Extension methods (AddDbContext, AddSerilog) require Phase 4 (Execute & Inspect)
- All 1023 tests pass with these changes
