// ═══════════════════════════════════════════════════════════════════════════════
// DB QUERY
// ═══════════════════════════════════════════════════════════════════════════════
// Simulate database query with telemetry.

namespace PipelineTelemetry.Endpoints;

using TimeWarp.Nuru;

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
      Console.WriteLine($"Querying {query.Table} (limit: {query.Limit})");
      await Task.Delay(50, ct);

      string[] results = Enumerable.Range(1, Math.Min(query.Limit, 3))
        .Select(i => $"Row {i} from {query.Table}")
        .ToArray();

      Console.WriteLine($"Retrieved {results.Length} rows");
      return results;
    }
  }
}
