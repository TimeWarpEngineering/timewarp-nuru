#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../Source/TimeWarp.Nuru.Repl/TimeWarp.Nuru.Repl.csproj
#:package Serilog
#:package Serilog.Sinks.File
#:package Serilog.Extensions.Logging

// Basic REPL Demo for TimeWarp.Nuru with Filtered File Logging
// Run this file to see REPL mode in action
// Logs will be written to repl-debug.log for debugging completion logic
// Filter: Only REPL messages (Event IDs 2000-2499), no parsing/route registration noise

using Serilog;
using Serilog.Events;
using Microsoft.Extensions.Logging;
using TimeWarp.Nuru;
using TimeWarp.Nuru.Repl;
using static System.Console;

// Configure Serilog with filtered logging for REPL debugging only
Log.Logger = new LoggerConfiguration()
  .MinimumLevel.Verbose()  // Enable trace-level logs for debugging
  .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)  // Reduce noise from Microsoft components
  
  // Filter to keep only REPL messages by content - exclude parsing/route registration noise
  .Filter.ByExcluding(e => 
  {
    var message = e.RenderMessage();
    return message.Contains("Registering route:") ||
           message.Contains("Starting lexical analysis") ||
           message.Contains("Lexical analysis complete") ||
           message.Contains("Parsing pattern:") ||
           message.Contains("AST:") ||
           message.Contains("Checking route:") ||
           message.Contains("Failed to match") ||
           message.Contains("Route") && message.Contains("failed at") ||
           message.Contains("Tokens:") ||
           message.Contains("Setting boolean option parameter") ||
           message.Contains("Optional boolean option") ||
           message.Contains("Positional matching") ||
           message.Contains("Resolving command:");
  })
  
  // File sink for debugging REPL completion logic
  .WriteTo.File
  (
    path: "repl-debug.log",
    rollingInterval: RollingInterval.Day,
    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj}{NewLine}{Exception}",
    retainedFileCountLimit: 7
  )
  .Enrich.FromLogContext()
  .CreateLogger();

// Create an ILoggerFactory that uses Serilog
var loggerFactory = LoggerFactory.Create(builder =>
{
  builder.AddSerilog(Log.Logger);
});

try
{
  Log.Information("Starting TimeWarp.Nuru REPL Demo with file logging");

  WriteLine("TimeWarp.Nuru REPL Demo");
  WriteLine("========================");
  WriteLine("Debug logs will be written to: repl-debug.log");
  WriteLine();

  // Build a simple CLI app with logging
  var app = new NuruAppBuilder()
    .UseLogging(loggerFactory)  // Add logging to the app
    .WithMetadata
    (
      description: "Interactive REPL demo application for TimeWarp.Nuru framework."
    )
    .AddRoute
    (
      pattern: "greet {name}",
      handler: (string name) =>
      {
        Log.Information("Greet command executed with name: {Name}", name);
        WriteLine($"Hello, {name}!");
      },
      description: "Greets the person with the specified name."
    )
    .AddRoute
    (
      pattern: "status",
      handler: () =>
      {
        Log.Information("Status command executed");
        WriteLine("System is running OK");
      },
      description: "Displays the current system status."
    )
    .AddRoute
    (
      pattern: "echo {*message}",
      handler: (string[] message) =>
      {
        Log.Information("Echo command executed with message: {Message}", string.Join(" ", message));
        WriteLine(string.Join(" ", message));
      },
      description: "Echoes the provided message back to the user."
    )
    .AddRoute
    (
      pattern: "add {a:int} {b:int}",
      handler: (int a, int b) =>
      {
        Log.Information("Add command executed with values: {A} + {B}", a, b);
        WriteLine($"{a} + {b} = {a + b}");
      },
      description: "Adds two integers and displays the result."
    )
    .AddRoute(
      pattern: "time",
      handler: () =>
      {
        var now = DateTime.Now;
        Log.Information("Time command executed at: {Time}", now);
        WriteLine($"Current time: {now:HH:mm:ss}");
      },
      description: "Displays the current time."
    )
    .AddReplSupport
    (
      options =>
      {
        options.Prompt = "demo> ";
        options.WelcomeMessage = "Welcome to REPL demo! Try: greet World, status, add 5 3, time, or 'exit' to quit.";
        options.GoodbyeMessage = "Thanks for trying the REPL demo!";
        options.PersistHistory = false; // Don't persist history for demo
        Log.Information("REPL support configured with custom options");
      }
    )
    .Build();

  Log.Information("Starting REPL mode");

  // Start REPL mode directly
  return await app.RunReplAsync();
}
catch (Exception ex)
{
  Log.Fatal(ex, "REPL demo terminated unexpectedly");
  return 1;
}
finally
{
  Log.Information("Closing logger and flushing logs");
  Log.CloseAndFlush();
}
