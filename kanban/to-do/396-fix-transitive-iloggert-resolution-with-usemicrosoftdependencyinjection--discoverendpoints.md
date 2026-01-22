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

## Expected Fix

The generated `GetServiceProvider()` should either:
1. Actually invoke the user's `ConfigureServices` delegate at runtime, OR
2. Register `ILoggerFactory` and open generic `ILogger<>` in the generated service collection

## Checklist

- [ ] Create test endpoints mirroring the reproduction cases (test, test2, test3, test4, test5)
- [ ] Add failing test for transitive ILogger<T> resolution
- [ ] Fix generated code to register logging in DI container
- [ ] Verify ConfigureServices callback is invoked at runtime with UseMicrosoftDependencyInjection

## Test Endpoints to Create

| Endpoint | Dependencies | Expected |
|----------|--------------|----------|
| test | None | Pass |
| test2 | ITerminal (Nuru-provided) | Pass |
| test3 | IGreetingService -> IMessageFormatter (custom) | Pass |
| test4 | ILogger<T> (direct) | Pass |
| test5 | IServiceWithLogger -> ILogger<T> (transitive) | **Currently Fails** |

## Notes

Discovered while migrating ccc1-cli to Nuru 3.x Endpoints API. The library services (UserDatabaseManager, SqliteCredentialRepository, etc.) all require `ILogger<T>` in their constructors.
