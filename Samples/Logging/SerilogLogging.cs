#!/usr/bin/dotnet --
// SerilogExample.cs - Example of using TimeWarp.Nuru with Serilog for structured logging

#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:package Serilog
#:package Serilog.Sinks.Console
#:package Serilog.Sinks.Seq
#:package Serilog.Extensions.Logging

using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using Microsoft.Extensions.Logging;
using TimeWarp.Nuru;

// Configure Serilog with both Console and Seq sinks
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()  // Enable trace-level logs
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)  // Reduce noise from Microsoft components
    
    // Console sink with a nice theme for development
    .WriteTo.Console(
        theme: AnsiConsoleTheme.Literate,  // Try also: AnsiConsoleTheme.Code or AnsiConsoleTheme.Sixteen
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    
    // Seq sink for structured logging (assumes Seq is running on localhost:5341)
    // To run Seq locally: docker run --rm -it -p 5341:80 datalust/seq
    .WriteTo.Seq("http://localhost:5341")
    
    // Enrich logs with additional context
    .Enrich.FromLogContext()
    
    .CreateLogger();

// Create an ILoggerFactory that uses Serilog
ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddSerilog(Log.Logger);
});

try
{
    Log.Information("Starting TimeWarp.Nuru application with Serilog");
    
    NuruApp app = new NuruAppBuilder()
        .UseLogging(loggerFactory)  // Use Serilog for all Nuru logging
        
        .Map("test", () => 
        {
            Log.Information("Test command executed");
            Console.WriteLine("Test successful!");
        })
        
        .Map("greet {name}", (string name) => 
        {
            // Structured logging - the name will be a searchable property in Seq
            Log.Information("Greeting user {UserName}", name);
            Console.WriteLine($"Hello, {name}!");
        })
        
        .Map("error", () => 
        {
            Log.Error("Simulating an error for demonstration");
            throw new InvalidOperationException("This is a test error");
        })
        
        .Map("bench {iterations:int}", (int iterations) => 
        {
            using (Log.Logger.BeginTimedOperation("Benchmark operation"))
            {
                Log.Information("Starting benchmark with {IterationCount} iterations", iterations);
                
                for (int i = 0; i < iterations; i++)
                {
                    if (i % 1000 == 0)
                    {
                        Log.Debug("Completed {CompletedIterations} iterations", i);
                    }
                }
                
                Log.Information("Benchmark completed");
            }
        })
        
        .Build();
    
    await app.RunAsync(args);
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

return 0;

// Extension method for timed operations (useful pattern with Serilog)
public static class SerilogExtensions
{
    public static IDisposable BeginTimedOperation(this Serilog.ILogger logger, string operationName)
    {
        return new TimedOperation(logger, operationName);
    }
    
    private class TimedOperation : IDisposable
    {
        private readonly Serilog.ILogger Logger;
        private readonly string OperationName;
        private readonly DateTime Start;
        
        public TimedOperation(Serilog.ILogger logger, string operationName)
        {
            Logger = logger;
            OperationName = operationName;
            Start = DateTime.UtcNow;
            Logger.Information("Starting operation: {OperationName}", OperationName);
        }
        
        public void Dispose()
        {
            TimeSpan elapsed = DateTime.UtcNow - Start;
            Logger.Information("Completed operation: {OperationName} in {ElapsedMs}ms", 
                OperationName, elapsed.TotalMilliseconds);
        }
    }
}