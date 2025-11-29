#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../Source/TimeWarp.Nuru.Logging/TimeWarp.Nuru.Logging.csproj
#:package Microsoft.Extensions.Logging
#:package Mediator.Abstractions
#:package Mediator.SourceGenerator

using TimeWarp.Nuru;
using Mediator;
using Microsoft.Extensions.Logging;

// TimeWarp.Nuru.Logging provides three convenience methods for console logging:
//
// 1. UseConsoleLogging()
//    - Default option with Information level
//    - Shows route resolution and general application flow
//
// 2. UseConsoleLogging(LogLevel.Debug)
//    - Custom minimum log level
//    - Available levels: Trace, Debug, Information, Warning, Error, Critical
//
// 3. UseDebugLogging()
//    - Alias for UseConsoleLogging(LogLevel.Trace)
//    - Shows maximum detail including internal route matching
//
// 4. UseConsoleLogging(builder => { ... })
//    - Advanced configuration with filters and custom options
//    - See example below

// Example 1: Default logging (Information level)
// Uncomment to try:
// NuruCoreApp app = new NuruAppBuilder()
//     .UseConsoleLogging()
//     .Map("test", () => Console.WriteLine("Test command executed"))
//     .Build();

// Example 2: Debug level logging
// Uncomment to try:
// NuruCoreApp app = new NuruAppBuilder()
//     .UseConsoleLogging(LogLevel.Debug)
//     .Map("test", () => Console.WriteLine("Test command executed"))
//     .Build();

// Example 3: Trace level logging (most verbose)
// Uncomment to try:
// NuruCoreApp app = new NuruAppBuilder()
//     .UseDebugLogging()
//     .Map("test", () => Console.WriteLine("Test command executed"))
//     .Build();

// Active example - change which one is active by commenting/uncommenting:

// Example A: DELEGATE routes with ILogger<T> injection (NO DI container needed!)
// Demonstrates that delegates can use logging without the memory overhead of full DI
NuruCoreApp app = new NuruAppBuilder()
    .UseConsoleLogging()
    .Map("test", (ILogger<Program> logger) =>
    {
      logger.LogTrace("This is a TRACE message (very detailed)");
      logger.LogDebug("This is a DEBUG message (detailed)");
      logger.LogInformation("This is an INFORMATION message - Test command executed!");
      logger.LogWarning("This is a WARNING message");
      logger.LogError("This is an ERROR message");
      Console.WriteLine("✓ Test delegate completed");
    })
    .Map("greet {name}", (string name, ILogger<Program> logger) =>
    {
      logger.LogInformation("Greeting user: {Name}", name);
      Console.WriteLine($"Hello, {name}!");
    })
    .Build();

// Example A2: MEDIATOR commands with ILogger<T> injection (requires DI)
// Uncomment to try:
// NuruCoreApp app = new NuruAppBuilder()
//     .UseConsoleLogging()
//     .AddDependencyInjection()
//     .ConfigureServices(services => services.AddMediator()) // Source generator discovers handlers
//     .Map<TestCommand>("test")
//     .Map<GreetCommand>("greet {name}")
//     .AddAutoHelp()
//     .Build();

// Example B: Debug level (shows Debug, Info, Warning, Error)
// Uncomment to try:
// NuruCoreApp app = new NuruAppBuilder()
//     .UseConsoleLogging(LogLevel.Debug)
//     .AddDependencyInjection()
//     .ConfigureServices(services => services.AddMediator())
//     .Map<TestCommand>("test")
//     .Map<GreetCommand>("greet {name}")
//     .Build();

// Example C: Trace level - maximum verbosity (shows ALL log levels + internal routing details)
// Uncomment to try:
// NuruCoreApp app = new NuruAppBuilder()
//     .UseDebugLogging()
//     .AddDependencyInjection()
//     .ConfigureServices(services => services.AddMediator())
//     .Map<TestCommand>("test")
//     .Map<GreetCommand>("greet {name}")
//     .Build();

return await app.RunAsync(args);

// Commands that use ILogger to demonstrate logging in action
public sealed class TestCommand : IRequest
{
  internal sealed class Handler : IRequestHandler<TestCommand>
  {
    private readonly ILogger<Handler> Logger;

    public Handler(ILogger<Handler> logger)
    {
      Logger = logger;
    }

    public ValueTask<Unit> Handle(TestCommand request, CancellationToken cancellationToken)
    {
      Logger.LogTrace("This is a TRACE message (very detailed)");
      Logger.LogDebug("This is a DEBUG message (detailed)");
      Logger.LogInformation("This is an INFORMATION message - Test command executed!");
      Logger.LogWarning("This is a WARNING message");
      Logger.LogError("This is an ERROR message");
      Console.WriteLine("✓ Test command completed");
      return default;
    }
  }
}

public sealed class GreetCommand : IRequest
{
  public string Name { get; set; } = string.Empty;

  internal sealed class Handler : IRequestHandler<GreetCommand>
  {
    private readonly ILogger<Handler> Logger;

    public Handler(ILogger<Handler> logger)
    {
      Logger = logger;
    }

    public ValueTask<Unit> Handle(GreetCommand request, CancellationToken cancellationToken)
    {
      Logger.LogInformation("Greeting user: {Name}", request.Name);
      Console.WriteLine($"Hello, {request.Name}!");
      return default;
    }
  }
}