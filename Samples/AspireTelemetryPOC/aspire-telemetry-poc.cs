#!/usr/bin/dotnet --
// aspire-telemetry-poc - Demonstrates OpenTelemetry integration with Aspire Dashboard
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../Source/TimeWarp.Nuru.Logging/TimeWarp.Nuru.Logging.csproj
#:package OpenTelemetry@1.10.0
#:package OpenTelemetry.Exporter.OpenTelemetryProtocol@1.10.0
#:package OpenTelemetry.Extensions.Hosting@1.10.0

using System.Diagnostics;
using System.Diagnostics.Metrics;
using TimeWarp.Nuru;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using static System.Console;

// Aspire Telemetry POC
// ====================
// This sample demonstrates how to send telemetry (traces, metrics, logs) from a
// Nuru CLI application to the standalone Aspire Dashboard.
//
// Prerequisites:
// 1. Start Aspire Dashboard:
//    docker run --rm -it -p 18888:18888 -p 4317:18889 --name aspire-dashboard \
//      mcr.microsoft.com/dotnet/aspire-dashboard:latest
//
// 2. Set environment variables:
//    export OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
//    export OTEL_SERVICE_NAME=nuru-telemetry-poc
//
// 3. Run this sample and execute commands
// 4. Open http://localhost:18888 to view telemetry (copy login token from container output)
//
// When OTEL_EXPORTER_OTLP_ENDPOINT is not set, telemetry is disabled with zero overhead.

// =============================================================================
// TELEMETRY CONFIGURATION
// =============================================================================

// Check if OTLP endpoint is configured
string? otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
string serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? "nuru-telemetry-poc";
bool telemetryEnabled = !string.IsNullOrEmpty(otlpEndpoint);

// Define telemetry sources (these exist regardless of OTLP config)
ActivitySource activitySource = new("TimeWarp.Nuru.POC", "1.0.0");
Meter meter = new("TimeWarp.Nuru.POC", "1.0.0");

// Create metrics instruments
Counter<int> commandCounter = meter.CreateCounter<int>(
  name: "nuru.commands.invoked",
  unit: "{commands}",
  description: "Number of commands executed");

Counter<int> errorCounter = meter.CreateCounter<int>(
  name: "nuru.commands.errors",
  unit: "{errors}",
  description: "Number of failed commands");

Histogram<double> commandDuration = meter.CreateHistogram<double>(
  name: "nuru.commands.duration",
  unit: "ms",
  description: "Command execution duration in milliseconds");

// Configure OpenTelemetry only when OTLP endpoint is set
TracerProvider? tracerProvider = null;
MeterProvider? meterProvider = null;

if (telemetryEnabled)
{
  WriteLine($"[TELEMETRY] Enabled - sending to {otlpEndpoint}");
  WriteLine($"[TELEMETRY] Service name: {serviceName}");

  ResourceBuilder resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService(serviceName: serviceName, serviceVersion: "1.0.0");

  // Configure tracing
  tracerProvider = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(resourceBuilder)
    .AddSource(activitySource.Name)
    .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint))
    .Build();

  // Configure metrics
  meterProvider = Sdk.CreateMeterProviderBuilder()
    .SetResourceBuilder(resourceBuilder)
    .AddMeter(meter.Name)
    .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint))
    .Build();
}
else
{
  WriteLine("[TELEMETRY] Disabled - set OTEL_EXPORTER_OTLP_ENDPOINT to enable");
}

// =============================================================================
// APPLICATION SETUP
// =============================================================================

NuruApp app = new NuruAppBuilder()
  .UseConsoleLogging(LogLevel.Information)
  .ConfigureLogging(logging =>
  {
    // Add OpenTelemetry logging when enabled
    if (telemetryEnabled)
    {
      logging.AddOpenTelemetry(options =>
      {
        options.SetResourceBuilder(ResourceBuilder.CreateDefault()
          .AddService(serviceName: serviceName, serviceVersion: "1.0.0"));
        options.AddOtlpExporter(exporterOptions =>
          exporterOptions.Endpoint = new Uri(otlpEndpoint!));
      });
    }
  })
  // Greet command - demonstrates basic telemetry
  .Map
  (
    pattern: "greet {name}",
    handler: (string name) => ExecuteWithTelemetry("greet", () =>
    {
      WriteLine($"Hello, {name}!");
    }),
    description: "Greet someone (demonstrates basic telemetry)"
  )
  // Work command - demonstrates longer duration
  .Map
  (
    pattern: "work {duration:int}",
    handler: async (int duration) => await ExecuteWithTelemetryAsync("work", async () =>
    {
      WriteLine($"Starting work for {duration}ms...");
      await Task.Delay(duration);
      WriteLine("Work completed!");
    }),
    description: "Simulate work with specified duration in ms"
  )
  // Fail command - demonstrates error telemetry
  .Map
  (
    pattern: "fail {message}",
    handler: (string message) => ExecuteWithTelemetry("fail", () =>
    {
      throw new InvalidOperationException(message);
    }),
    description: "Throw an exception (demonstrates error telemetry)"
  )
  // Status command - shows telemetry configuration
  .Map
  (
    pattern: "status",
    handler: () =>
    {
      WriteLine($"Telemetry Enabled: {telemetryEnabled}");
      if (telemetryEnabled)
      {
        WriteLine($"OTLP Endpoint: {otlpEndpoint}");
        WriteLine($"Service Name: {serviceName}");
        WriteLine("Dashboard: http://localhost:18888");
      }
      else
      {
        WriteLine("Set OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317 to enable");
      }
    },
    description: "Show telemetry configuration status"
  )
  .AddAutoHelp()
  .Build();

int exitCode = await app.RunAsync(args);

// Clean up OpenTelemetry providers
tracerProvider?.Dispose();
meterProvider?.Dispose();

return exitCode;

// =============================================================================
// TELEMETRY HELPER METHODS
// =============================================================================

void ExecuteWithTelemetry(string commandName, Action action)
{
  using Activity? activity = activitySource.StartActivity(commandName, ActivityKind.Internal);
  activity?.SetTag("command.name", commandName);

  Stopwatch stopwatch = Stopwatch.StartNew();

  try
  {
    action();

    stopwatch.Stop();
    activity?.SetStatus(ActivityStatusCode.Ok);

    commandCounter.Add(1, new KeyValuePair<string, object?>("command", commandName));
    commandDuration.Record(stopwatch.ElapsedMilliseconds,
      new KeyValuePair<string, object?>("command", commandName));
  }
  catch (Exception ex)
  {
    stopwatch.Stop();
    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
    activity?.SetTag("error.type", ex.GetType().Name);
    activity?.SetTag("error.message", ex.Message);

    errorCounter.Add(1,
      new KeyValuePair<string, object?>("command", commandName),
      new KeyValuePair<string, object?>("error.type", ex.GetType().Name));

    commandDuration.Record(stopwatch.ElapsedMilliseconds,
      new KeyValuePair<string, object?>("command", commandName),
      new KeyValuePair<string, object?>("status", "error"));

    throw;
  }
}

async Task ExecuteWithTelemetryAsync(string commandName, Func<Task> action)
{
  using Activity? activity = activitySource.StartActivity(commandName, ActivityKind.Internal);
  activity?.SetTag("command.name", commandName);

  Stopwatch stopwatch = Stopwatch.StartNew();

  try
  {
    await action();

    stopwatch.Stop();
    activity?.SetStatus(ActivityStatusCode.Ok);

    commandCounter.Add(1, new KeyValuePair<string, object?>("command", commandName));
    commandDuration.Record(stopwatch.ElapsedMilliseconds,
      new KeyValuePair<string, object?>("command", commandName));
  }
  catch (Exception ex)
  {
    stopwatch.Stop();
    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
    activity?.SetTag("error.type", ex.GetType().Name);
    activity?.SetTag("error.message", ex.Message);

    errorCounter.Add(1,
      new KeyValuePair<string, object?>("command", commandName),
      new KeyValuePair<string, object?>("error.type", ex.GetType().Name));

    commandDuration.Record(stopwatch.ElapsedMilliseconds,
      new KeyValuePair<string, object?>("command", commandName),
      new KeyValuePair<string, object?>("status", "error"));

    throw;
  }
}
