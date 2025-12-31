namespace AttributedRoutes.Messages;

using TimeWarp.Nuru;
using TimeWarp.Terminal;

/// <summary>
/// Simple health check ping.
/// This is a Query (Q) - read-only health check, safe to retry.
/// Demonstrates ITerminal injection for testable output.
/// </summary>
[NuruRoute("ping", Description = "Simple health check")]
public sealed class PingQuery : IQuery<Unit>
{
  public sealed class Handler : IQueryHandler<PingQuery, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public ValueTask<Unit> Handle(PingQuery query, CancellationToken ct)
    {
      Terminal.WriteLine("pong");
      return default;
    }
  }
}
