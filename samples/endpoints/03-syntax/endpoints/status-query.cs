// ═══════════════════════════════════════════════════════════════════════════════
// STATUS QUERY
// ═══════════════════════════════════════════════════════════════════════════════
// Simple status check endpoint - demonstrates literal route pattern.

namespace SyntaxExamples.Endpoints;

using TimeWarp.Nuru;

[NuruRoute("status", Description = "Check system status")]
public sealed class StatusQuery : IQuery<Unit>
{
  public sealed class Handler : IQueryHandler<StatusQuery, Unit>
  {
    public ValueTask<Unit> Handle(StatusQuery query, CancellationToken ct)
    {
      Console.WriteLine("OK");
      return default;
    }
  }
}
