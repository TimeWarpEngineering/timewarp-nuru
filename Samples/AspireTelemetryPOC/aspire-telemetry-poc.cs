#!/usr/bin/dotnet --
// aspire-telemetry-poc - Demonstrates OpenTelemetry integration with Aspire Dashboard using Pipeline Middleware
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../Source/TimeWarp.Nuru.Logging/TimeWarp.Nuru.Logging.csproj
#:project ../../Source/TimeWarp.Nuru.Telemetry/TimeWarp.Nuru.Telemetry.csproj
#:package Mediator.Abstractions
#:package Mediator.SourceGenerator

using System.Diagnostics;
using System.Diagnostics.Metrics;
using TimeWarp.Nuru;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using static System.Console;

// Aspire Telemetry POC with Pipeline Middleware
// ==============================================
// This sample demonstrates how to send telemetry (traces, metrics) from a
// Nuru CLI application to the standalone Aspire Dashboard using the
// recommended Pipeline Middleware pattern.
//
// Prerequisites:
// 1. Start Aspire Dashboard:
//    docker run --rm -it -p 18888:18888 -p 4317:18889 --name aspire-dashboard \
//      mcr.microsoft.com/dotnet/aspire-dashboard:latest
//
// 2. Set environment variables:
//    Bash/Zsh:   export OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
//                export OTEL_SERVICE_NAME=nuru-telemetry-poc
//    PowerShell: $env:OTEL_EXPORTER_OTLP_ENDPOINT = "http://localhost:4317"
//                $env:OTEL_SERVICE_NAME = "nuru-telemetry-poc"
//
// 3. Run this sample and execute commands
// 4. Open http://localhost:18888 to view telemetry (copy login token from container output)
//
// When OTEL_EXPORTER_OTLP_ENDPOINT is not set, telemetry is disabled with zero overhead.

// =============================================================================
// TELEMETRY CONFIGURATION
// =============================================================================

string? otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
string serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? "nuru-telemetry-poc";
bool telemetryEnabled = !string.IsNullOrEmpty(otlpEndpoint);

// Define telemetry sources (shared with TelemetryBehavior)
ActivitySource activitySource = new("TimeWarp.Nuru.POC", "1.0.0");
Meter meter = new("TimeWarp.Nuru.POC", "1.0.0");

Counter<int> commandCounter = meter.CreateCounter<int>("nuru.commands.invoked", "{commands}", "Commands executed");
Counter<int> errorCounter = meter.CreateCounter<int>("nuru.commands.errors", "{errors}", "Failed commands");
Histogram<double> commandDuration = meter.CreateHistogram<double>("nuru.commands.duration", "ms", "Command duration");

// Configure OpenTelemetry providers
TracerProvider? tracerProvider = null;
MeterProvider? meterProvider = null;

if (telemetryEnabled)
{
  WriteLine($"[TELEMETRY] Enabled - sending to {otlpEndpoint}");
  WriteLine($"[TELEMETRY] Service name: {serviceName}");

  ResourceBuilder resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService(serviceName: serviceName, serviceVersion: "1.0.0");

  tracerProvider = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(resourceBuilder)
    .AddSource(activitySource.Name)
    .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint!))
    .Build();

  meterProvider = Sdk.CreateMeterProviderBuilder()
    .SetResourceBuilder(resourceBuilder)
    .AddMeter(meter.Name)
    .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint!))
    .Build();
}
else
{
  WriteLine("[TELEMETRY] Disabled - set OTEL_EXPORTER_OTLP_ENDPOINT to enable");
}

// =============================================================================
// APPLICATION SETUP WITH PIPELINE MIDDLEWARE
// =============================================================================

NuruApp app = new NuruAppBuilder()
  .UseConsoleLogging(LogLevel.Information)
  .AddDependencyInjection()
  .ConfigureServices
  (
    (services, config) =>
    {
      // Register Mediator
      services.AddMediator();

      // Register TelemetryBehavior for all commands - this is the key pattern!
      // The behavior wraps every command automatically, no manual instrumentation needed.
      services.AddSingleton<IPipelineBehavior<GreetCommand, Unit>, TelemetryBehavior<GreetCommand, Unit>>();
      services.AddSingleton<IPipelineBehavior<WorkCommand, Unit>, TelemetryBehavior<WorkCommand, Unit>>();
      services.AddSingleton<IPipelineBehavior<FailCommand, Unit>, TelemetryBehavior<FailCommand, Unit>>();
      services.AddSingleton<IPipelineBehavior<StatusCommand, Unit>, TelemetryBehavior<StatusCommand, Unit>>();

      // Share telemetry instances with the behavior via DI
      services.AddSingleton(activitySource);
      services.AddSingleton(meter);
      services.AddSingleton(commandCounter);
      services.AddSingleton(errorCounter);
      services.AddSingleton(commandDuration);
      services.AddSingleton(new TelemetryConfig(telemetryEnabled, otlpEndpoint, serviceName));
    }
  )
  .Map<GreetCommand>(pattern: "greet {name}", description: "Greet someone (demonstrates basic telemetry)")
  .Map<WorkCommand>(pattern: "work {duration:int}", description: "Simulate work with specified duration in ms")
  .Map<FailCommand>(pattern: "fail {message}", description: "Throw an exception (demonstrates error telemetry)")
  .Map<StatusCommand>(pattern: "status", description: "Show telemetry configuration status")
  .AddAutoHelp()
  .Build();

int exitCode = await app.RunAsync(args);

// Flush telemetry before exit - critical for CLI apps!
if (telemetryEnabled)
{
  WriteLine("[TELEMETRY] Flushing telemetry data...");
  tracerProvider?.ForceFlush();
  meterProvider?.ForceFlush();
  await Task.Delay(1000);
}

tracerProvider?.Dispose();
meterProvider?.Dispose();

return exitCode;

// =============================================================================
// TELEMETRY CONFIG (shared via DI)
// =============================================================================

public record TelemetryConfig(bool Enabled, string? OtlpEndpoint, string ServiceName);

// =============================================================================
// COMMANDS
// =============================================================================

public sealed class GreetCommand : IRequest
{
  public string Name { get; set; } = string.Empty;

  public sealed class Handler : IRequestHandler<GreetCommand>
  {
    public ValueTask<Unit> Handle(GreetCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"Hello, {request.Name}!");
      return default;
    }
  }
}

public sealed class WorkCommand : IRequest
{
  public int Duration { get; set; }

  public sealed class Handler : IRequestHandler<WorkCommand>
  {
    public async ValueTask<Unit> Handle(WorkCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"Starting work for {request.Duration}ms...");
      await Task.Delay(request.Duration, cancellationToken);
      WriteLine("Work completed!");
      return Unit.Value;
    }
  }
}

public sealed class FailCommand : IRequest
{
  public string Message { get; set; } = string.Empty;

  public sealed class Handler : IRequestHandler<FailCommand>
  {
    public ValueTask<Unit> Handle(FailCommand request, CancellationToken cancellationToken)
    {
      throw new InvalidOperationException(request.Message);
    }
  }
}

public sealed class StatusCommand : IRequest
{
  public sealed class Handler : IRequestHandler<StatusCommand>
  {
    private readonly TelemetryConfig Config;

    public Handler(TelemetryConfig config)
    {
      Config = config;
    }

    public ValueTask<Unit> Handle(StatusCommand request, CancellationToken cancellationToken)
    {
      WriteLine($"Telemetry Enabled: {Config.Enabled}");
      if (Config.Enabled)
      {
        WriteLine($"OTLP Endpoint: {Config.OtlpEndpoint}");
        WriteLine($"Service Name: {Config.ServiceName}");
        WriteLine("Dashboard: http://localhost:18888");
      }
      else
      {
        WriteLine("Set OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317 to enable");
      }
      return default;
    }
  }
}

// =============================================================================
// TELEMETRY PIPELINE BEHAVIOR
// =============================================================================

/// <summary>
/// Pipeline behavior that automatically instruments all commands with OpenTelemetry.
/// This is the recommended pattern - telemetry is applied consistently without
/// manual instrumentation in each command handler.
/// </summary>
public sealed class TelemetryBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
  where TMessage : IMessage
{
  private readonly ActivitySource ActivitySource;
  private readonly Counter<int> CommandCounter;
  private readonly Counter<int> ErrorCounter;
  private readonly Histogram<double> CommandDuration;

  public TelemetryBehavior
  (
    ActivitySource activitySource,
    Counter<int> commandCounter,
    Counter<int> errorCounter,
    Histogram<double> commandDuration
  )
  {
    ActivitySource = activitySource;
    CommandCounter = commandCounter;
    ErrorCounter = errorCounter;
    CommandDuration = commandDuration;
  }

  public async ValueTask<TResponse> Handle
  (
    TMessage message,
    MessageHandlerDelegate<TMessage, TResponse> next,
    CancellationToken cancellationToken
  )
  {
    string commandName = typeof(TMessage).Name;

    // Start Activity span for distributed tracing
    using Activity? activity = ActivitySource.StartActivity(commandName, ActivityKind.Internal);
    activity?.SetTag("command.type", typeof(TMessage).FullName);
    activity?.SetTag("command.name", commandName);

    Stopwatch stopwatch = Stopwatch.StartNew();

    try
    {
      TResponse response = await next(message, cancellationToken);

      stopwatch.Stop();
      activity?.SetStatus(ActivityStatusCode.Ok);

      // Record success metrics
      CommandCounter.Add(1, new KeyValuePair<string, object?>("command", commandName));
      CommandDuration.Record(stopwatch.ElapsedMilliseconds,
        new KeyValuePair<string, object?>("command", commandName),
        new KeyValuePair<string, object?>("status", "ok"));

      return response;
    }
    catch (Exception ex)
    {
      stopwatch.Stop();

      // Record error in trace
      activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
      activity?.SetTag("error.type", ex.GetType().Name);
      activity?.SetTag("error.message", ex.Message);

      // Record error metrics
      ErrorCounter.Add(1,
        new KeyValuePair<string, object?>("command", commandName),
        new KeyValuePair<string, object?>("error.type", ex.GetType().Name));

      CommandDuration.Record(stopwatch.ElapsedMilliseconds,
        new KeyValuePair<string, object?>("command", commandName),
        new KeyValuePair<string, object?>("status", "error"));

      throw;
    }
  }
}
