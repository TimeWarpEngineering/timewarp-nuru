#!/usr/bin/env -S dotnet run --launch-profile AppHost --
#:package Mediator.Abstractions
#:package Mediator.SourceGenerator
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../Source/TimeWarp.Nuru.Repl/TimeWarp.Nuru.Repl.csproj
#:project ../../Source/TimeWarp.Nuru.Telemetry/TimeWarp.Nuru.Telemetry.csproj

// Nuru REPL Client with OpenTelemetry for Aspire Host
// ====================================================
// This sample demonstrates:
// - Nuru implements IHostApplicationBuilder for seamless Aspire integration
// - Extension methods targeting IHostApplicationBuilder work with NuruAppBuilder
// - Dual output: Console.WriteLine for user feedback, ILogger for telemetry
// - Telemetry flows to Aspire Dashboard via OpenTelemetry
//
// Run with Docker dashboard:
//   OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317 ./nuru-client.cs
//
// Run with AppHost dashboard:
//   OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:19034 ./nuru-client.cs

using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using TimeWarp.Nuru;
using TimeWarp.Nuru.Repl;

// Build the Nuru app with telemetry and REPL support
// NuruAppBuilder implements IHostApplicationBuilder, so Aspire-style extensions work!
NuruAppBuilder builder = NuruApp.CreateBuilder
(
  args,
  new NuruAppOptions
  {
    ConfigureRepl = options =>
    {
      options.Prompt = "otel> ";
      options.WelcomeMessage =
        "Aspire Host + OpenTelemetry + Nuru REPL Demo\n" +
        "=============================================\n" +
        "\n" +
        "COMMANDS:\n" +
        "  greet Alice      - Greet someone (watch Aspire Dashboard logs)\n" +
        "  status           - Show system status\n" +
        "  work 500         - Simulate 500ms work (watch traces)\n" +
        "  config           - Show telemetry configuration\n" +
        "\n" +
        "NuruAppBuilder implements IHostApplicationBuilder!\n" +
        "This enables Aspire-style extension methods to work directly.\n" +
        "\n" +
        "Type 'help' for all commands, 'exit' to quit.";
      options.GoodbyeMessage = "Goodbye! Check Aspire Dashboard for telemetry data.";
    }
  }
);

// Demonstrate IHostApplicationBuilder integration:
// Extension methods targeting IHostApplicationBuilder work with NuruAppBuilder!
builder.AddNuruClientDefaults();

builder
  .ConfigureServices
  (
    services =>
    {
      services.AddMediator();

      // Register TelemetryBehavior for automatic command instrumentation
      services.AddSingleton<IPipelineBehavior<GreetCommand, Unit>, TelemetryBehavior<GreetCommand, Unit>>();
      services.AddSingleton<IPipelineBehavior<StatusCommand, Unit>, TelemetryBehavior<StatusCommand, Unit>>();
      services.AddSingleton<IPipelineBehavior<WorkCommand, Unit>, TelemetryBehavior<WorkCommand, Unit>>();
      services.AddSingleton<IPipelineBehavior<ConfigCommand, Unit>, TelemetryBehavior<ConfigCommand, Unit>>();
    }
  )
  // Commands - all use structured ILogger, not Console.WriteLine
  .Map<GreetCommand>(pattern: "greet {name}", description: "Greet someone (structured log)")
  .Map<StatusCommand>(pattern: "status", description: "Show system status (structured log)")
  .Map<WorkCommand>(pattern: "work {duration:int}", description: "Simulate work with duration in ms")
  .Map<ConfigCommand>(pattern: "config", description: "Show telemetry configuration");

NuruCoreApp app = builder.Build();

// Run the app - enters REPL if no args, otherwise executes command
int exitCode;
if (args.Length == 0)
{
  exitCode = await app.RunReplAsync();
}
else
{
  exitCode = await app.RunAsync(args);
}

// Flush telemetry before exit - critical for CLI apps!
await NuruTelemetryExtensions.FlushAndShutdownAsync();

return exitCode;

// =============================================================================
// ASPIRE-STYLE EXTENSION METHOD
// =============================================================================
// This demonstrates that IHostApplicationBuilder extensions work with NuruAppBuilder.
// In a real Aspire project, you'd use the shared AppDefaults library.

/// <summary>
/// Extension methods for configuring Nuru client apps with OpenTelemetry.
/// These work because NuruAppBuilder implements IHostApplicationBuilder.
/// </summary>
public static class NuruClientDefaultsExtensions
{
  /// <summary>
  /// Adds default configuration for Nuru client apps.
  /// This pattern mirrors Aspire's AddAppDefaults() extension.
  /// </summary>
  public static IHostApplicationBuilder AddNuruClientDefaults(this IHostApplicationBuilder builder)
  {
    builder.ConfigureNuruOpenTelemetry();
    return builder;
  }

  /// <summary>
  /// Configures OpenTelemetry for the Nuru client.
  /// </summary>
  public static IHostApplicationBuilder ConfigureNuruOpenTelemetry(this IHostApplicationBuilder builder)
  {
    // Configure logging with OpenTelemetry
    builder.Logging.AddOpenTelemetry(logging =>
    {
      logging.IncludeFormattedMessage = true;
      logging.IncludeScopes = true;
    });

    // Configure OpenTelemetry services
    builder.Services.AddOpenTelemetry()
      .WithTracing(tracing =>
      {
        tracing.AddSource(builder.Environment.ApplicationName);
      });

    // Add OTLP exporter if endpoint is configured
    builder.AddOpenTelemetryExporters();

    return builder;
  }

  private static IHostApplicationBuilder AddOpenTelemetryExporters(this IHostApplicationBuilder builder)
  {
    string? otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
    bool useOtlpExporter = !string.IsNullOrWhiteSpace(otlpEndpoint);

    if (useOtlpExporter)
    {
      builder.Services.AddOpenTelemetry().UseOtlpExporter();
    }

    return builder;
  }
}

// =============================================================================
// COMMANDS - Using structured ILogger, NOT Console.WriteLine
// =============================================================================

/// <summary>
/// Greet command demonstrating structured logging with semantic properties.
/// The log message flows to Aspire Dashboard via OTEL pipeline.
/// </summary>
public sealed class GreetCommand : IRequest
{
  public string Name { get; set; } = string.Empty;

  public sealed class Handler(ILogger<GreetCommand> logger) : IRequestHandler<GreetCommand>
  {
    public ValueTask<Unit> Handle(GreetCommand request, CancellationToken cancellationToken)
    {
      // Console.WriteLine for user feedback (visible in terminal)
      Console.WriteLine($"Hello, {request.Name}!");

      // ILogger for telemetry (flows to Aspire Dashboard via OTLP)
      logger.LogInformation("Greeting {Name} at {Timestamp}", request.Name, DateTime.UtcNow);

      return default;
    }
  }
}

/// <summary>
/// Status command showing system information with structured logging.
/// </summary>
public sealed class StatusCommand : IRequest
{
  public sealed class Handler(ILogger<StatusCommand> logger) : IRequestHandler<StatusCommand>
  {
    public ValueTask<Unit> Handle(StatusCommand request, CancellationToken cancellationToken)
    {
      // Console.WriteLine for user feedback (visible in terminal)
      Console.WriteLine($"Machine: {Environment.MachineName}");
      Console.WriteLine($"Process ID: {Environment.ProcessId}");
      Console.WriteLine($"Runtime: {Environment.Version}");

      // ILogger for telemetry (flows to Aspire Dashboard via OTLP)
      logger.LogInformation
      (
        "System status: MachineName={MachineName}, ProcessId={ProcessId}, Runtime={Runtime}",
        Environment.MachineName,
        Environment.ProcessId,
        Environment.Version
      );

      return default;
    }
  }
}

/// <summary>
/// Work command simulating async work - demonstrates trace spans in Aspire Dashboard.
/// </summary>
public sealed class WorkCommand : IRequest
{
  public int Duration { get; set; }

  public sealed class Handler(ILogger<WorkCommand> logger) : IRequestHandler<WorkCommand>
  {
    public async ValueTask<Unit> Handle(WorkCommand request, CancellationToken cancellationToken)
    {
      // Console.WriteLine for user feedback (visible in terminal)
      Console.WriteLine($"Starting work for {request.Duration}ms...");

      // ILogger for telemetry (flows to Aspire Dashboard via OTLP)
      logger.LogInformation("Starting work for {Duration}ms", request.Duration);

      await Task.Delay(request.Duration, cancellationToken);

      // Console.WriteLine for user feedback (visible in terminal)
      Console.WriteLine($"Work completed after {request.Duration}ms");

      // ILogger for telemetry (flows to Aspire Dashboard via OTLP)
      logger.LogInformation("Work completed after {Duration}ms", request.Duration);

      return Unit.Value;
    }
  }
}

/// <summary>
/// Config command showing current telemetry configuration.
/// </summary>
public sealed class ConfigCommand : IRequest
{
  public sealed class Handler(ILogger<ConfigCommand> logger) : IRequestHandler<ConfigCommand>
  {
    public ValueTask<Unit> Handle(ConfigCommand request, CancellationToken cancellationToken)
    {
      string? otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
      string serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME") ?? "nuru-client";
      bool telemetryEnabled = !string.IsNullOrEmpty(otlpEndpoint);

      // Console.WriteLine for user feedback (visible in terminal)
      Console.WriteLine($"Telemetry Enabled: {telemetryEnabled}");
      Console.WriteLine($"OTLP Endpoint: {otlpEndpoint ?? "(not set)"}");
      Console.WriteLine($"Service Name: {serviceName}");

      // ILogger for telemetry (flows to Aspire Dashboard via OTLP)
      logger.LogInformation
      (
        "Telemetry config: Enabled={TelemetryEnabled}, Endpoint={OtlpEndpoint}, ServiceName={ServiceName}",
        telemetryEnabled,
        otlpEndpoint ?? "(not set)",
        serviceName
      );

      return default;
    }
  }
}
