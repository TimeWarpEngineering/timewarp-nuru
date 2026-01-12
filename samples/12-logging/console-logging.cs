#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:package Microsoft.Extensions.Logging
#:package Microsoft.Extensions.Logging.Console

// ═══════════════════════════════════════════════════════════════════════════════
// CONSOLE LOGGING EXAMPLE
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates logging configuration with TimeWarp.Nuru DSL API.
//
// Key features shown:
// - ConfigureServices(s => s.AddLogging(...)) to configure logging
// - ILogger<T> injection into behaviors
// - Automatic LoggerFactory disposal
//
// Run with: ./console-logging.cs test
//           ./console-logging.cs greet Alice
//
// ═══════════════════════════════════════════════════════════════════════════════

using TimeWarp.Nuru;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

NuruCoreApp app = NuruApp.CreateBuilder(args)
  // Configure logging via standard Microsoft.Extensions.Logging pattern
  .ConfigureServices(services => services
    .AddLogging(builder => builder
      .SetMinimumLevel(LogLevel.Debug)
      .AddConsole()))
  // Add a logging behavior that will use the configured logger
  .AddBehavior(typeof(LoggingBehavior))
  // Simple routes
  .Map("test").WithHandler(() => Console.WriteLine("Test command executed")).AsCommand().Done()
  .Map("greet {name}").WithHandler((string name) => Console.WriteLine($"Hello, {name}!")).AsCommand().Done()
  .Build();

return await app.RunAsync(args);

/// <summary>
/// A behavior that logs before and after each command using ILogger.
/// When AddLogging() is configured, this will use the real LoggerFactory.
/// When AddLogging() is NOT configured, this will use NullLogger (no output).
/// </summary>
public sealed class LoggingBehavior : INuruBehavior
{
  private readonly ILogger<LoggingBehavior> Logger;

  public LoggingBehavior(ILogger<LoggingBehavior> logger)
  {
    Logger = logger;
  }

  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    Logger.LogInformation("Starting: {CommandName}", context.CommandName);
    System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();

    try
    {
      await proceed();
      stopwatch.Stop();
      Logger.LogInformation("Completed: {CommandName} ({ElapsedMs}ms)", context.CommandName, stopwatch.ElapsedMilliseconds);
    }
    catch (Exception ex)
    {
      stopwatch.Stop();
      Logger.LogError(ex, "Failed: {CommandName} ({ElapsedMs}ms)", context.CommandName, stopwatch.ElapsedMilliseconds);
      throw;
    }
  }
}
