// ═══════════════════════════════════════════════════════════════════════════════
// HEALTH-CHECK QUERY
// ═══════════════════════════════════════════════════════════════════════════════
// Perform health check on services.

namespace AsyncExamples.Endpoints.Queries;

using TimeWarp.Nuru;

/// <summary>
/// Health status result for multiple services.
/// </summary>
public class HealthStatus
{
  public bool OverallHealthy { get; set; }
  public Dictionary<string, bool> Services { get; set; } = new Dictionary<string, bool>();
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
