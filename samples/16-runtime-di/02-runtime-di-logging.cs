#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:package Microsoft.Extensions.Logging.Console

// ═══════════════════════════════════════════════════════════════════════════════
// RUNTIME DI - TRANSITIVE ILOGGER<T> RESOLUTION
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates the fix for issue #396: transitive ILogger<T> resolution
// when using UseMicrosoftDependencyInjection() + ConfigureServices with AddLogging().
//
// PROBLEM (before fix):
//   - Direct ILogger<T> injection in handlers worked (via static __loggerFactory)
//   - Transitive ILogger<T> dependencies failed (service -> ILogger<T>)
//
// SOLUTION:
//   - Emit user's ConfigureServices lambda body as static method
//   - Invoke it at runtime so AddLogging() actually runs
//   - MS DI handles all ILogger<T> resolution naturally
//
// RUN THIS SAMPLE:
//   ./02-runtime-di-logging.cs test    # Direct ILogger<T> injection
//   ./02-runtime-di-logging.cs greet   # Transitive ILogger<T> injection
// ═══════════════════════════════════════════════════════════════════════════════

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .UseMicrosoftDependencyInjection()
  .ConfigureServices(services =>
  {
    // This MUST be invoked at runtime for ILogger<T> to work
    services.AddLogging(builder => builder.AddConsole());

    // Service with transitive ILogger<T> dependency
    services.AddSingleton<IGreetingService, GreetingService>();
  })
  // Route with direct ILogger<T> injection (always worked)
  .Map("test")
    .WithDescription("Test direct ILogger<T> injection")
    .WithHandler((ILogger<TestEndpoint> logger) =>
    {
      logger.LogInformation("Direct ILogger<T> injection works!");
      return "Direct logging test passed";
    })
    .Done()
  // Route with transitive ILogger<T> dependency (fixed by #396)
  .Map("greet {name?}")
    .WithDescription("Greet using service with ILogger<T> dependency")
    .WithHandler((string? name, IGreetingService greeter) =>
    {
      return greeter.Greet(name ?? "World");
    })
    .Done()
  .Build();

return await app.RunAsync(args);

// Placeholder for ILogger<T> type parameter
public class TestEndpoint { }

// ═══════════════════════════════════════════════════════════════════════════════
// SERVICE WITH TRANSITIVE ILOGGER<T> DEPENDENCY
// ═══════════════════════════════════════════════════════════════════════════════

public interface IGreetingService
{
  string Greet(string name);
}

/// <summary>
/// Service that depends on ILogger<T>.
/// Before fix #396, this would fail to resolve because AddLogging()
/// was not being invoked at runtime.
/// </summary>
public class GreetingService : IGreetingService
{
  private readonly ILogger<GreetingService> _logger;

  public GreetingService(ILogger<GreetingService> logger)
  {
    _logger = logger;
  }

  public string Greet(string name)
  {
    _logger.LogInformation("Greeting {Name}", name);
    return $"Hello, {name}! (logged via transitive ILogger<T>)";
  }
}
