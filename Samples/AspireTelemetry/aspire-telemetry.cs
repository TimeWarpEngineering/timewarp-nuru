#!/usr/bin/dotnet --
// aspire-telemetry - Demonstrates OpenTelemetry integration with Aspire Dashboard
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../Source/TimeWarp.Nuru.Telemetry/TimeWarp.Nuru.Telemetry.csproj
#:package Mediator.Abstractions
#:package Mediator.SourceGenerator

using TimeWarp.Nuru;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using static System.Console;

// Aspire Telemetry Sample
// =======================
// This sample demonstrates how to send telemetry (traces, metrics, logs) from a
// Nuru CLI application to the standalone Aspire Dashboard with minimal setup.
//
// Prerequisites:
// 1. Start Aspire Dashboard:
//    docker run --rm -it -p 18888:18888 -p 4317:18889 --name aspire-dashboard \
//      mcr.microsoft.com/dotnet/aspire-dashboard:latest
//
// 2. Set environment variables (optional - without these, telemetry export is disabled):
//    Bash/Zsh:   export OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
//                export OTEL_SERVICE_NAME=nuru-aspire-sample
//    PowerShell: $env:OTEL_EXPORTER_OTLP_ENDPOINT = "http://localhost:4317"
//                $env:OTEL_SERVICE_NAME = "nuru-aspire-sample"
//
// 3. Run this sample and execute commands
// 4. Open http://localhost:18888 to view telemetry (copy login token from container output)
//
// When OTEL_EXPORTER_OTLP_ENDPOINT is not set, console logging works but OTLP export is disabled.

// =============================================================================
// SIMPLE ONE-LINE TELEMETRY SETUP WITH CreateBuilder
// =============================================================================

NuruCoreApp app = NuruApp.CreateBuilder(args)
  // UseTelemetry() handles ALL telemetry configuration:
  // - Console logging with timestamps
  // - OTLP export to any compatible backend (Aspire, Jaeger, Zipkin, etc.)
  // - Distributed tracing via ActivitySource
  // - Metrics via Meter
  .UseTelemetry()
  .ConfigureServices
  (
    services =>
    {
      services.AddMediator();

      // Register TelemetryBehavior for automatic command instrumentation
      // The pre-built behavior from TimeWarp.Nuru.Telemetry uses shared ActivitySource/Meter
      services.AddSingleton<IPipelineBehavior<GreetCommand, Unit>, TelemetryBehavior<GreetCommand, Unit>>();
      services.AddSingleton<IPipelineBehavior<WorkCommand, Unit>, TelemetryBehavior<WorkCommand, Unit>>();
      services.AddSingleton<IPipelineBehavior<FailCommand, Unit>, TelemetryBehavior<FailCommand, Unit>>();
      services.AddSingleton<IPipelineBehavior<StatusCommand, Unit>, TelemetryBehavior<StatusCommand, Unit>>();
    }
  )
  .Map<GreetCommand>(pattern: "greet {name}", description: "Greet someone (demonstrates basic telemetry)")
  .Map<WorkCommand>(pattern: "work {duration:int}", description: "Simulate work with specified duration in ms")
  .Map<FailCommand>(pattern: "fail {message}", description: "Throw an exception (demonstrates error telemetry)")
  .Map<StatusCommand>(pattern: "status", description: "Show telemetry configuration status")
  .Build();

int exitCode = await app.RunAsync(args);

// Flush telemetry before exit - critical for CLI apps!
await NuruTelemetryExtensions.FlushAndShutdownAsync();

return exitCode;

// =============================================================================
// COMMANDS (unchanged - business logic stays clean)
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
    public ValueTask<Unit> Handle(StatusCommand request, CancellationToken cancellationToken)
    {
      string? otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
      string serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? "nuru-cli";
      bool telemetryEnabled = !string.IsNullOrEmpty(otlpEndpoint);

      WriteLine($"Telemetry Export Enabled: {telemetryEnabled}");
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
      return default;
    }
  }
}
