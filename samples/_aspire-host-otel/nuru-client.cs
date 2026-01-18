#!/usr/bin/env -S dotnet run --launch-profile AppHost --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Nuru CLI Client with OpenTelemetry for Aspire Host
// ===================================================
// This sample demonstrates:
// - Mediator commands with TelemetryBehavior pipeline
// - Auto-wired OpenTelemetry via NuruApp.CreateBuilder()
// - Dual output: Console.WriteLine for user feedback, ILogger for telemetry
// - Telemetry flows to Aspire Dashboard via OTLP
//
// Run standalone with OTLP endpoint:
//   OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317 ./nuru-client.cs greet Alice
//
// Run via AppHost (telemetry auto-configured):
//   ./apphost.cs

using TimeWarp.Nuru;

// Build the Nuru app with auto-wired telemetry and REPL support
NuruCoreApp app = NuruApp.CreateBuilder(args)
  .UseTelemetry()  // Configures OTLP export when OTEL_EXPORTER_OTLP_ENDPOINT is set
  .ConfigureServices(ConfigureServices)
  .AddBehavior(typeof(TelemetryBehavior))
  .DiscoverEndpoints()
  .AddRepl(options =>
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
      "Type 'help' for all commands, 'exit' to quit.";
    options.GoodbyeMessage = "Goodbye! Check Aspire Dashboard for telemetry data.";
  })
  .Build();

// Run the app - use -i or --interactive to enter REPL mode
// Telemetry is automatically flushed by NuruApp.RunAsync()
return await app.RunAsync(args);


static void ConfigureServices(IServiceCollection services)
{
  services.AddLogging(builder => builder
    .SetMinimumLevel(LogLevel.Debug)
    .AddConsole());
}

// =============================================================================
// COMMANDS - Using structured ILogger, NOT Console.WriteLine
// =============================================================================

/// <summary>
/// Greet command demonstrating structured logging with semantic properties.
/// The log message flows to Aspire Dashboard via OTEL pipeline.
/// </summary>
[NuruRoute("greet", Description = "Greet a user by name")]

public sealed class GreetCommand : ICommand<Unit>
{
  [Parameter(Order = 0)]
  public string Name { get; set; } = string.Empty;

  public sealed class Handler(ILogger<GreetCommand> logger) : ICommandHandler<GreetCommand,Unit>
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
[NuruRoute("status", Description = "Show system status (structured log)")]
public sealed class StatusCommand : IQuery<Unit>
{
  public sealed class Handler(ILogger<StatusCommand> logger) : IQueryHandler<StatusCommand, Unit>
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
[NuruRoute("work {duration:int}", Description = "Simulate work with duration in ms")]
public sealed class WorkCommand : ICommand<Unit>
{
  public int Duration { get; set; }

  public sealed class Handler(ILogger<WorkCommand> logger) : ICommandHandler<WorkCommand, Unit>
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
[NuruRoute("config", Description = "Show current telemetry configuration")]
public sealed class ConfigCommand : IQuery<Unit>
{
  public sealed class Handler(ILogger<ConfigCommand> logger) : IQueryHandler<ConfigCommand, Unit>
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
