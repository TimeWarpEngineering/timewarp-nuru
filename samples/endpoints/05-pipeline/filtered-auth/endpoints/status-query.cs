// ═══════════════════════════════════════════════════════════════════════════════
// STATUS QUERY
// ═══════════════════════════════════════════════════════════════════════════════
// Check system status (public, no auth required).

namespace PipelineFilteredAuth.Endpoints;

using TimeWarp.Nuru;
using static System.Console;

[NuruRoute("status", Description = "Check system status (public, no auth required)")]
public sealed class StatusQuery : IQuery<Unit>
{
  public sealed class Handler : IQueryHandler<StatusQuery, Unit>
  {
    public ValueTask<Unit> Handle(StatusQuery query, CancellationToken ct)
    {
      WriteLine("System status: ✓ OK");
      return default;
    }
  }
}
