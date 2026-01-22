# Fix transitive ILogger<T> resolution with UseMicrosoftDependencyInjection + DiscoverEndpoints

## Description

When using `.UseMicrosoftDependencyInjection()` + `.DiscoverEndpoints()`, services with `ILogger<T>` as a **transitive** dependency fail to resolve, while direct `ILogger<T>` injection in handlers works.

**Root Cause:** The generated `GetServiceProvider()` creates its own `ServiceCollection` and extracts service registrations at compile time, but:
1. It never calls the user's `ConfigureServices` delegate at runtime
2. `AddLogging()` (which uses a callback) is not parsed/emitted by the source generator
3. The static `__loggerFactory` is used for direct handler dependencies, but not registered in the DI container for transitive resolution

## Reproduction

```csharp
// test4 - WORKS: ILogger<T> as direct handler dependency
[NuruRoute("test4")]
public sealed class Test4Endpoint : ICommand<Unit>
{
  public sealed class Handler(ITerminal terminal, ILogger<Test4Endpoint> logger) 
    : ICommandHandler<Test4Endpoint, Unit>
  {
    public ValueTask<Unit> Handle(Test4Endpoint command, CancellationToken ct)
    {
      logger.LogInformation("This works!");
      return ValueTask.FromResult(Unit.Value);
    }
  }
}

// test5 - FAILS: ILogger<T> as transitive dependency
[NuruRoute("test5")]
public sealed class Test5Endpoint : ICommand<Unit>
{
  public sealed class Handler(ITerminal terminal, IServiceWithLogger svc) 
    : ICommandHandler<Test5Endpoint, Unit>
  {
    public ValueTask<Unit> Handle(Test5Endpoint command, CancellationToken ct)
    {
      svc.DoSomething(); // FAILS - can't resolve ILogger<ServiceWithLogger>
      return ValueTask.FromResult(Unit.Value);
    }
  }
}

public interface IServiceWithLogger { void DoSomething(); }

public class ServiceWithLogger : IServiceWithLogger
{
  private readonly ILogger<ServiceWithLogger> Logger;
  public ServiceWithLogger(ILogger<ServiceWithLogger> logger) => Logger = logger;
  public void DoSomething() => Logger.LogInformation("Called!");
}

// Program.cs
NuruApp app = NuruApp.CreateBuilder()
  .UseMicrosoftDependencyInjection()
  .ConfigureServices(services =>
  {
    services.AddLogging(b => b.AddConsole()); // NOT being invoked at runtime!
    services.AddSingleton<IServiceWithLogger, ServiceWithLogger>();
  })
  .DiscoverEndpoints()
  .Build();
```

**Error:**
```
System.InvalidOperationException: Unable to resolve service for type 
'Microsoft.Extensions.Logging.ILogger`1[ServiceWithLogger]' while attempting 
to activate 'ServiceWithLogger'.
```

## Solution

When `UseMicrosoftDependencyInjection()` is enabled, emit the user's `ConfigureServices` lambda body as a static method and invoke it at runtime. This ensures all extension methods (`AddLogging`, `AddDbContext`, etc.) work naturally.

**Current broken approach:**
```csharp
// Generated code extracts individual registrations and replays them
private static IServiceProvider GetServiceProvider()
{
  var services = new ServiceCollection();
  services.AddSingleton<IServiceWithLogger, ServiceWithLogger>(); // Extracted
  // AddLogging() is NOT called - only detected as extension method
  return services.BuildServiceProvider();
}
```

**Fixed approach:**
```csharp
// Generated code invokes the user's delegate directly
private static void __ConfigureServices(IServiceCollection services)
{
  // User's lambda body emitted verbatim
  services.AddLogging(b => b.AddConsole());
  services.AddSingleton<IServiceWithLogger, ServiceWithLogger>();
}

private static IServiceProvider GetServiceProvider()
{
  var services = new ServiceCollection();
  __ConfigureServices(services); // Invoke user's delegate at runtime
  return services.BuildServiceProvider();
}
```

## Implementation Plan

### Step 1: Capture ConfigureServices Lambda Body

**File:** `source/timewarp-nuru-analyzers/generators/models/app-model.cs`

- [ ] Add `string? ConfigureServicesLambdaBody` property to `AppModel`
- [ ] This stores the raw lambda body text when `UseMicrosoftDependencyInjection` is true

### Step 2: Extract Lambda Body in DSL Interpreter

**File:** `source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs`

- [ ] In `ProcessConfigureServices()`, when `UseMicrosoftDependencyInjection` is true:
  - Extract the full lambda body text (block or expression)
  - Store in `appBuilder.SetConfigureServicesBody(lambdaBody)`
- [ ] Add `SetConfigureServicesBody()` method to `IIrAppBuilder` and `IrAppBuilder`

### Step 3: Update IR Builder

**Files:**
- `source/timewarp-nuru-analyzers/generators/ir-builders/abstractions/iir-app-builder.cs`
- `source/timewarp-nuru-analyzers/generators/ir-builders/ir-app-builder.cs`

- [ ] Add `IIrAppBuilder SetConfigureServicesBody(string lambdaBody)`
- [ ] Store and pass through to `AppModel`

### Step 4: Modify Interceptor Emitter

**File:** `source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs`

- [ ] In `EmitRuntimeDiInfrastructure()`:
  - Emit `__ConfigureServices(IServiceCollection services)` static method with user's lambda body
  - Remove the loop that re-registers extracted services
  - Call `__ConfigureServices(services)` in `GetServiceProvider()`
- [ ] Handle both block body `{ ... }` and expression body `=> ...` lambdas

### Step 5: Handle Built-in Services

**File:** `source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs`

- [ ] Register Nuru built-ins before calling user's delegate:
  ```csharp
  services.AddSingleton<ITerminal>(app.Terminal);
  services.AddSingleton<IConfiguration>(configuration);
  services.AddSingleton<NuruApp>(app);
  ```
- [ ] This ensures built-ins are available for user services that depend on them

### Step 6: Remove Static LoggerFactory for Runtime DI Path

**File:** `source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs`

- [ ] When `UseMicrosoftDependencyInjection` + `HasLogging`: don't emit static `__loggerFactory` field
- [ ] Let MS DI handle `ILogger<T>` resolution naturally via `AddLogging()`

## Files to Modify

| File | Action |
|------|--------|
| `generators/models/app-model.cs` | Add `ConfigureServicesLambdaBody` property |
| `generators/ir-builders/abstractions/iir-app-builder.cs` | Add `SetConfigureServicesBody()` |
| `generators/ir-builders/ir-app-builder.cs` | Implement `SetConfigureServicesBody()` |
| `generators/interpreter/dsl-interpreter.cs` | Extract and store lambda body |
| `generators/emitters/interceptor-emitter.cs` | Emit static method + invoke at runtime |

## Verification

- [ ] Create test endpoints (test, test2, test3, test4, test5) as sample
- [ ] Run test5 - transitive `ILogger<T>` must resolve
- [ ] Run CI tests: `dotnet run tests/ci-tests/run-ci-tests.cs`
- [ ] Verify existing samples still work

## Test Endpoints to Create

| Endpoint | Dependencies | Expected |
|----------|--------------|----------|
| test | None | Pass |
| test2 | ITerminal (Nuru-provided) | Pass |
| test3 | IGreetingService -> IMessageFormatter (custom) | Pass |
| test4 | ILogger<T> (direct) | Pass |
| test5 | IServiceWithLogger -> ILogger<T> (transitive) | Pass (currently fails) |

## Notes

- This is the proper fix: invoke user's delegate at runtime instead of replaying extracted registrations
- All extension methods (`AddLogging`, `AddDbContext`, `AddHttpClient`, etc.) will work naturally
- No need for special-case handling of `AddLogging()` or other extensions
- Discovered while migrating ccc1-cli to Nuru 3.x Endpoints API
