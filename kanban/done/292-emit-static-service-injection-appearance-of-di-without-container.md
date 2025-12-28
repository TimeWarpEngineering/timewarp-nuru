# Emit static service injection - appearance of DI without container

## Parent

#265 Epic: V2 Source Generator Implementation

## Description

Provide the **appearance of dependency injection** in handler signatures without the runtime DI container overhead.

Users write handlers with service parameters using familiar `ConfigureServices` syntax:
```csharp
NuruApp.CreateBuilder(args)
  .ConfigureServices(services => {
    services.AddSingleton<IFoo, Foo>();
    services.AddTransient<IBar, Bar>();
  })
  .Map("greet {name}")
    .WithHandler((string name, IFoo foo) => foo.Greet(name))
    .Done()
  .Build();
```

The source generator:
1. Parses `ConfigureServices` registrations at compile time (via `ServiceExtractor`)
2. Matches handler parameters against registered services
3. Emits `new ImplementationType(...)` with recursive constructor resolution
4. Caches singletons in static fields with lazy initialization

**No runtime `IServiceProvider.GetRequiredService<T>()` calls** - all instantiation is compile-time generated.

## Package Strategy

- **Replace** `Microsoft.Extensions.DependencyInjection` (full, ~200KB) 
- **With** `Microsoft.Extensions.DependencyInjection.Abstractions` (lightweight, ~50KB)
- Users get familiar `services.AddSingleton<>()` syntax
- Generator emits `new Foo()` directly - no runtime container

## How It Works

### 1. Service Registration (Compile-Time Parsing)

`ServiceExtractor` already parses `ConfigureServices` lambdas and extracts:
```csharp
ServiceDefinition {
  ServiceTypeName: "IFoo",
  ImplementationTypeName: "Foo", 
  Lifetime: Singleton
}
```

### 2. Handler Parameter Matching

For each handler parameter:
- **Route parameter** (`{name}`) → bind from args
- **`ITerminal`** → emit `app.Terminal` (built-in)
- **`IConfiguration`** → emit `configuration` (from ConfigurationEmitter)
- **Registered service** → look up implementation, emit instantiation
- **Unregistered type** → **compile error**

### 3. Constructor Resolution

If `Foo` has constructor `Foo(IBar bar, IConfiguration config)`:
1. Analyzer inspects constructor parameters
2. Recursively resolves each dependency
3. Emits: `new Foo(new Bar(), configuration)`

### 4. Lifetime Handling

| Lifetime    | Behavior                           | Generated Code                              |
| ----------- | ---------------------------------- | ------------------------------------------- |
| `Singleton` | One instance, cached in static field | `__svc_Foo ??= new Foo(...)`                  |
| `Scoped`    | Same as Singleton (for now*)       | `__svc_Foo ??= new Foo(...)`                  |
| `Transient` | New instance every time            | `new Foo(...)`                                |

*Scoped behavior for REPL (SessionScoped/CommandScoped) deferred to follow-up task #294

## Generated Code Example

### User Code
```csharp
.ConfigureServices(services => {
  services.AddSingleton<IGreeter, Greeter>();
  services.AddTransient<IFormatter, Formatter>();
})
.Map("greet {name}")
  .WithHandler((string name, IGreeter greeter, IFormatter formatter) => 
    greeter.Greet(formatter.Format(name)))
```

### Generated Code
```csharp
// Static fields for singletons
private static Greeter? __svc_Greeter;

// In route handler:
if (args is ["greet", var __arg_name])
{
  // Singleton - cached
  Greeter greeter = __svc_Greeter ??= new Greeter();
  // Transient - new each time
  Formatter formatter = new Formatter();
  
  string result = greeter.Greet(formatter.Format(__arg_name));
  app.Terminal.WriteLine(result);
  return 0;
}
```

## Files to Modify/Create

| Action | File |
|--------|------|
| Modify | `source/timewarp-nuru-core/timewarp-nuru-core.csproj` - Replace DI package with Abstractions |
| Modify | `source/timewarp-nuru-analyzers/generators/emitters/service-resolver-emitter.cs` - Emit instantiation |
| Modify | `source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs` - Emit static fields |
| Create | `source/timewarp-nuru-analyzers/generators/analyzers/constructor-analyzer.cs` - Resolve constructor deps |

## Checklist

### Phase 1: Package Reference
- [x] Replace `Microsoft.Extensions.DependencyInjection` with `Microsoft.Extensions.DependencyInjection.Abstractions` in `timewarp-nuru-core.csproj`
- [x] Verify build still works

### Phase 2: Built-in Service Handling
- [x] Add `IsTerminalType()` check in `ServiceResolverEmitter`
- [x] Emit `app.Terminal` for `ITerminal` parameters
- [x] Verify `IConfiguration` handling (already done in #291)

### Phase 3: Service Registry Lookup
- [x] Pass `AppModel.Services` to `ServiceResolverEmitter`
- [x] Look up handler parameter types in registered services
- [x] Emit `new ImplementationType()` for registered services
- [x] Remove broken `app.Services.GetRequiredService<T>()` code path

### Phase 4: Constructor Analysis (Deferred)
- [ ] Create `ConstructorAnalyzer` class
- [ ] Get constructor parameters for implementation type
- [ ] Recursively resolve constructor dependencies against service registry
- [ ] Detect circular dependencies → emit diagnostic error
- [ ] Emit constructor calls with resolved dependencies

> **Note**: Phase 4 deferred - services must have parameterless constructors for now.

### Phase 5: Lifetime Handling
- [x] For `Singleton`/`Scoped`: emit `Lazy<T>` static field (thread-safe initialization)
- [x] For `Transient`: emit direct `new T()` each time
- [x] Generate static fields at class level in `InterceptorEmitter`

### Phase 6: Error Handling (Partial)
- [ ] Compile error for unregistered service types (emits error comment instead)
- [ ] Compile error for interfaces without implementation
- [ ] Compile error for circular dependencies
- [ ] Helpful error messages with registration suggestions

> **Note**: Phase 6 deferred - currently emits error comments rather than compile-time diagnostics.

### Phase 7: Verification
- [x] Build solution successfully
- [x] Test with sample using `ConfigureServices` registrations
- [x] Test singleton caching behavior
- [x] Test transient instantiation behavior
- [ ] Test constructor dependency resolution (deferred with Phase 4)

## Design Decisions

1. **Package**: Use `Microsoft.Extensions.DependencyInjection.Abstractions` (~50KB) for familiar API without runtime container overhead

2. **Scoped Lifetime**: Treat as `Singleton` for now (single CLI invocation = single scope). REPL scoping (SessionScoped/CommandScoped) deferred to follow-up task.

3. **Built-in Services**: Only `ITerminal` is pre-wired (via `app.Terminal`). All other services must be explicitly registered.

4. **Error Strategy**: Fail at compile time with helpful messages rather than runtime exceptions.

## Out of Scope (Follow-up Tasks)

- **REPL Scoping** (#294): SessionScoped and CommandScoped lifetimes for REPL mode
- **Factory Methods**: Support for `services.AddSingleton<IFoo>(sp => new Foo(sp.GetService<IBar>()))`
- **Open Generics**: Support for `services.AddSingleton(typeof(IRepository<>), typeof(Repository<>))`

## References

- Archived #245: `kanban/archived/245-emit-static-service-fields-replace-di.md`
- Related #291: AddConfiguration emitter support
- Follow-up #294: REPL scoping (SessionScoped/CommandScoped)

## Notes

This is the "killer feature" that makes Nuru competitive with ConsoleAppFramework on startup time while keeping the ergonomic DI-style API. Users get familiar syntax, zero runtime overhead.

## Results

**Completed 2024-12-29**

### Implementation Summary

- **Package**: Replaced `Microsoft.Extensions.DependencyInjection` with `Microsoft.Extensions.DependencyInjection.Abstractions` (lighter weight, ~50KB vs ~200KB)
- **Service Extraction**: Source generator extracts services from `ConfigureServices()` lambda at compile time
- **Lifetime Handling**:
  - Singleton/Scoped: Emits `Lazy<T>` static fields for thread-safe lazy initialization
  - Transient: Emits `new T()` for fresh instance each time
- **Built-in Services**:
  - `ITerminal` → `app.Terminal`
  - `IConfiguration` → `configuration` variable (from ConfigurationEmitter)
- **ConfigureServices()**: Made into a runtime no-op (generator handles everything at compile time)
- **Enhanced `IsServiceType()`**: Now recognizes interface types (pattern: `I` + uppercase letter)

### Test Coverage

Created comprehensive test file: `tests/timewarp-nuru-analyzers-tests/auto/generator-04-static-service-injection.cs`
- 7 passing tests covering:
  - Singleton service injection with `Lazy<T>` caching
  - Transient service injection with fresh instances
  - Built-in `ITerminal` service injection
  - Built-in `IConfiguration` service injection
  - Mixed services (singleton + transient + built-in)
  - Multiple handlers sharing singleton instances
  - Unregistered service error handling

### Deferred Work

- **Phase 4 (Constructor Resolution)**: Services must have parameterless constructors; constructor dependency resolution not implemented
- **Phase 6 (Compile-time Errors)**: Emits error comments for unregistered services rather than compile-time diagnostics
- **REPL Scoping**: Task #294 exists for SessionScoped/CommandScoped lifetimes
