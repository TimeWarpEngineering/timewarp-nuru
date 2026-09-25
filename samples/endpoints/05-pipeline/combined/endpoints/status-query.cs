// ═══════════════════════════════════════════════════════════════════════════════
// STATUS QUERY
// ═══════════════════════════════════════════════════════════════════════════════
// Check system status (public, no filters).

namespace PipelineCombined.Endpoints;

using TimeWarp.Mediator;
using TimeWarp.Nuru;
using static System.Console;

[NuruRoute("status", Description = "Check system status (public)")]
public sealed class StatusQuery : IQuery<Unit>
{
  public sealed class Handler : IQueryHandler<StatusQuery, Unit>
  {
    public Task<Unit> Handle(StatusQuery q, CancellationToken ct)
    {
      WriteLine("✓ System operational");
      return Unit.Task;
    }
  }
}
