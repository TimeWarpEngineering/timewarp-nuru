namespace AttributedRoutes.Messages;

using TimeWarp.Nuru;
using Mediator;
using TimeWarp.Terminal;

/// <summary>
/// Simple health check ping.
/// This is Unspecified ( ) - developer hasn't yet decided if it's a Query or Command.
/// Using IRequest indicates "TODO: classify this properly".
/// Demonstrates ITerminal injection for testable output.
/// </summary>
[NuruRoute("ping", Description = "Simple health check")]
public sealed class PingRequest : IRequest
{
  public sealed class Handler : IRequestHandler<PingRequest>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public ValueTask<Unit> Handle(PingRequest request, CancellationToken ct)
    {
      Terminal.WriteLine("pong");
      return default;
    }
  }
}
