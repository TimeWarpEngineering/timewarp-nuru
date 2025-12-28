# Emit static service injection - appearance of DI without container

## Parent

#265 Epic: V2 Source Generator Implementation

## Description

Provide the **appearance of dependency injection** in handler signatures without the bloat of `Microsoft.Extensions.DependencyInjection`. 

Users write handlers with service parameters as if DI exists:
```csharp
.WithHandler((string name, ILogger logger, IConfiguration config) => ...)
```

The source generator:
1. Detects non-route parameters (services) in handler signatures
2. Emits static fields with lazy initialization for each service type
3. Passes the static instances to handlers at call time

No `IServiceCollection`, no `IServiceProvider`, no runtime container resolution.

## User Experience

### DSL (what user writes)
```csharp
NuruApp.CreateBuilder(args)
  .AddConfiguration()  // Opts in to IConfiguration
  .Map("greet {name}")
    .WithHandler((string name, ILogger logger, IConfiguration config) => 
      $"Hello {name}")
    .Done()
  .Build();
```

### Generated Code
```csharp
file static class GeneratedServices
{
  // Configuration - lazy loaded at first access
  private static IConfigurationRoot? _configuration;
  public static IConfigurationRoot Configuration => 
    _configuration ??= new ConfigurationBuilder()
      .SetBasePath(AppContext.BaseDirectory)
      .AddJsonFile("appsettings.json", optional: true)
      .AddEnvironmentVariables()
      .Build();

  // Logger - uses LoggerFactory
  private static ILogger? _logger;
  public static ILogger Logger =>
    _logger ??= LoggerFactory
      .Create(builder => builder.AddConsole())
      .CreateLogger("App");
}

// In route handler:
if (args is ["greet", var name])
{
  string result = __handler_0(name, GeneratedServices.Logger, GeneratedServices.Configuration);
  app.Terminal.WriteLine(result);
  return 0;
}
```

## Checklist

### Analysis
- [ ] Identify which handler parameters are route params vs services
- [ ] Route params: come from args (string, int, bool, etc.)
- [ ] Service params: everything else (ILogger, IConfiguration, custom types)

### Interpreter
- [ ] Capture service parameter types from handler signatures
- [ ] Store in `RouteDefinition.ServiceDependencies` or similar

### Emitter - GeneratedServices class
- [ ] Emit `file static class GeneratedServices`
- [ ] For each unique service type, emit lazy static property
- [ ] Special handling for well-known types:
  - `IConfiguration` / `IConfigurationRoot` - ConfigurationBuilder
  - `ILogger` / `ILogger<T>` - LoggerFactory
  - `ITerminal` - use `app.Terminal`
  - Custom types - direct instantiation with `new()`

### Emitter - Handler calls
- [ ] Pass `GeneratedServices.Xxx` for each service parameter
- [ ] Maintain correct parameter order

### Custom Services
- [ ] User-defined types: emit `new MyService()`
- [ ] If constructor has dependencies, recursively resolve
- [ ] Detect circular dependencies at compile time (error)

### Edge Cases
- [ ] Interface without known implementation → compile error with helpful message
- [ ] Abstract class → compile error
- [ ] No parameterless constructor → try to resolve constructor params

## Benefits

- **Zero runtime overhead** - No container, no reflection, no resolution
- **AOT compatible** - All types known at compile time
- **Smaller binary** - No DI framework packages
- **Familiar API** - Looks like DI, feels like DI
- **Tree-shakeable** - Only services actually used are emitted

## What Gets Removed

After this is implemented:
- `IServiceCollection` usage
- `IServiceProvider` usage
- `ConfigureServices()` method
- `AddDependencyInjection()` method
- `Microsoft.Extensions.DependencyInjection` package reference

## References

- Archived #245: `kanban/archived/245-emit-static-service-fields-replace-di.md`
- Related #291: AddConfiguration emitter support

## Notes

This is the "killer feature" that makes Nuru competitive with ConsoleAppFramework on startup time while keeping the ergonomic DI-style API.
