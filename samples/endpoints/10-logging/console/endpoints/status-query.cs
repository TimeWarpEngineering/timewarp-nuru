using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using TimeWarp.Nuru;
using static System.Console;

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
