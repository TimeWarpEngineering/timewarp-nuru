# Emit static service fields (replace DI)

## Parent

#239 Epic: Compile-time endpoint generation

## Description

Replace MS.Extensions.DependencyInjection with generated static fields for service access. CLI apps don't need the complexity of a DI container - services can be static fields with lazy initialization.

## Requirements

- Analyze what services handlers actually use
- Generate static fields with lazy initialization
- Handle `IConfiguration` as special case (still runtime-loaded)
- Handle `ILogger` as special case
- No `IServiceProvider` resolution at runtime

## Checklist

- [ ] Detect service dependencies from handler parameters
- [ ] Generate static service holder class
- [ ] Emit lazy initialization for each service
- [ ] Handle `IConfiguration` (must still load at runtime)
- [ ] Handle `ILogger<T>` creation
- [ ] Handle user-defined services
- [ ] Test services are available to handlers
- [ ] Verify no DI container is created

## Notes

### Current Runtime Approach

```csharp
services.AddSingleton<IMyService, MyService>();
// Later
var service = serviceProvider.GetRequiredService<IMyService>();
```

### Generated Approach

```csharp
internal static class GeneratedServices
{
    // Configuration - still loaded at runtime from files/env
    private static IConfiguration? _configuration;
    public static IConfiguration Configuration => 
        _configuration ??= new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
    
    // Logger - uses configuration
    private static ILogger? _logger;
    public static ILogger Logger =>
        _logger ??= LoggerFactory
            .Create(b => b.AddConsole())
            .CreateLogger("App");
    
    // User service - direct instantiation
    private static MyService? _myService;
    public static MyService MyService =>
        _myService ??= new MyService(Configuration);
}

// Handler uses directly
static void Handle(string arg)
{
    GeneratedServices.Logger.LogInformation("Processing {Arg}", arg);
    GeneratedServices.MyService.DoWork(arg);
}
```

### What Gets Removed

- `IServiceCollection` / `IServiceProvider`
- `services.AddXxx()` calls
- `serviceProvider.GetRequiredService<T>()` calls
- MS.Extensions.DependencyInjection package reference
