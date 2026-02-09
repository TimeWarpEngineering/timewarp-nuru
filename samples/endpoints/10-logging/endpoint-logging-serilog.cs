#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj
#:package Serilog
#:package Serilog.Extensions.Logging
#:package Serilog.Sinks.Console

// ═══════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - SERILOG STRUCTURED LOGGING ⭐ RECOMMENDED
// ═══════════════════════════════════════════════════════════════════════════════
//
// Serilog integration with structured logging and multiple sinks.
//
// DSL: Endpoint with Serilog logging via Microsoft.Extensions.Logging
//
// FEATURES:
//   - Structured JSON logging
//   - Multiple output formats
//   - Context enrichment
//   - Log level control
// ═══════════════════════════════════════════════════════════════════════════════

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using TimeWarp.Nuru;
using static System.Console;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
  .MinimumLevel.Debug()
  .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
  .Enrich.FromLogContext()
  .CreateLogger();

NuruApp app = NuruApp.CreateBuilder()
  .ConfigureServices(services =>
  {
    services.AddLogging(builder => builder.AddSerilog());
  })
  .DiscoverEndpoints()
  .Build();

try
{
  return await app.RunAsync(args);
}
finally
{
  Log.CloseAndFlush();
}

// =============================================================================
// ENDPOINT DEFINITIONS
// =============================================================================

[NuruRoute("order", Description = "Process an order with structured logging")]
public sealed class OrderCommand : ICommand<Unit>
{
  [Parameter] public string OrderId { get; set; } = "";
  [Parameter] public decimal Amount { get; set; }

  public sealed class Handler(ILogger<OrderCommand> Logger) : ICommandHandler<OrderCommand, Unit>
  {
    public ValueTask<Unit> Handle(OrderCommand c, CancellationToken ct)
    {
      using IDisposable? scope = Logger.BeginScope(new Dictionary<string, object>
      {
        ["OrderId"] = c.OrderId,
        ["Amount"] = c.Amount
      });

      Logger.LogInformation("Processing order {OrderId} for ${Amount}", c.OrderId, c.Amount);

      // Simulate processing
      Logger.LogDebug("Validating order {OrderId}", c.OrderId);
      Thread.Sleep(50);

      Logger.LogDebug("Charging payment for order {OrderId}", c.OrderId);
      Thread.Sleep(50);

      Logger.LogInformation("Order {OrderId} completed successfully", c.OrderId);

      return default;
    }
  }
}

[NuruRoute("audit", Description = "Log audit event with full context")]
public sealed class AuditCommand : ICommand<Unit>
{
  [Parameter] public string Action { get; set; } = "";
  [Parameter] public string Resource { get; set; } = "";

  public sealed class Handler(ILogger<AuditCommand> Logger) : ICommandHandler<AuditCommand, Unit>
  {
    public ValueTask<Unit> Handle(AuditCommand c, CancellationToken ct)
    {
      Logger.LogInformation(
        "Audit: User performed {Action} on {Resource} at {Timestamp}",
        c.Action,
        c.Resource,
        DateTime.UtcNow
      );

      // Log as structured object
      Logger.LogInformation(
        "{@AuditEvent}",
        new
        {
          EventType = "Audit",
            Action = c.Action,
            Resource = c.Resource,
            Timestamp = DateTime.UtcNow,
            User = Environment.UserName
        }
      );

      return default;
    }
  }
}

[NuruRoute("error", Description = "Simulate and log an error")]
public sealed class ErrorCommand : ICommand<Unit>
{
  [Parameter] public string Scenario { get; set; } = "";

  public sealed class Handler(ILogger<ErrorCommand> Logger) : ICommandHandler<ErrorCommand, Unit>
  {
    public ValueTask<Unit> Handle(ErrorCommand c, CancellationToken ct)
    {
      try
      {
        throw c.Scenario.ToLower() switch
        {
          "timeout" => new TimeoutException("Operation timed out"),
          "auth" => new UnauthorizedAccessException("Access denied"),
          _ => new InvalidOperationException($"Unknown error: {c.Scenario}")
        };
      }
      catch (Exception ex)
      {
        Logger.LogError(ex, "Error processing scenario: {Scenario}", c.Scenario);
        throw;
      }
    }
  }
}
