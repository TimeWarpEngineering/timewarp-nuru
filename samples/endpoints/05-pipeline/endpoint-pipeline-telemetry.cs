#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// ═══════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - TELEMETRY PIPELINE ⭐ RECOMMENDED
// ═══════════════════════════════════════════════════════════════════════════════
//
// This sample demonstrates OpenTelemetry-compatible distributed tracing
// using INuruBehavior with Activity spans.
//
// DSL: Endpoint with TelemetryBehavior registered via .AddBehavior()
//
// BEHAVIOR DEMONSTRATED:
//   - TelemetryBehavior: Creates Activity spans for observability
//   - Compatible with OpenTelemetry exporters
//   - Adds tags for command name and correlation ID
// ═══════════════════════════════════════════════════════════════════════════════

using System.Diagnostics;
using TimeWarp.Nuru;
using static System.Console;

NuruApp app = NuruApp.CreateBuilder()
  .AddBehavior(typeof(TelemetryBehavior))
  .DiscoverEndpoints()
  .Build();

await app.RunAsync(args);

// =============================================================================
// TELEMETRY BEHAVIOR
// =============================================================================

/// <summary>
/// Telemetry behavior that creates Activity spans for distributed tracing.
/// Compatible with OpenTelemetry - export traces to Jaeger, Zipkin, etc.
/// </summary>
public sealed class TelemetryBehavior : INuruBehavior
{
  private static readonly ActivitySource ActivitySource = new("TimeWarp.Nuru.Samples");

  public async ValueTask HandleAsync(BehaviorContext context, Func<ValueTask> proceed)
  {
    string activityName = $"nuru.command.{context.CommandName}";

    using Activity? activity = ActivitySource.StartActivity(activityName, ActivityKind.Server);

    if (activity != null)
    {
      activity.SetTag("nuru.command.name", context.CommandName);
      activity.SetTag("nuru.correlation.id", context.CorrelationId);
      activity.SetTag("nuru.command.type", context.Command?.GetType().Name ?? "unknown");
    }

    try
    {
      await proceed();
      activity?.SetStatus(ActivityStatusCode.Ok);
    }
    catch (Exception ex)
    {
      activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
      activity?.RecordException(ex);
      throw;
    }
  }
}

// =============================================================================
// ENDPOINT DEFINITIONS
// =============================================================================

[NuruRoute("api", Description = "Simulate API call with telemetry")]
public sealed class ApiCommand : ICommand<Unit>
{
  [Parameter(Description = "API endpoint")]
  public string Endpoint { get; set; } = string.Empty;

  public sealed class Handler : ICommandHandler<ApiCommand, Unit>
  {
    public async ValueTask<Unit> Handle(ApiCommand command, CancellationToken ct)
    {
      WriteLine($"Calling API: {command.Endpoint}");
      await Task.Delay(100, ct); // Simulate API call
      WriteLine("API call complete");
      return default;
    }
  }
}

[NuruRoute("db-query", Description = "Simulate database query with telemetry")]
public sealed class DbQuery : IQuery<string[]>
{
  [Parameter(Description = "Table to query")]
  public string Table { get; set; } = string.Empty;

  [Option("limit", "l", Description = "Maximum rows")]
  public int Limit { get; set; } = 10;

  public sealed class Handler : IQueryHandler<DbQuery, string[]>
  {
    public async ValueTask<string[]> Handle(DbQuery query, CancellationToken ct)
    {
      WriteLine($"Querying {query.Table} (limit: {query.Limit})");
      await Task.Delay(50, ct); // Simulate DB query

      string[] results = Enumerable.Range(1, Math.Min(query.Limit, 3))
        .Select(i => $"Row {i} from {query.Table}")
        .ToArray();

      WriteLine($"Retrieved {results.Length} rows");
      return results;
    }
  }
}

[NuruRoute("workflow", Description = "Multi-step workflow with nested telemetry")]
public sealed class WorkflowCommand : ICommand<Unit>
{
  [Parameter(Description = "Workflow name")]
  public string Name { get; set; } = string.Empty;

  public sealed class Handler : ICommandHandler<WorkflowCommand, Unit>
  {
    public async ValueTask<Unit> Handle(WorkflowCommand command, CancellationToken ct)
    {
      WriteLine($"Starting workflow: {command.Name}");

      // Step 1
      await Task.Delay(50, ct);
      WriteLine("  ✓ Step 1 complete");

      // Step 2
      await Task.Delay(50, ct);
      WriteLine("  ✓ Step 2 complete");

      // Step 3
      await Task.Delay(50, ct);
      WriteLine("  ✓ Step 3 complete");

      WriteLine($"Workflow '{command.Name}' complete!");
      return default;
    }
  }
}
