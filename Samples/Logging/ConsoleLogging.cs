#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../Source/TimeWarp.Nuru.Logging/TimeWarp.Nuru.Logging.csproj
#:package Microsoft.Extensions.Logging

using TimeWarp.Nuru;
using TimeWarp.Nuru.Logging;
using TimeWarp.Mediator;
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
// var app = new NuruAppBuilder()
//     .UseConsoleLogging()
//     .AddRoute("test", () => Console.WriteLine("Test command executed"))
//     .Build();

// Example 2: Debug level logging
// Uncomment to try:
// var app = new NuruAppBuilder()
//     .UseConsoleLogging(LogLevel.Debug)
//     .AddRoute("test", () => Console.WriteLine("Test command executed"))
//     .Build();

// Example 3: Trace level logging (most verbose)
// Uncomment to try:
// var app = new NuruAppBuilder()
//     .UseDebugLogging()
//     .AddRoute("test", () => Console.WriteLine("Test command executed"))
//     .Build();

// Active example - change which one is active by commenting/uncommenting:

// Example A: DELEGATE routes with ILoggerFactory injection (NO DI container needed!)
// Demonstrates that delegates can use logging without the memory overhead of full DI
NuruApp app = new NuruAppBuilder()
    .UseConsoleLogging()
    .AddRoute("test", (ILoggerFactory loggerFactory) =>
    {
      ILogger logger = loggerFactory.CreateLogger("TestDelegate");
      logger.LogTrace("This is a TRACE message (very detailed)");
      logger.LogDebug("This is a DEBUG message (detailed)");
      logger.LogInformation("This is an INFORMATION message - Test command executed!");
      logger.LogWarning("This is a WARNING message");
      logger.LogError("This is an ERROR message");
      Console.WriteLine("✓ Test delegate completed");
    })
    .AddRoute("greet {name}", (string name, ILoggerFactory loggerFactory) =>
    {
      ILogger logger = loggerFactory.CreateLogger("GreetDelegate");
      logger.LogInformation("Greeting user: {Name}", name);
      Console.WriteLine($"Hello, {name}!");
    })
    .Build();

// Example A2: MEDIATOR commands with ILogger<T> injection (requires DI)
// Uncomment to try:
// NuruApp app = new NuruAppBuilder()
//     .UseConsoleLogging()
//     .AddDependencyInjection(config => config.RegisterServicesFromAssembly(typeof(TestCommand).Assembly))
//     .AddRoute<TestCommand>("test")
//     .AddRoute<GreetCommand>("greet {name}")
//     .AddAutoHelp()
//     .Build();

// Example B: Debug level (shows Debug, Info, Warning, Error)
// Uncomment to try:
// NuruApp app = new NuruAppBuilder()
//     .UseConsoleLogging(LogLevel.Debug)
//     .AddDependencyInjection(config => config.RegisterServicesFromAssembly(typeof(TestCommand).Assembly))
//     .AddRoute<TestCommand>("test")
//     .AddRoute<GreetCommand>("greet {name}")
//     .Build();

// Example C: Trace level - maximum verbosity (shows ALL log levels + internal routing details)
// Uncomment to try:
// NuruApp app = new NuruAppBuilder()
//     .UseDebugLogging()
//     .AddDependencyInjection(config => config.RegisterServicesFromAssembly(typeof(TestCommand).Assembly))
//     .AddRoute<TestCommand>("test")
//     .AddRoute<GreetCommand>("greet {name}")
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

    public async Task Handle(TestCommand request, CancellationToken cancellationToken)
    {
      Logger.LogTrace("This is a TRACE message (very detailed)");
      Logger.LogDebug("This is a DEBUG message (detailed)");
      Logger.LogInformation("This is an INFORMATION message - Test command executed!");
      Logger.LogWarning("This is a WARNING message");
      Logger.LogError("This is an ERROR message");
      Console.WriteLine("✓ Test command completed");
      await Task.CompletedTask;
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

    public async Task Handle(GreetCommand request, CancellationToken cancellationToken)
    {
      Logger.LogInformation("Greeting user: {Name}", request.Name);
      Console.WriteLine($"Hello, {request.Name}!");
      await Task.CompletedTask;
    }
  }
}