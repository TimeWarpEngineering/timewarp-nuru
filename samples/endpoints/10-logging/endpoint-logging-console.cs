#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// ═══════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - CONSOLE LOGGING ⭐ RECOMMENDED
// ═══════════════════════════════════════════════════════════════════════════════
//
// Console logging integration with Microsoft.Extensions.Logging using Endpoint DSL.
//
// DSL: Endpoint with ILogger<T> constructor injection
//
// PATTERN:
//   - Add logging via ConfigureServices()
//   - Inject ILogger<T> into handlers
//   - Log at different levels (Debug, Info, Warning, Error)
// ═══════════════════════════════════════════════════════════════════════════════

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TimeWarp.Nuru;
using static System.Console;

NuruApp app = NuruApp.CreateBuilder()
  .ConfigureServices(services =>
  {
    services.AddLogging(builder => builder.AddConsole());
  })
  .DiscoverEndpoints()
  .Build();

return await app.RunAsync(args);

// =============================================================================
// ENDPOINT DEFINITIONS
// =============================================================================

[NuruRoute("process", Description = "Process an item with logging")]
public sealed class ProcessCommand : ICommand<Unit>
{
  [Parameter] public string Item { get; set; } = "";

  public sealed class Handler(ILogger<ProcessCommand> Logger) : ICommandHandler<ProcessCommand, Unit>
  {
    public ValueTask<Unit> Handle(ProcessCommand c, CancellationToken ct)
    {
      Logger.LogInformation("Starting processing of {Item}", c.Item);

      try
      {
        Logger.LogDebug("Validating item {Item}", c.Item);

        if (string.IsNullOrWhiteSpace(c.Item))
        {
          Logger.LogWarning("Empty item provided");
          throw new ArgumentException("Item cannot be empty");
        }

        Logger.LogInformation("Processing {Item}...", c.Item);
        // Simulate work
        Thread.Sleep(100);

        Logger.LogInformation("Successfully processed {Item}", c.Item);
      }
      catch (Exception ex)
      {
        Logger.LogError(ex, "Failed to process {Item}", c.Item);
        throw;
      }

      return default;
    }
  }
}

[NuruRoute("batch", Description = "Process multiple items with batch logging")]
public sealed class BatchCommand : ICommand<Unit>
{
  [Parameter(IsCatchAll = true)] public string[] Items { get; set; } = [];

  public sealed class Handler(ILogger<BatchCommand> Logger) : ICommandHandler<BatchCommand, Unit>
  {
    public ValueTask<Unit> Handle(BatchCommand c, CancellationToken ct)
    {
      Logger.LogInformation("Starting batch processing of {Count} items", c.Items.Length);

      int successCount = 0;
      int failCount = 0;

      foreach (string item in c.Items)
      {
        Logger.LogDebug("Processing {Item}", item);

        if (Random.Shared.Next(10) > 7) // 30% failure rate for demo
        {
          Logger.LogError("Failed to process {Item}", item);
          failCount++;
        }
        else
        {
          Logger.LogInformation("Successfully processed {Item}", item);
          successCount++;
        }
      }

      Logger.LogInformation(
        "Batch complete: {Success} succeeded, {Failed} failed",
        successCount,
        failCount
      );

      return default;
    }
  }
}

[NuruRoute("status", Description = "Check system status with structured logging")]
public sealed class StatusQuery : IQuery<Unit>
{
  public sealed class Handler(ILogger<StatusQuery> Logger) : IQueryHandler<StatusQuery, Unit>
  {
    public ValueTask<Unit> Handle(StatusQuery q, CancellationToken ct)
    {
      using IDisposable? scope = Logger.BeginScope(new Dictionary<string, object>
      {
        ["QueryId"] = Guid.NewGuid().ToString()[..8],
        ["Timestamp"] = DateTime.UtcNow
      });

      Logger.LogInformation("Executing status check");

      // Log structured data
      Logger.LogInformation(
        "System status: {CpuPercent}% CPU, {MemoryMb}MB memory, {UptimeHours}h uptime",
        Random.Shared.Next(10, 50),
        Random.Shared.Next(100, 1000),
        Random.Shared.Next(1, 100)
      );

      WriteLine("System status logged. Check console output.");

      return default;
    }
  }
}
