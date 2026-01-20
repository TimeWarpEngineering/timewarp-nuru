#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:package Serilog
#:package Serilog.Sinks.Console
#:package Serilog.Extensions.Logging

// ═══════════════════════════════════════════════════════════════════════════════
// SERILOG LOGGING EXAMPLE
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates Serilog integration with TimeWarp.Nuru.
//
// Key features shown:
// - ConfigureServices(s => s.AddLogging(...)) with Serilog provider
// - ILogger<T> injection into behaviors
// - Structured logging with Serilog
//
// Run with: ./serilog-logging.cs test
//           ./serilog-logging.cs greet Alice
//
// ═══════════════════════════════════════════════════════════════════════════════

using TimeWarp.Nuru;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
  .MinimumLevel.Debug()
  .WriteTo.Console(
    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
  .CreateLogger();

NuruApp app = NuruApp.CreateBuilder()
  // Configure logging via AddLogging with Serilog provider
  .ConfigureServices(services => services
    .AddLogging(builder => builder
      .SetMinimumLevel(LogLevel.Debug)
      .AddSerilog(Log.Logger, dispose: true)))
  // Add a logging behavior that will use Serilog
  .AddBehavior(typeof(LoggingBehavior))
  // Simple routes
  .Map("test").WithHandler(() => Console.WriteLine("Test command executed")).AsCommand().Done()
  .Map("greet {name}").WithHandler((string name) =>
  {
    // Direct Serilog usage for structured logging in handlers
    Log.Information("Greeting user {UserName}", name);
    Console.WriteLine($"Hello, {name}!");
  }).AsCommand().Done()
  .Build();

return await app.RunAsync(args);

/// <summary>
/// A behavior that logs before and after each command using ILogger.
/// The logger will use Serilog because we configured AddSerilog() in AddLogging().
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
      Logger.LogInformation("Completed: {CommandName} ({ElapsedMs}ms)",
        context.CommandName, stopwatch.ElapsedMilliseconds);
    }
    catch (Exception ex)
    {
      stopwatch.Stop();
      Logger.LogError(ex, "Failed: {CommandName} ({ElapsedMs}ms)",
        context.CommandName, stopwatch.ElapsedMilliseconds);
      throw;
    }
  }
}
