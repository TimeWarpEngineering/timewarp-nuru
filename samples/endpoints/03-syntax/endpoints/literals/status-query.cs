// ═══════════════════════════════════════════════════════════════════════════════
// STATUS QUERY
// ═══════════════════════════════════════════════════════════════════════════════
// Simple status check endpoint - demonstrates literal route pattern.

namespace SyntaxExamples.Endpoints;

using TimeWarp.Mediator;
using TimeWarp.Nuru;

[NuruRoute("status", Description = "Check system status")]
public sealed class StatusQuery : IQuery<Unit>
{
  public sealed class Handler : IRequestHandler<StatusQuery, Unit>
  {
    public ValueTask<Unit> Handle(StatusQuery query, CancellationToken ct)
    {
      Console.WriteLine("OK");
      return Unit.ValueTask;
    }
  }
}
