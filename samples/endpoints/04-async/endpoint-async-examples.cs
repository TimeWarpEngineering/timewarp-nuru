#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// ═══════════════════════════════════════════════════════════════════════════════
// ENDPOINT DSL - ASYNC EXAMPLES ⭐ RECOMMENDED
// ═══════════════════════════════════════════════════════════════════════════════
// Demonstrates async command handlers using the Endpoint DSL pattern.
//
// DSL: Endpoint (class-based with async handlers using ValueTask)
//
// PATTERNS DEMONSTRATED:
//   - Async handlers with ValueTask
//   - CancellationToken support
//   - Task-based async operations
//   - Async I/O simulation
// ═══════════════════════════════════════════════════════════════════════════════

using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .Build();

await app.RunAsync(args);

// =============================================================================
// BASIC ASYNC COMMANDS
// =============================================================================

[NuruRoute("delay", Description = "Async delay command with milliseconds")]
public sealed class DelayCommand : ICommand<Unit>
{
  [Parameter(Description = "Milliseconds to delay")]
  public int Ms { get; set; }

  public sealed class Handler : ICommandHandler<DelayCommand, Unit>
  {
    public async ValueTask<Unit> Handle(DelayCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Starting {command.Ms}ms delay...");
      await Task.Delay(command.Ms, ct);
      Console.WriteLine("Delay complete!");
      return default;
    }
  }
}

[NuruRoute("fetch", Description = "Simulate async HTTP fetch")]
public sealed class FetchCommand : ICommand<string>
{
  [Parameter(Description = "URL to fetch")]
  public string Url { get; set; } = string.Empty;

  public sealed class Handler : ICommandHandler<FetchCommand, string>
  {
    public async ValueTask<string> Handle(FetchCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Fetching {command.Url}...");
      await Task.Delay(100, ct); // Simulate network delay
      return $"Content from {command.Url}";
    }
  }
}

// =============================================================================
// CANCELLATION SUPPORT
// =============================================================================

[NuruRoute("long-running", Description = "Long-running operation with cancellation support")]
public sealed class LongRunningCommand : ICommand<Unit>
{
  [Parameter(Description = "Number of iterations")]
  public int Iterations { get; set; } = 10;

  public sealed class Handler : ICommandHandler<LongRunningCommand, Unit>
  {
    public async ValueTask<Unit> Handle(LongRunningCommand command, CancellationToken ct)
    {
      for (int i = 0; i < command.Iterations; i++)
      {
        ct.ThrowIfCancellationRequested();
        Console.WriteLine($"Iteration {i + 1}/{command.Iterations}");
        await Task.Delay(100, ct);
      }

      Console.WriteLine("Long-running operation complete!");
      return default;
    }
  }
}

// =============================================================================
// ASYNC I/O SIMULATION
// =============================================================================

[NuruRoute("read-file", Description = "Simulate async file read")]
public sealed class ReadFileCommand : ICommand<string>
{
  [Parameter(Description = "File path to read")]
  public string Path { get; set; } = string.Empty;

  public sealed class Handler : ICommandHandler<ReadFileCommand, string>
  {
    public async ValueTask<string> Handle(ReadFileCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Reading file: {command.Path}");
      await Task.Delay(50, ct); // Simulate I/O
      return $"Contents of {command.Path}";
    }
  }
}

[NuruRoute("process-batch", Description = "Process items in batches asynchronously")]
public sealed class ProcessBatchCommand : ICommand<Unit>
{
  [Parameter(IsCatchAll = true, Description = "Items to process")]
  public string[] Items { get; set; } = [];

  [Option("parallel", "p", Description = "Process in parallel")]
  public bool Parallel { get; set; }

  public sealed class Handler : ICommandHandler<ProcessBatchCommand, Unit>
  {
    public async ValueTask<Unit> Handle(ProcessBatchCommand command, CancellationToken ct)
    {
      Console.WriteLine($"Processing {command.Items.Length} items...");

      if (command.Parallel)
      {
        await Parallel.ForEachAsync(
          command.Items,
          new ParallelOptions { CancellationToken = ct, MaxDegreeOfParallelism = 4 },
          async (item, token) =>
          {
            await Task.Delay(10, token);
            Console.WriteLine($"  Processed: {item}");
          }
        );
      }
      else
      {
        foreach (string item in command.Items)
        {
          ct.ThrowIfCancellationRequested();
          await Task.Delay(10, ct);
          Console.WriteLine($"  Processed: {item}");
        }
      }

      Console.WriteLine("Batch processing complete!");
      return default;
    }
  }
}

// =============================================================================
// ASYNC QUERY EXAMPLES
// =============================================================================

[NuruRoute("search", Description = "Search with async results")]
public sealed class SearchQuery : IQuery<SearchResult[]>
{
  [Parameter(Description = "Search query")]
  public string Query { get; set; } = string.Empty;

  [Option("limit", "l", Description = "Maximum results")]
  public int Limit { get; set; } = 10;

  public sealed class Handler : IQueryHandler<SearchQuery, SearchResult[]>
  {
    public async ValueTask<SearchResult[]> Handle(SearchQuery query, CancellationToken ct)
    {
      Console.WriteLine($"Searching for: {query.Query}");
      await Task.Delay(100, ct); // Simulate search

      // Generate fake results
      SearchResult[] results = Enumerable.Range(1, Math.Min(query.Limit, 5))
        .Select(i => new SearchResult { Id = i, Title = $"Result {i} for '{query.Query}'" })
        .ToArray();

      return results;
    }
  }
}

[NuruRoute("health-check", Description = "Perform health check on services")]
public sealed class HealthCheckQuery : IQuery<HealthStatus>
{
  [Parameter(IsCatchAll = true, Description = "Services to check")]
  public string[] Services { get; set; } = [];

  public sealed class Handler : IQueryHandler<HealthCheckQuery, HealthStatus>
  {
    public async ValueTask<HealthStatus> Handle(HealthCheckQuery query, CancellationToken ct)
    {
      Dictionary<string, bool> statuses = new Dictionary<string, bool>();

      foreach (string service in query.Services.Length > 0 ? query.Services : ["database", "api", "cache"])
      {
        ct.ThrowIfCancellationRequested();
        await Task.Delay(50, ct); // Simulate health check
        statuses[service] = Random.Shared.Next(0, 10) > 2; // 80% healthy
      }

      return new HealthStatus
      {
        OverallHealthy = statuses.Values.All(s => s),
        Services = statuses
      };
    }
  }
}

// =============================================================================
// SUPPORTING TYPES
// =============================================================================

public class SearchResult
{
  public int Id { get; set; }
  public string Title { get; set; } = string.Empty;
}

public class HealthStatus
{
  public bool OverallHealthy { get; set; }
  public Dictionary<string, bool> Services { get; set; } = new Dictionary<string, bool>();
}
