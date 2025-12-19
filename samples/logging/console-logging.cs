#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../source/timewarp-nuru-logging/timewarp-nuru-logging.csproj
#:package Mediator.Abstractions
#:package Mediator.SourceGenerator
#:package Microsoft.Extensions.Logging

// ═══════════════════════════════════════════════════════════════════════════════
// CONSOLE LOGGING - MICROSOFT.EXTENSIONS.LOGGING INTEGRATION
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates NuruApp.CreateBuilder(args) with console logging.
// The builder provides full DI container setup for ILogger<T> injection.
//
// REQUIRED PACKAGES:
//   #:package Mediator.Abstractions    - Required by NuruApp.CreateBuilder
//   #:package Mediator.SourceGenerator - Generates AddMediator() in YOUR assembly
//
// LOGGING CONVENIENCE METHODS:
//   .UseConsoleLogging()              - Information level (default)
//   .UseConsoleLogging(LogLevel.X)    - Custom level (Trace/Debug/Information/Warning/Error)
//   .UseDebugLogging()                - Alias for UseConsoleLogging(LogLevel.Trace)
// ═══════════════════════════════════════════════════════════════════════════════

using TimeWarp.Nuru;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Active example: Delegate routes with ILogger<T> injection
NuruCoreApp app = NuruApp.CreateBuilder(args)
    .ConfigureServices(services => services.AddMediator())
    .UseConsoleLogging()
    .Map("test")
      .WithHandler((ILogger<Program> logger) =>
      {
        logger.LogTrace("This is a TRACE message (very detailed)");
        logger.LogDebug("This is a DEBUG message (detailed)");
        logger.LogInformation("This is an INFORMATION message - Test command executed!");
        logger.LogWarning("This is a WARNING message");
        logger.LogError("This is an ERROR message");
        Console.WriteLine("✓ Test delegate completed");
      })
      .AsCommand()
      .Done()
    .Map("greet {name}")
      .WithHandler((string name, ILogger<Program> logger) =>
      {
        logger.LogInformation("Greeting user: {Name}", name);
        Console.WriteLine($"Hello, {name}!");
      })
      .AsCommand()
      .Done()
    .Build();

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